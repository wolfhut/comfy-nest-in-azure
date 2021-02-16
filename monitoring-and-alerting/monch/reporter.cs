using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq; // for Enumerable.SequenceEqual
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

// The way to conceptualize this is:
//
// Within a Resource, there might (or might not) be lots of Services. This is
// primarily because not everything we want to run checks against, even has an
// Azure resource id. So the Resource might be something generic like the
// resource id of the VM doing the checking. In that case, the Service would
// be a string that lets you know what we're even looking at.
//
// (Because Azure does not directly have the concept of a Service, if we
// have a Service name, we have to stick it in as the first Dim. But that's
// not where it belongs, logically.)
//
// Within a Resource, (optionally within a Service), there could be lots of
// Namespaces, corresponding to different areas of interest within that
// Resource. For example, you might be pinging a system, and that would be
// in the "icmp" Namespace, or you might be sending DNS queries to the same
// system, and that would be in the "dns" Namespace. The idea is that they
// really don't even belong in the same category together.
//
// Within a Resource, (optionally within a Service), within a Namespace, there
// could be lots of Dims, corresponding to specific "things" that this Resource
// is managing. For example, the Namespace could be a broad category, but the
// Dims might tell you which specific instance of the category the metrics
// relate to.
//
// Within a Resource, (optionally within a Service), within a Namespace, within
// a particular set of Dims, there are Metrics which are specific kinds of
// measurement we can report on.
//
// Within a Resource, (optionally within a Service), within a Namespace, within
// a particular set of Dims, within a specific Metric, there is timeseries
// data.

namespace Monch
{
    public abstract class MonchReporter
    {
        protected readonly string metricsNamespace;
        protected readonly bool verbose;
        protected struct dataPoint {
            public List<(string, string)> Dims;
            public string Metric;
            public int Count;
            public double Min;
            public double Max;
            public double Sum;
        }
        protected readonly List<dataPoint> dataPoints;

        public MonchReporter(string metricsNamespace, bool verbose)
        {
            this.metricsNamespace = metricsNamespace;
            this.verbose = verbose;
            this.dataPoints = new List<dataPoint>();
        }

        public void Report(IList<(string, string)> dims, string metric,
                           int count, double min, double max, double sum)
        {
            dataPoints.Add(
                new dataPoint {
                    Dims = new List<(string, string)>(dims),
                    Metric = metric,
                    Count = count,
                    Min = min,
                    Max = max,
                    Sum = sum,
                });
        }

        public abstract Task Initialize();
        public abstract Task Finalize();
    }

    public class MonchReporterDryRun : MonchReporter
    {
        public MonchReporterDryRun(string metricsNamespace, bool verbose) :
            base(metricsNamespace, verbose) { }

        public override async Task Initialize() {
        }

        public override async Task Finalize()
        {
            foreach (var oneDataPoint in dataPoints) {
                Console.WriteLine("");
                Console.WriteLine(metricsNamespace);
                foreach (var dim in oneDataPoint.Dims) {
                    Console.WriteLine($"  {dim.Item1}={dim.Item2}");
                }
                double avg = (oneDataPoint.Count != 0) ?
                             (oneDataPoint.Sum / oneDataPoint.Count) :
                             0.0;
                Console.WriteLine(
                    $"    {oneDataPoint.Metric}: {oneDataPoint.Min}/{avg}/{oneDataPoint.Max} (x{oneDataPoint.Count})");
            }
        }
    }

    public class MonchReporterAzure : MonchReporter
    {
        private readonly HttpClient httpClient;
        private readonly DateTimeOffset time;
        private readonly string svcName;
        private readonly string metricsUrl;
        private readonly string authResourceId;
        private readonly string authClientId;
        private readonly string authObjectId;
        private string authToken;

        class AuthResponse
        {
            // There's more gunk in here, but the access token is the only
            // important part.
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
        }

        class MetricsRequestDataBaseDataSeries
        {
            [JsonPropertyName("dimValues")]
            public List<string> DimValues { get; set; }

            [JsonPropertyName("min")]
            public double Min { get; set; }

            [JsonPropertyName("max")]
            public double Max { get; set; }

            [JsonPropertyName("sum")]
            public double Sum { get; set; }

            [JsonPropertyName("count")]
            public long Count { get; set; }
        }

        class MetricsRequestDataBaseData
        {
            [JsonPropertyName("metric")]
            public string Metric { get; set; }

            [JsonPropertyName("namespace")]
            public string Namespace { get; set; }

            [JsonPropertyName("dimNames")]
            public List<string> DimNames { get; set; }

            [JsonPropertyName("series")]
            public List<MetricsRequestDataBaseDataSeries> Series { get; set; }
        }

        class MetricsRequestData
        {
            [JsonPropertyName("baseData")]
            public MetricsRequestDataBaseData BaseData { get; set; }
        }

        class MetricsRequest
        {
            [JsonPropertyName("time")]
            public string Time { get; set; }

            [JsonPropertyName("data")]
            public MetricsRequestData Data { get; set; }
        }

        public MonchReporterAzure(string authResourceId, string authClientId,
                                  string authObjectId, int reportingTimeout,
                                  string region, string resourceId,
                                  string svcName, string metricsNamespace,
                                  bool verbose) :
            base(metricsNamespace, verbose)
        {
            time = DateTimeOffset.UtcNow;
            this.svcName = svcName;
            this.authResourceId = authResourceId;
            this.authClientId = authClientId;
            this.authObjectId = authObjectId;
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(reportingTimeout);

            // The resource ID will start with a slash, so we shouldn't put in
            // a slash of our own.
            metricsUrl =
                $"https://{region}.monitoring.azure.com{resourceId}/metrics";
        }

        public override async Task Initialize()
        {
            if (authToken != null) {
                return;
            }

            // Adapted from:
            // https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-c
            // with "monitoring.azure.com" in place of what they used in the
            // example. (I took "monitoring.azure.com" from the docs for
            // publishing custom metrics)
            string authUrl = "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://monitoring.azure.com/";
            if (authResourceId != null) {
                authUrl += $"&mi_res_id={authResourceId}";
            }
            if (authClientId != null) {
                authUrl += $"&client_id={authClientId}";
            }
            if (authObjectId != null) {
                authUrl += $"&object_id={authObjectId}";
            }
            var req = new HttpRequestMessage(HttpMethod.Get, authUrl);
            req.Headers.Add("Metadata", "true");

            HttpResponseMessage resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            AuthResponse respDecoded =
                await JsonSerializer.DeserializeAsync<AuthResponse>(
                          await resp.Content.ReadAsStreamAsync());

            authToken = respDecoded.AccessToken;
        }

        public override async Task Finalize()
        {
            if (authToken == null) {
                throw new Exception("need to call Initialize()");
            }

            // https://docs.microsoft.com/en-us/azure/azure-monitor/platform/metrics-custom-overview
            // https://docs.microsoft.com/en-us/azure/azure-monitor/platform/metrics-store-custom-rest-api

            var inAzureFmt =
                new Dictionary<string, List<MetricsRequestDataBaseData>>();
            foreach (var oneDataPoint in dataPoints) {
                var dimNames = new List<string>();
                var dimValues = new List<string>();
                if (svcName != null) {
                    dimNames.Add("service");
                    dimValues.Add(svcName);
                }
                foreach (var dim in oneDataPoint.Dims) {
                    dimNames.Add(dim.Item1);
                    dimValues.Add(dim.Item2);
                }

                List<MetricsRequestDataBaseData> dataBaseDatas;
                if (inAzureFmt.ContainsKey(oneDataPoint.Metric)) {
                    dataBaseDatas = inAzureFmt[oneDataPoint.Metric];
                } else {
                    dataBaseDatas = new List<MetricsRequestDataBaseData>();
                    inAzureFmt[oneDataPoint.Metric] = dataBaseDatas;
                }
                MetricsRequestDataBaseData dataBaseData = null;
                foreach (var oneDataBaseData in dataBaseDatas) {
                    // This is some weird linq stuff and I don't understand
                    // it, but this seems to be the idiomatic way of comparing
                    // two lists. Go figure.
                    if (Enumerable.SequenceEqual(oneDataBaseData.DimNames,
                                                 dimNames)) {
                        dataBaseData = oneDataBaseData;
                        break;
                    }
                }
                if (dataBaseData == null) {
                    dataBaseData =
                        new MetricsRequestDataBaseData {
                            Metric = oneDataPoint.Metric,
                            Namespace = metricsNamespace,
                            DimNames = dimNames,
                            Series = new List<MetricsRequestDataBaseDataSeries>(),
                        };
                    dataBaseDatas.Add(dataBaseData);
                }
                dataBaseData.Series.Add(
                    new MetricsRequestDataBaseDataSeries {
                        DimValues = dimValues,
                        Min = oneDataPoint.Min,
                        Max = oneDataPoint.Max,
                        Sum = oneDataPoint.Sum,
                        Count = oneDataPoint.Count,
                    });
            }

            var reqData = new MetricsRequestData();
            var req =
                new MetricsRequest {
                    Time = TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Utc)
                               .ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Data = reqData,
                };
            var uploadTasks = new List<Task<HttpResponseMessage>>();
            foreach (var dataBaseDatas in inAzureFmt.Values) {
                foreach (var dataBaseData in dataBaseDatas) {
                    reqData.BaseData = dataBaseData;
                    string reqJson = JsonSerializer.Serialize(req);
                    if (verbose) {
                        Console.WriteLine($"POST {metricsUrl}");
                        Console.WriteLine($"  -> {reqJson}");
                    }
                    var httpReq = new HttpRequestMessage(HttpMethod.Post,
                                                         metricsUrl);
                    httpReq.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", authToken);

                    // We'd like to be able to do:
                    //     httpReq.Content =
                    //         JsonContent.Create<MetricsRequest>(req);
                    // but we get back a 411 Length Required. So we have to do
                    // this the hard way.
                    httpReq.Content =
                        new StringContent(reqJson, Encoding.UTF8,
                                          "application/json");

                    uploadTasks.Add(httpClient.SendAsync(httpReq));
                }
            }
            foreach (var uploadTask in uploadTasks) {
                HttpResponseMessage resp = await uploadTask;
                resp.EnsureSuccessStatusCode();
            }
        }
    }
}

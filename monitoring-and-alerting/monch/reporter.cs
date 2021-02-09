using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// The way to conceptualize this is:
//
// Within a Resource, there might (or might not) be lots of Services. This is
// primarily because not everything we want to run checks against, even has an
// Azure resource id. So the Resource might be something generic like the
// resource id of the Log Analytics workspace. In that case, the Service would
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
        protected readonly string resourceId;
        protected readonly string svcName;
        protected readonly string ns;
        protected readonly DateTimeOffset time;

        public MonchReporter(string resourceId, string svcName, string ns)
        {
            this.resourceId = resourceId;
            this.svcName = svcName;
            this.ns = ns;
            time = DateTimeOffset.UtcNow;
        }

        public abstract Task Report(IList<(string, string)> dims,
                                    string metric, int count, double min,
                                    double max, double sum);
    }

    public class MonchReporterDryRun : MonchReporter
    {
        public MonchReporterDryRun(string resourceId, string svcName,
                                   string ns) :
            base(resourceId, svcName, ns) { }

        public override async Task Report(IList<(string, string)> dims,
                                          string metric, int count, double min,
                                          double max, double sum)
        {
            if (svcName != null) {
                Console.WriteLine($"{resourceId}/{svcName}");
            } else {
                Console.WriteLine(resourceId);
            }
            Console.WriteLine(ns);
            foreach (var dim in dims) {
                Console.WriteLine($"  {dim.Item1}={dim.Item2}");
            }
            double avg = (count != 0) ? (sum / count) : 0.0;
            Console.WriteLine($"    {metric}: {min}/{avg}/{max} (x{count})");
            Console.WriteLine("");
        }
    }

    public class MonchReporterAzure : MonchReporter
    {
        public MonchReporterAzure(string resourceId, string svcName,
                                  string ns) :
            base(resourceId, svcName, ns)
        {
            // XXX needs to be filled in
        }

        public override async Task Report(IList<(string, string)> dims,
                                          string metric, int count, double min,
                                          double max, double sum)
        {
            // XXX needs to be filled in
        }
    }
}

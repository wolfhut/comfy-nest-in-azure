using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Monch
{
    class MonchCommand
    {
        public class BaseConfiguration
        {
            public string AuthResource { get; set; }
            public string AuthClientId { get; set; }
            public string AuthObjectId { get; set; }
            public string Resource { get; set; }
            public string ResourceRegion { get; set; }
            public string ServiceName { get; set; }
            public bool DryRun { get; set; }
            public bool Verbose { get; set; }
            public int SpreadOutOver { get; set; }
            public int ReportingTimeout { get; set; }
        }
        static async Task<MonchReporter> makeReporter(
            BaseConfiguration baseConfig, string metricsNamespace)
        {
            MonchReporter reporter;
            if (baseConfig.DryRun) {
                reporter = new MonchReporterDryRun(metricsNamespace,
                                                   baseConfig.Verbose);
            } else {
                if (baseConfig.Resource == null) {
                    throw new Exception("Missing --resource");
                } else if (!baseConfig.Resource.StartsWith('/')) {
                    throw new Exception("Resource ID must start with /");
                }
                if (baseConfig.ResourceRegion == null) {
                    throw new Exception("Missing --resource-region");
                }
                reporter = new MonchReporterAzure(baseConfig.AuthResource,
                                                  baseConfig.AuthClientId,
                                                  baseConfig.AuthObjectId,
                                                  baseConfig.ReportingTimeout,
                                                  baseConfig.ResourceRegion,
                                                  baseConfig.Resource,
                                                  baseConfig.ServiceName,
                                                  metricsNamespace,
                                                  baseConfig.Verbose);
            }
            await reporter.Initialize();
            return reporter;
        }

        public class PingConfiguration
        {
            public string Host { get; set; }
            public int Count { get; set; }
            public int Timeout { get; set; }
        }
        public static async Task<int> doPing(
            BaseConfiguration baseConfig,
            PingConfiguration pingConfig)
        {
            var reporter = await makeReporter(baseConfig, "ping");
            await MonchPing.Check(reporter,
                                  await getAddressesByFamily(pingConfig.Host),
                                  pingConfig.Count, pingConfig.Timeout,
                                  baseConfig.SpreadOutOver);
            await reporter.Finalize();
            return 0;
        }

        public class RecursiveDnsConfiguration
        {
            public string Host { get; set; }
            public int Timeout { get; set; }
            public List<string> Name { get; set; }
        }
        public static async Task<int> doRecursiveDns(
            BaseConfiguration baseConfig,
            RecursiveDnsConfiguration recursiveDnsConfig)
        {
            var reporter = await makeReporter(baseConfig, "recursiveDns");
            await MonchRecursiveDns.Check(
                      reporter,
                      await getAddressesByFamily(recursiveDnsConfig.Host),
                      recursiveDnsConfig.Name, recursiveDnsConfig.Timeout,
                      baseConfig.SpreadOutOver);
            await reporter.Finalize();
            return 0;
        }

        // This discards anything except IPv4 and IPv6. I can't think of
        // a reason anyone would want any other address family.
        //
        // This also helps us standardize on a single string representation
        // for various address families, in a format that'll be consistent
        // across different worker classes.
        //
        // It also makes the guarantee that the returned list will not be
        // empty. You can bank on this.
        static async Task<List<(IPAddress, string)>>
            getAddressesByFamily(string s)
        {
            IPAddress addr;
            var d = new SortedDictionary<AddressFamily, List<IPAddress>>();

            if (IPAddress.TryParse(s, out addr)) {
                d[addr.AddressFamily] = new List<IPAddress> {addr};
            } else {
                foreach (IPAddress oneAddr in
                         await Dns.GetHostAddressesAsync(s)) {
                    if (d.ContainsKey(oneAddr.AddressFamily)) {
                        d[oneAddr.AddressFamily].Add(oneAddr);
                    } else {
                        d[oneAddr.AddressFamily] =
                            new List<IPAddress> {oneAddr};
                    }
                }
            }

            var rng = new Random();
            var l = new List<(IPAddress, string)>();
            foreach (List<IPAddress> addrs in d.Values) {
                addr = addrs[rng.Next(addrs.Count)];
                switch (addr.AddressFamily) {
                case AddressFamily.InterNetwork:
                    l.Add((addr, "ipv4"));
                    break;
                case AddressFamily.InterNetworkV6:
                    l.Add((addr, "ipv6"));
                    break;
                }
            }

            if (l.Count == 0) {
                throw new Exception($"{s}: No valid addresses");
            }

            return l;
        }

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Description = "Check a thing";
            rootCommand.Add(
                new Option<string>(
                        "--auth-resource",
                        description: "Resource ID of the Managed Identity to authenticate as"));
            rootCommand.Add(
                new Option<string>(
                        "--auth-client-id",
                        description: "Client ID of the Managed Identity to authenticate as"));
            rootCommand.Add(
                new Option<string>(
                        "--auth-object-id",
                        description: "Object ID of the Managed Identity to authenticate as"));
            rootCommand.Add(
                new Option<string>(
                        "--resource",
                        description: "Resource ID to report stats on"));
            rootCommand.Add(
                new Option<string>(
                        "--resource-region",
                        description: "Azure region of the given resource, e.g. westus2"));
            rootCommand.Add(
                new Option<string>(
                        "--service-name",
                        description: "Optional description, if resource is insufficiently specific"));
            rootCommand.Add(
                new Option<bool>(
                        new string[] { "--dry-run", "-n" },
                        "Do not actually report statistics to Azure"));
            rootCommand.Add(
                new Option<bool>(
                        new string[] { "--verbose", "-v" },
                        "Print extra information to stdout"));
            rootCommand.Add(
                new Option<int>(
                        "--spread-out-over",
                        getDefaultValue: () => 45000,
                        description: "Spread out checks over the given number of milliseconds, if supported by the module"));
            rootCommand.Add(
                new Option<int>(
                        "--reporting-timeout",
                        getDefaultValue: () => 5000,
                        description: "Timeout in milliseconds for authenticating or reporting metrics"));

            var pingCommand = new Command("ping");
            pingCommand.Add(
                new Option<int>(
                        "--count",
                        getDefaultValue: () => 10,
                        description: "Number of pings to send, Vasili"));
            pingCommand.Add(
                new Option<int>(
                        "--timeout",
                        getDefaultValue: () => 15000,
                        description: "Timeout per ping in milliseconds"));
            pingCommand.Add(
                new Argument<string>(
                        "host",
                        description: "Hostname or IP address to ping"));
            pingCommand.Handler =
                CommandHandler.Create<BaseConfiguration,
                                      PingConfiguration>(
                    doPing);
            rootCommand.Add(pingCommand);

            var recursiveDnsCommand = new Command("recursive-dns");
            // Short timeout by default because clients are gonna have
            // equally short timeouts. If a query doesn't succeed in 1 second,
            // it basically counts as a failure regardless.
            recursiveDnsCommand.Add(
                new Option<int>(
                        "--timeout",
                        getDefaultValue: () => 1000,
                        description: "Timeout per query in milliseconds"));
            recursiveDnsCommand.Add(
                new Argument<string>(
                        "host",
                        description: "Hostname or IP address to send DNS requests to"));
            var recursiveDnsNamesArg =
                new Option<List<string>>(
                        "--name",
                        getDefaultValue: () => new List<string> {"google.com"},
                        description: "DNS names to query");
            recursiveDnsCommand.Add(recursiveDnsNamesArg);
            recursiveDnsCommand.Handler =
                CommandHandler.Create<BaseConfiguration,
                                      RecursiveDnsConfiguration>(
                    doRecursiveDns);
            rootCommand.Add(recursiveDnsCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}

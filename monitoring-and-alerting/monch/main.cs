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
            public string Resource { get; set; }
            public string ServiceName { get; set; }
            public bool DryRun { get; set; }
            public int SpreadOutOver { get; set; }
        }
        static MonchReporter makeReporter(BaseConfiguration baseConfig,
                                          string ns)
        {
            string resourceId = baseConfig.Resource;
            if (resourceId == null) {
                // This won't actually work in real life, but if they're just
                // trying something out on the command line with -n, let's make
                // it work for them.
                resourceId = "unknown";
            }
            if (baseConfig.DryRun) {
                return new MonchReporterDryRun(
                               resourceId, baseConfig.ServiceName, ns);
            } else {
                return new MonchReporterAzure(
                               resourceId, baseConfig.ServiceName, ns);
            }
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
            var reporter = makeReporter(baseConfig, "ping");
            await MonchPing.Check(reporter,
                                  await getAddressesByFamily(pingConfig.Host),
                                  pingConfig.Count, pingConfig.Timeout,
                                  baseConfig.SpreadOutOver);
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
            var reporter = makeReporter(baseConfig, "recursiveDns");
            await MonchRecursiveDns.Check(
                      reporter,
                      await getAddressesByFamily(recursiveDnsConfig.Host),
                      recursiveDnsConfig.Name, recursiveDnsConfig.Timeout,
                      baseConfig.SpreadOutOver);
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
                        "--resource",
                        description: "Resource ID to report stats on"));
            rootCommand.Add(
                new Option<string>(
                        "--service-name",
                        description: "Optional description, if resource is insufficiently specific"));
            rootCommand.Add(
                new Option<bool>(
                        new string[] { "--dry-run", "-n" },
                        "Do not actually report statistics to Azure"));
            rootCommand.Add(
                new Option<int>(
                        "--spread-out-over",
                        getDefaultValue: () => 50000,
                        description: "Spread out checks over the given number of milliseconds, if supported by the module"));

            var pingCommand = new Command("ping");
            pingCommand.Add(
                new Option<int>(
                        "--count",
                        getDefaultValue: () => 10,
                        description: "Number of pings to send, Vasili"));
            pingCommand.Add(
                new Option<int>(
                        "--timeout",
                        getDefaultValue: () => 20000,
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
            recursiveDnsCommand.Add(
                new Option<int>(
                        "--timeout",
                        getDefaultValue: () => 10000,
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

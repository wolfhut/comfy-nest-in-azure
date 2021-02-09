using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsClient;

// We only support class IN.
//
// We also only support query type SOA (at least for now), since in a
// pre-transition world, AAAA records aren't guaranteed to exist, and in a
// post-transition world, A records aren't guaranteed to exist. We could
// build some kind of complicated mechanism to specify what kind of query we
// want to do, but SOA is guaranteed to be there and it seems like the
// simplest.
//
// We also don't publish metrics that go into detail about which of the
// names were resolvable and which weren't. It's not likely that a
// nameserver would go into a state where it was categorically unable to
// resolve google.com but microsoft.com worked just fine. Or if that ever
// did happen, it would almost certainly be a problem with the network and
// would not reflect poorly on the health of the nameserver itself.
//
// The metrics we publish only specify how many of the names you asked for,
// worked. This is intended to let you capture stuff like "this nameserver
// is able to resolve 20% of the names on the internet, it's pretty busted"
// vs "this nameserver is able to resolve 90% of the names on the internet,
// it's probably okay".

namespace Monch
{
    public class MonchRecursiveDns
    {
        public static async Task Check(MonchReporter reporter,
                                       IList<(IPAddress, string)> addrs,
                                       IList<string> names,
                                       int timeoutMs, int spreadOutOverMs)
        {
            var rng = new Random();
            var clientsAndFamilies = new List<(LookupClient, string)>();
            foreach (var addrAndFamily in addrs) {
                clientsAndFamilies.Add(
                    (new LookupClient(addrAndFamily.Item1, 53),
                     addrAndFamily.Item2));
            }
            var timeoutSpan = new TimeSpan(0, 0, 0, 0, timeoutMs);
            var optionsAndProtos =
                new List<(DnsQueryAndServerOptions, string)>();

            var optionsUdp = new DnsQueryAndServerOptions();
            optionsUdp.UseCache = false;
            optionsUdp.Timeout = timeoutSpan;
            optionsUdp.Retries = 0;
            optionsAndProtos.Add((optionsUdp, "udp"));

            var optionsTcp = new DnsQueryAndServerOptions();
            optionsTcp.UseTcpOnly = true;
            optionsTcp.UseCache = false;
            optionsTcp.Timeout = timeoutSpan;
            optionsTcp.Retries = 0;
            optionsAndProtos.Add((optionsTcp, "tcp"));

            int totalNumQueries =
                names.Count * addrs.Count * optionsAndProtos.Count;
            int sendEveryMs =
                (totalNumQueries <= 1) ?
                0 :
                ((spreadOutOverMs - timeoutMs) / (totalNumQueries - 1));
            if (sendEveryMs < 0) {
                sendEveryMs = 0;
            }

            var queryTasks = new Dictionary<(string, string),
                                            List<Task<IDnsQueryResponse>>>();
            foreach (var addrAndFamily in addrs) {
                foreach (var optionsAndProto in optionsAndProtos) {
                    queryTasks[(addrAndFamily.Item2, optionsAndProto.Item2)] =
                        new List<Task<IDnsQueryResponse>>();
                }
            }

            // We're going to unfairly penalize whichever address family and
            // protocol we try first, for a given name, because it may not be
            // in the cache the first time we look. So we go through the names
            // in order, but each time through we shuffle the address families
            // and protocols.
            bool isFirst = true;
            foreach (string name in names) {
                var question =
                    new DnsQuestion(name, QueryType.SOA, QueryClass.IN);

                if (rng.Next(2) > 0) {
                    clientsAndFamilies.Reverse();
                }
                foreach (var clientAndFamily in clientsAndFamilies) {
                    if (rng.Next(2) > 0) {
                        optionsAndProtos.Reverse();
                    }
                    foreach (var optionsAndProto in optionsAndProtos) {
                        if (isFirst) {
                            isFirst = false;
                        } else {
                            await Task.Delay(sendEveryMs);
                        }
                        Console.WriteLine($"Querying {name} over {clientAndFamily.Item2} {optionsAndProto.Item2}");
                        queryTasks[(clientAndFamily.Item2,
                                    optionsAndProto.Item2)].Add(
                            clientAndFamily.Item1.QueryAsync(
                                question,
                                optionsAndProto.Item1));
                    }
                }
            }

            var reportTasks = new List<Task>();
            foreach (var familyProtoTasks in queryTasks) {
                var dims = new List<(string, string)> {
                    ("family", familyProtoTasks.Key.Item1),
                    ("protocol", familyProtoTasks.Key.Item2),
                };

                int numSuccess = 0;
                foreach (var queryTask in familyProtoTasks.Value) {
                    try {
                        var response = await queryTask;
                        // As long as response code is NOERROR, flags contains
                        // RA, and answer count is greater than zero, I'm
                        // happy. I'm not going to spend a lot of effort to
                        // check that the answer, actually answers the
                        // question. I actually don't care, because even if it
                        // doesn't, that's almost certainly the authoritative
                        // servers doing something funny, anyway.
                        if ((!response.HasError) &&
                            response.Header.RecursionAvailable &&
                            (response.Header.AnswerCount > 0)) {
                            numSuccess++;
                        }
                    } catch (DnsResponseException ex) {
                    }
                }
    
                reportTasks.Add(
                    reporter.Report(
                        dims, "success",
                        names.Count,
                        (numSuccess < names.Count) ? 0 : 1,
                        (numSuccess > 0) ? 1 : 0,
                        numSuccess));
            }

            foreach (var task in reportTasks) {
                await task;
            }
        }
    }
}

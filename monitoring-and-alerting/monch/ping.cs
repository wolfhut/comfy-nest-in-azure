using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Monch
{
    public class MonchPing
    {
        public static async Task Check(MonchReporter reporter,
                                       IList<(IPAddress, string)> addrs,
                                       int count, int timeoutMs,
                                       int spreadOutOverMs)
        {
            var pingTasks = new Dictionary<string, List<Task<PingReply>>>();
            foreach (var addrAndFamily in addrs) {
                pingTasks[addrAndFamily.Item2] = new List<Task<PingReply>>();
            }

            int totalNumPings = count * addrs.Count;
            int sendEveryMs =
                (totalNumPings <= 1) ?
                0 :
                ((spreadOutOverMs - timeoutMs) / (totalNumPings - 1));
            if (sendEveryMs < 0) {
                sendEveryMs = 0;
            }

            bool isFirst = true;
            for (int i = 0; i < count; i++) {
                foreach (var addrAndFamily in addrs) {
                    if (isFirst) {
                        isFirst = false;
                    } else {
                        await Task.Delay(sendEveryMs);
                    }
                    Console.WriteLine($"Pinging {addrAndFamily.Item2.ToString()}");
                    pingTasks[addrAndFamily.Item2].Add(
                        new Ping().SendPingAsync(addrAndFamily.Item1,
                                                 timeoutMs));
                }
            }

            foreach (var familyAndTasks in pingTasks) {
                var dims = new List<(string, string)> {
                    ("family", familyAndTasks.Key)
                };

                int numReachable = 0;
                long sumRtt = 0;
                long minRtt = 0;
                long maxRtt = 0;
                foreach (var pingTask in familyAndTasks.Value) {
                    var pingReply = await pingTask;
                    if (pingReply.Status == IPStatus.Success) {
                        if (numReachable++ > 0) {
                            if (pingReply.RoundtripTime < minRtt) {
                                minRtt = pingReply.RoundtripTime;
                            }
                            if (pingReply.RoundtripTime > maxRtt) {
                                maxRtt = pingReply.RoundtripTime;
                            }
                        } else {
                            minRtt = pingReply.RoundtripTime;
                            maxRtt = pingReply.RoundtripTime;
                        }
                        sumRtt += pingReply.RoundtripTime;
                    }
                }
    
                reporter.Report(
                    dims, "reachability",
                    count,
                    (numReachable < count) ? 0 : 1,
                    (numReachable > 0) ? 1 : 0,
                    numReachable);
                reporter.Report(
                    dims, "loss",
                    count,
                    (numReachable > 0) ? 0 : 1,
                    (numReachable < count) ? 1 : 0,
                    count - numReachable);
                if (numReachable > 0) {
                    reporter.Report(
                        dims, "rttMs",
                        numReachable, minRtt, maxRtt, sumRtt);
                }
            }
        }
    }
}

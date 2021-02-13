# Alerting

Once you have metrics going in, you can make alerts for the metrics.

Unfortunately, since they have to do with specific VMs, they are not "set up
once and they keep working". You have to re-make them every time you
make new VMs.

## Thresholds

For each thing we want to alert on, we have to figure out two thresholds:
1. Absolute count during a time bucket where if the count falls below that
   threshold, something's wrong, not with the thing itself, but with the
   monitoring. This can be an email alert, responded to when it's convenient
   to do so.
   * Need to keep in mind how many Monch invocations a thing is split up
     into. For example, if a thing is split up into two invocations, then
     if we get at least 51% of the number we expect, then Monch is working.
     (Or the other option there is to set up alerts on the two, separately)
   * The shorter the time granularity, the lower of a bar is appropriate here.
     For example, if the checker VM takes 2 minutes to reboot, then we might
     only get data points for 3 in 5 minutes. But we can reasonably expect to
     get data points for 13 in 15 minutes, or for 58 minutes in an hour.
2. Success percentage during a time bucket where if the absolute count is
   nonzero (meaning, we have SOME data), but the success percentage
   is below the success threshold, then send a page.
   * Need to keep in mind different failure modes. For example, if a DNS
     server is hard-down for IPv4 TCP but working perfectly for the other
     three combinations, then it'll still show up with a 75% success rate.
   * Need to keep in mind external dependencies. For example, if google.com is
     one of the domain names we test recursive DNS with, and if google.com is
     having an outage, then that's a 10% failure rate right there.
   * Need to keep in mind how long it takes for a VM to reboot. One minute of
     downtime due to reboot, is 7% of 15 minutes or 20% of 5 minutes.

| Thing | #datapoints/min | Is monitoring borked? | Is thing borked? |
| --- | --- | --- | --- |
| Recurs. DNS, both AFs combined | 40 | <1500/1hr | <85%/15m |
| Home net reach. per AF | 10 | <500/1hr | <98%/15m warn, <50%/5m crit |

## Setup

Go to the "Monitor" section of the Azure portal. This is outside of any
Log Analytics workspace.

For every alert we need to set up:

* Click New Alert Rule
* Select the resource (it will be of type Virtual Machine)
* Add a condition. For the "is monitoring borked?" ones, there will be one
  condition, and it will be based on count. For the "is thing borked?" ones,
  there will be two conditions: is count nonzero, and then the one based on
  avg.
* Choose a rule name. I suggest the following naming scheme:
  * monitoring_recursiveDns_westus2_warn
  * recursiveDns_westus2_warn
  * monitoring_reachability_homeNetwork_ipv6_warn
  * reachability_homeNetwork_ipv6_warn
  * reachability_homeNetwork_ipv6_crit
* Make sure to save the alert rule to the "monitoring" resource group, so
  that you don't clutter up the "VMs" resource group with alert rules

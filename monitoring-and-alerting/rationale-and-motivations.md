# Monitoring

I have a couple of things I want to be able to monitor.

1. Services being provided by virtual machines in Azure, e.g. recursive DNS,
   or Asterisk
2. I'd like to be able to ping my home network and graph reachability and
   latency over time

I'm assuming that all of the actual monitoring will be done *by* something
running in Azure. I'm not going to try to make this work with some
third-party place where the monitoring runs.

I'm further assuming that where the metrics need to end up in, such that
they can turn into pages and alerts, is Azure Monitoring. And I'm assuming
that I'll use [Azure's own built-in
alerting](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/alerts-overview)
for this. This is mainly a question of cost. Services like Pagerduty
are expensive per month. Azure's monitoring (and paging/alerting based
on the metrics and logs) is free.

## Why not Nagios? or CheckMk?

I... could use Nagios, but... man.

I really want something simpler than that. I don't want to have to worry
about configuration files, and upgrades to the monitoring, and what if
the monitoring goes down, and what kind of backing store does the monitoring
need.

I'm not a big company. I'm sorry. I'm just not. I don't want to have to
spend time debugging why my monitoring system broke.

So I want something that can run from cron, figure out if Thing X is up
or down, and publish some metrics somewhere. That's it. That's all I want.

My [first iteration](old-way/) of this was just a shell script. It had
issues, which are documented in [its README.md](old-way/README.md).

I have a [slightly more sophisticated version](monch/) of the same basic
idea, which I'm hoping will work better but retain the basic simplicity.
At heart, it's just a bunch of cron jobs. And I love that about it.

## Summary

So the plan is:

1. Have one VM in Azure that runs [monch](monch/) from cron
2. It publishes metrics to Azure Metrics
3. I can make metric alerts using Azure's built-in alerting
4. I can also look for OOM-killer syslog stuff the same way, and/or whatever
   else I want to monitor. It's all gonna be in Azure Metrics or Azure
   Log Analytics.

## Slight fly in the ointment

The way Azure Metrics works, you can only publish metrics that are
relating to a specific Azure Resource ID. Furthermore, this resource
ID cannot be the ID of a subscription, or a resource group.

So what to do about metrics that relate to pinging my home network?

My workaround is to attach those metrics to the Log Analytics workspace,
since I can't do it on the Subscription.

Shrug?
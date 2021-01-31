# Log Analytics one-time setup

## Prerequisites

* There should be a Log Analytics workspace already created. (Or more than one,
  depending on how you want to split things up, whether that's prod and test,
  or whatever)

## Setting up the rules
The authoritative document is
[here](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collection-rule-azure-monitor-agent).
You can read the full document for more details, but here
is the short version.

This step will have to be repeated once for each (subscription, log analytics
workspace) pair that will have VMs to be monitored.

(It will *not* have to be repeated for each VM. So in that sense at least,
this can be considered to be one-time setup. It's something you can set up
once and then leave alone from that point on.)

Go to the "Monitor" section of the Azure portal. This is outside of any VM
resources, it's its own top-level thing.

Under Settings, click Data Collection Rules.

Click Add.

In the Basics tab:
* Choose a generic name for the name of the rule. The name should disambiguate
  which subscription, and which log analytics workspace, the rule will apply
  to. For example, "mysubscription-prod" or "myothersubscription-test".

In the Resources tab:
* Don't add any resources for now. This is just to set up the rule.

In the Collect and Deliver tab:
* Add syslogs, and choose the right Log Analytics workspace as the destination.
* Add performance counters. In addition to the default destination,
  also add the same Log Analytics workspace as a second destination.

## Great!

The rule is set up now. It's not doing anything yet, and it won't, until
you go and add resources to it.

What would be awesome is if you could add entire resource groups to it,
and then as you add VMs into those resource groups, they'd automatically
get added to the data collection rule...

Unfortunately that isn't how it works. After you make a VM, you have to
come back to the data collection rule and add that specific VM, as a
resource in the rule.

C'est la vie.

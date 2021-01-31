# OOM killer alerts

## How to test

There is a "mkoom" python script in this directory. If you run it with a
number of megabytes you want it to consume, it will gradually and in a
controlled fashion consume up to that number of megabytes, prompting you
at each step to continue. (Each allocation is done in a subprocess, with
care taken to avoid becoming a forkbomb)

The first allocation it does will be the biggest, which means that
hopefully when *something* gets OOM-killed, it'll be that process.

You can open two windows, tail /var/log/messages in one window and run
mkoom in another, and as soon as you see the OOM killer go off, you can
hit ^C in the window where mkoom is running.

(You will have an easier time generating an OOM situation if you turn
off swap, temporarily, for testing!)

## How to set up alerts

Go to the Log Analytics workspace where the syslogs are going to.

Click Alerts, then New Alert Rule.

Add a condition:
* Choose "Custom log search"
* Put as the search query:
  `Syslog | where SyslogMessage contains "invoked oom-killer"`
* Set threshold to "greater than 0" since even one OOM is something we want to
  know about
* Set both period and frequency to 15 minutes, since that's [where the price
  break is](https://azure.microsoft.com/en-us/pricing/details/monitor/).
  Anything more frequent than that costs more.

Add an action group. I don't particularly want to be woken up in the
middle of the night by an OOM, so email is ok with me.

Give the alert rule a name, and save it.

Wait a few minutes, and then test using the above script, and make sure
you get alerted.

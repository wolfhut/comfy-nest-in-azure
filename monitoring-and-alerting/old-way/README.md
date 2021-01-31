# Old way of doing monitoring

This is how I used to do monitoring. It's based on syslog, and writing
alert rules that check if fewer than X successes happened in a Y minute
period.

*I do not do monitoring this way anymore. This is included for historical
interest only.*

Example cron entries for doing it this way:

```
* * * * * /usr/local/bin/smon "res_ipv4_udp_resolver-20200125-1" "host -t a -W 5 google.com 1.2.3.4"
* * * * * /usr/local/bin/smon "res_ipv6_udp_resolver-20200125-1" "host -t a -W 5 google.com 1::2"
* * * * * /usr/local/bin/smon "res_ipv4_tcp_resolver-20200125-1" "host -t a -W 5 -T google.com 1.2.3.4"
* * * * * /usr/local/bin/smon "res_ipv6_tcp_resolver-20200125-1" "host -t a -W 5 -T google.com 1::2"
* * * * * /usr/local/bin/smon "ping_ipv4_homemachine" "ping -q -c 1 -w 5 -n 1.2.3.4"
```

Example query for doing alerts:

```
Syslog | where Facility == "local1" and SyslogMessage contains "SMON CHECK res_ipv4_udp_resolver-20200125-1 OK"
```

You'd make the alerts in such a way so that if *fewer* than a given number of
OK results happen in a given time period, it alerts.

## So what's wrong with this?

Two things, mainly:

1. The alerts don't auto-resolve. Azure doesn't figure out that because it's
   a new period now and in this period it *did* get the right number of OK
   results, that the alert can be cancelled.
2. There's enough delay getting syslog messages into the Log Analytics
   workspace, that sometimes there's false positives.

So that's why I don't do it that way anymore. It was a nice try. I love the
idea of doing it this way; the reality not so much.

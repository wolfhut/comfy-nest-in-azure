# Recursive DNS

## Prerequisites

1. [(doc)](../virtual-machines/virtual-machines.md) Virtual machines to run
   the DNS servers on. How many? Probably 2, one in `West US` because it's
   low latency from my house, and one in `West US 2` as a secondary, because
   that region has the best prices.
2. The VMs need to be in a Security Group that allows port 53 inbound, from
   my home IP ranges, both TCP and UDP. Note that we are allowing recursion
   from "any" at the application layer, so the Security Group will be the main
   gatekeeper here as far as source IP addresses go.
3. The VMs need to have public IPv4 and IPv6 addresses attached to them.
   (Standard-sku, because VMs don't work with basic-sku)

The reason for number 3 is we can't use a load balancer (for reasons discussed
[here](../rationale-and-motivations/regions-load-balancers-slas.md#load-balancers)).
So we're kind of stuck with the public IP addresses.

## Setup

First install BIND: (note: you can check if there's any other package
variants that might be relevant, by doing `apt search bind`)
```
sudo apt install bind9
```

Now edit `/etc/bind/named.conf.options`:
* Remove all listen-on's (you can leave ones that say "any", as they're
  harmless, but might as well delete them)
* Add into the options section:
  ```
      allow-recursion { any; };
      max-cache-size 100M;
  ```

There seems to be about 200 MB of free+buffer memory on an Ubuntu machine
with nothing else running. So I figure 100 MB is about right for a cache.
If each cache entry is a kilobyte, that's still 100,000 cached entries,
which is... a lot. I don't see any need to push it any higher than that.

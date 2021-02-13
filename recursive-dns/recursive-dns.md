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

## Strange things are afoot at the Circle K

OK, this is a weird one. Ubuntu comes by default with systemd-resolved
turned on. This completely buggers Unbound's ability to run with
`interface-automatic: yes` because Unbound tries to bind to 0.0.0.0 port
53, and it can't because bloody systemd already has it. Ugh. Why!?!

* I was completely unsuccessful at my attempts to turn systemd-resolved off.
* More recent versions of Unbound also have the ability to specify
  interfaces by name, e.g. `interface: eth0`. The version Ubuntu comes
  with in 2021, doesn't. Maybe next year this will be different.
* Debian might, of course, be slightly better. [But,
  well.](../virtual-machines/rationale-and-motivations.md#choice-of-operating-system)
* My 2021 workaround for this, which is described below, is to give the VM a
  static *private* IP address, and specify that in the configuration. Hopefully
  2022 will bring different solutions. And doubtless new and exciting problems
  of its own, withal. But hopefully some solutions too.

# Setup

So, given the above, here's what we gotta do first, as the very first step:
* Go into the Network Interface, in the Azure portal
* Go into the IP Configurations
* Set both IPv4 and IPv6 to static
* Note what the IP addresses are

With that out of the way -- install Unbound:

```
sudo apt install unbound
```

Now make a file `/etc/unbound/unbound.conf.d/wolfhut.conf`, with the
following contents, substituting the correct IP addresses:

```
interface: 172.16.0.5
interface: fdf0:9435:9f80::5

interface: 127.0.0.1
interface: ::1

access-control: 0.0.0.0/0 allow
access-control: ::/0 allow

prefetch: yes
prefetch-key: yes
rrset-roundrobin: yes
```

See [this
page](https://www.nlnetlabs.nl/documentation/unbound/howto-optimise/) for
some configuration hints but also note that I have opted not to tweak
much of anything. The default cache size is 4 MB which sounds tiny until
you think about the fact that that's the size of the King James Bible.
That's a lot! I think I'm fine with the defaults!

More recent versions of unbound default to using qname minimization which I
don't quite trust, but, well, who am I to judge. (The version that Ubuntu ships
with defaults to off, so that's a problem for future-me to worry about,
after Ubuntu catches up)

It defaults to *not* using the 0x20 stuff, which I'm surprised by. I
would have thought that would be on by default. But, again, I'm trusting
its defaults. Maybe there are still enough broken servers out there that
you still can't quite do 0x20 yet. (See the `use-caps-for-id` option in
the config)

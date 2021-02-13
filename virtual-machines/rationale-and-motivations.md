# Virtual machines

## Machine size

As far as I'm concerned, the only worthwhile size is B1ls. It's cheap
and it's got plenty of everything -- including plenty of memory, as long
as you configure some swap.

If you don't configure swap, you *will* run out of memory, but only because
physical RAM is being filled up with tons of useless stuff that will never be
needed. 512 MB is plenty to hold the
[working set](https://en.wikipedia.org/wiki/Working_set) of any workload you
could possibly throw at the machine; and swap is there to hold all the useless
stuff so that it's not clogging up physical RAM.

Couple other things to note:

* SSD is not worth it. Go for Standard HDD. The whole business about SSD
  qualifying you for a better SLA is, in my opinion, [FUD and completely
  bogus](../rationale/regions-load-balancers-slas.md).
* If you go for the reserved instance pricing, a B1ls instance with Standard
  HDD is mind-buggeringly cheap. Which is awesome.

## Choice of operating system

My first choice would be FreeBSD. That seems to be impractical in Azure
because Azure wants to expose IPv6 configuration through DHCPv6, and
FreeBSD expects everything to come through Router Advertisements and it
doesn't include a DHCPv6 client.

My second choice would be Alpine Linux, since that's the most BSD-like of
all the Linuxes. That seems to be impractical in Azure because -- who am
I kidding, it's impractical anywhere. Awesome and technologically superior
to other Linuxes in pretty much any way you could name, but, impractical.

My third choice would be Debian, because it's not Red Hat and it's pretty
basic and inoffensive. But it's less well supported in Azure -- it doesn't
come with a built-in checkbox for AAD authentication, and I also have some
concerns about how waagent is set up. (the waagent configuration seems to
indicate that both waagent and cloudinit both are fighting over the
resource disk??)

My fourth choice would be Ubuntu, because it's like Debian, just a little
bit (or a lot) ickier. Fortunately, Ubuntu seems to be pretty well supported
in Azure.

So I'm going with Ubuntu. Just a second while I get naked. Aaahhh. That's
better.

*Note:* There will be places where we run into Ubuntu brain damage. The
fact that it comes by default with systemd-resolved enabled, is just one
example. There are many others. Ubuntu is, by any objective measure, a
terrible operating system. But that doesn't change the fact that it's the
best supported by Azure, cloudinit Just Works (mostly, kind of), AAD
authentication Just Works, and I think we just have to deal with its areas
of brain damage as well as we can.

The whole thing is a dumpster fire. I think the name of the game is to
just carve out a relatively stable part of the dumpster fire, draw a box
around it, and do the best we can within that box.

## Azure Active Directory integration

This is described
[here](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/login-using-aad).

I generally think it makes for a better experience if you're not uploading
an ssh key where if you have to change the key for any reason, now you're
stuck updating it on N different VMs. And what if you have multiple users
you want to have access?

I like what they're doing with AAD integration. It's a checkbox option during
install.

You still have to fill in the username/password on the first screen. But
you can delete the user afterwards and just use AAD. (And if your AAD
integration breaks in the future, Azure provides a way of getting in:
go to the VM and click Reset Password. It'll create a new local account)

## Cloudinit

Cloudinit is a pretty capable beast. I would love to take more advantage
of it. The problem is that cloudinit runs at the same time as everything
else is also happening during system setup, and one of those "other things"
is the Azure Active Directory integration is being set up. This is a thing
that requires installing packages, and if cloudinit is trying to install
packages at the same time, you tend to get locking conflicts.

So I'm just doing the minimum, which is setting up swap. Anything else
that needs to be done, can be done after the machine has booted.

Do I wish that were different? Sure. But it ain't. [And really that's
okay.](../rationale/no-arm-no-puppet-no-terraform.md)

## What defines "well supported by Azure"?

I'm defining "well supported by Azure", loosely, as consisting of the
intersection between the sets of supported operating systems for the
following:

1. [Cloudinit](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/using-cloud-init)
2. [Azure Monitor
   Agent](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/agents-overview#supported-operating-systems)
3. [AAD
   authentication](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/login-using-aad)

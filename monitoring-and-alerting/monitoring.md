# Monitoring

## Prerequisites

1. [(doc)](../virtual-machines/virtual-machines.md) A virtual machine to run
   the monitoring from
   * *NOTE:* See below discussion on VM sizing
2. The VM needs to have a [User-Assigned Managed
   Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal). *Save the client
   ID.* You will need it later.
3. That Managed Identity needs to be assigned the Monitoring Metrics Publisher
   role in:
   - the Resource Group that contains the VMs being monitored
   - the Resource Group that contains the VM doing the monitoring, if
     different from above (since that VM is going to hold the spillover metrics
     that don't have a more specific Resource ID to attach to)
4. The VM needs to have a public IPv4 and IPv6 address attached to it.
   (Standard-sku, because VMs don't work with basic-sku)
5. The VM needs to have [the .net
   SDK](https://docs.microsoft.com/en-us/dotnet/core/install/) installed on it.
   This is so that you can build Monch. See below.

Number 3 probably bears some clarification. See [the
discussion](rationale-and-motivations.md#slight-fly-in-the-ointment) of the
fact that Azure needs the metrics that you publish, to be hung off of a
specific Resource ID. So that means that the Managed Identity that Monch
is going to run as, needs access to be able to publish metrics to the
places it needs to publish them to. Namely, (a) the VMs that the metrics
relate to, if they relate to VMs, and (b) the VM we've chosen to stick them
on, if there's no better place to put them.

Regarding number 4, it's because we need ICMP to be able to do pings, and
the NAT that Azure gives you by default, doesn't pass ICMP. It should be
locked down tight with a security group that doesn't allow anything
inbound. (You'd think the security group would need ICMP inbound. It
seems to work just as well without, actually. It seems to work with
just the default "nothing extra allowed" security group rules)

## Setup

First, build [monch](monch/) on the VM, and copy the binary to
`/usr/local/bin/monch`.

Next, make a `monitoring` user and add some stuff to its crontab:

```
sudo useradd monitoring
sudo crontab -e -u monitoring
```

```
* * * * * /usr/local/bin/monch --auth-client-id d73d436e-bcfb-468d-bd70-a227a27cb462 --resource /resource/id/of/dns/resolver/vm --resource-region westus2 recursive-dns 1.2.3.4 --name google.com --name microsoft.com --name apple.com --name wikipedia.org --name amazon.com --name reddit.com --name twitch.tv --name adobe.com --name netflix.com --name ebay.com > /dev/null 2>&1
* * * * * /usr/local/bin/monch --auth-client-id d73d436e-bcfb-468d-bd70-a227a27cb462 --resource /resource/id/of/dns/resolver/vm --resource-region westus2 recursive-dns 1::2 --name google.com --name microsoft.com --name apple.com --name wikipedia.org --name amazon.com --name reddit.com --name twitch.tv --name adobe.com --name netflix.com --name ebay.com > /dev/null 2>&1
* * * * * /usr/local/bin/monch --auth-client-id d73d436e-bcfb-468d-bd70-a227a27cb462 --resource /resource/id/of/checker/vm --resource-region westus2 --service-name home-machine ping 5.6.7.8 > /dev/null 2>&1
* * * * * /usr/local/bin/monch --auth-client-id d73d436e-bcfb-468d-bd70-a227a27cb462 --resource /resource/id/of/checker/vm --resource-region westus2 --service-name home-machine ping 5::6 > /dev/null 2>&1
```

The client ID should be the client ID of the checker VM's User-Assigned
Managed Identity. It's needed because there's also a System-Assigned Managed
Identity, and when you get a token, the metadata server doesn't know which
Managed Identity to feed you. So you have to tell it explicitly which one
you want.

The `--service-name` is just a human readable string to associate with the
thing you're pinging. In this case, since I'm pinging my home machine, I
set it to `home-machine`. It's optional, but recommended in cases where
there's no good Resource ID to use so you have to use the checker VM's
resource ID.

Note that monch is capable of doing DNS lookups on hostnames, and if it finds
both an A record and a AAAA record on a hostname you give it, it'll try both.
So you may be able to get away with things like:

```
* * * * * /usr/local/bin/monch --auth-client-id d73d436e-bcfb-468d-bd70-a227a27cb462 --resource /resource/id/of/dns/resolver/vm --resource-region westus2 recursive-dns hostname.of.dns.server > /dev/null 2>&1
```

if the DNS name `hostname.of.dns.server` has both A and AAAA.

# VM Sizing

Unlike other VMs, the checker VM may actually need an instance size bigger
than B1ls. The biggest problem is that, even with all the pre-compilation we
do, we can't disable the C# JIT entirely. The C# runtime environment spins up
a whole lot of infrastructure on process start. It's rather impressive and
awesome, but it does not necessarily thrive in a "many short-lived executions
kicked off from cron" context.

I'm sure I could get away with a B1ls instance if I made it into a
long-running daemon where the JIT would work in my favor instead of working
against me.

But I value the simplicity of how things are, this way. I'd rather just throw
a little more money at the problem.

Start with a B1ls instance and just see how it does. Don't trust `top` --
the real thing to watch is the `CPU Credits Remaining` graph. This is where
the "burstable" nature of the B1 series comes into the picture.

Simple rule: If the derivative of `CPU Credits Remaining` is positive, you're
fine. If the derivative is negative, change the instance size to B1s (or even
B2s). Keep in mind the [instance size flexibility
ratios](https://docs.microsoft.com/en-us/azure/virtual-machines/reserved-vm-instance-size-flexibility)
when making the choice. Changing the instance size of a VM is a low-stress
operation and does not require deleting and recreating the VM. So it's fine
to have a "wait and see" approach.

# Regions, Load Balancers, & SLAs

## Regions, paired regions, pricing

Regions are not created equal, as far as pricing is concerned. In the US,
`West US 2` and `East US 2` have the cheapest prices on virtual machines,
managed disks, and blob storage.

I'm really into this idea that this is the future and compute is post-scarcity.
So I like the idea of defaulting to one of the cheap regions for everything.
I've chosen `West US 2` as my default where everything goes if there's no
compelling reason for it to go someplace else.

As far as my actual physical house goes, there are Azure regions that are
closer and there are Azure regions that are further away. `West US` is closer
to my house than `West US 2` is (although it's also slightly more expensive).

The Azure documentation warns you explicitly against picking two arbitrary
regions that are convenient for you, and assuming that you will be protected
if you have redundancy between those two regions. The [official
documentation](https://docs.microsoft.com/en-us/azure/best-practices-availability-paired-regions)
explains that if you do that, and if there is a big enough disaster, then
*both* of the regions you pick, might be the last to be repaired, in their
respective pairs.

I admit there is a lot of truth in this. But really none of the paired
regions make sense for me personally. `West US` is paired with `East US`
and I don't particularly have any desire to put anything there; and
`West US 2` is paired with `West Central US` which is expensive *and*
further away from me and there's just really nothing to recommend it for
my specific use case.

So for me, I'm going to go with `West US 2` for as much as I can, and take
advantage of the cheap prices; in cases where I need another region, or
in cases where I need ultra-low latency to my home network, I will go with
`West US`.

If a meteor falls and obliterates half the west coast... I think I'm going
to have bigger problems than the fact that my stuff in Azure might take
a little longer to get repaired.

## Load balancers

Microsoft would like to push you towards using "standard" load balancers,
which are $18/month at minimum (and go up from there if you add more rules),
rather than "basic" load balancers, which are free.

I don't particularly feel like paying $18/month for a load balancer no matter
how awesome it is. [Sorry.](post-scarcity.md)

I feel that way especially because compute in Azure is so cheap. You can
get a VM for $3/month including disk, but a load balancer is 6 times that?
Wait, what?

So, OK, "standard" load balancers are off the table. That means "basic"
load balancers are what we've got to work with. Well, they come with some
pretty big caveats, namely the following:
* "Basic" load balancers don't support static IP addresses, only dynamic ones.
  This fact is hard to corroborate in the documentation, but if you're having
  trouble convincing yourself of it, consider this: "Basic" load balancers only
  accept "basic-sku" public IP addresses, and if you try to create a
  "basic-sku" IPv6 address, it won't let you choose the Static option. There's
  no way around this; no clever workaround. Even if you go for maximum
  cleverness and try to create a whole IPv6 prefix and allocate addresses out
  of that, you will find that the only addresses you can allocate from it are
  "standard-sku" addresses, which won't work with "basic" load balancers.
* "Basic" load balancers don't support Availability Zones. They support
  something closely related, called an Availability Set. This doesn't turn
  out to be a huge problem in practice, but the first time you run into the
  concept of an Availability Set it won't quite make sense to you, and you
  have to bang your head against it a few times before it starts making sense.
  So this part is okay really.

One thing this implies is that you can't use a load balancer for a recursive
DNS server, because those need to have static IPs because that's how clients
find them.

For everything else, though, dynamic IP addresses aren't really a problem
since if you serve your authoritative DNS zones from Azure DNS, then you
can make an A record or an AAAA record whose value is "whatever the IP
address of this Azure resource is currently". And that solves the problem
neatly.

So that brings up another point. "Basic" load balancers are free, but
an IPv4/IPv6 public address pair is $6/month. That doubles to $12/month
if you want to have an address in each of two regions.

Here's the solution to that: Once you've created a load balancer, and
added IPv4/IPv6 front-end addresses to it -- you can create any number
of "rules" that match different ports on those front-end IP addresses.
So you could have a single load balancer that is sending port 22 to one
VM, port 80 to a different VM, and some other port to a third VM.

Is this gross? Not really, at least in my mind. You might want to partition
things by [subscription](subscriptions.md), so that a user of Alice's port
80 doesn't decide to be clever and poke around and wind up on Bob's port 22.

But really I don't see it as any different from what you'd have had in a
pre-cloud world where you would have had One Machine In Your Garage That
Does Everything, and everything would be pointing at the same IP address
there, too.

## SLAs

First of all, let me be clear on why I care about SLAs. I never intend to
actually ask for any money back. That would be silly. I care about them as
a *proxy* for what I actually care about, which is "how reliable does
Microsoft expect this to be". I'm interested in what SLAs can hint at,
as far as what Microsoft anticipates the reliability of different options
to be.

[Here](https://azure.microsoft.com/en-us/support/legal/sla/virtual-machines/v1_9/)
is the official Azure documentation on SLAs. The tl;dr is:
* You get 3.5 nines if you have a load balancer
* You don't get any SLA benefit from having 3 VMs in a load balancer. If you
  have 2 VMs in a load balancer then you've got all the SLA boost you're going
  to get. (I think this hints at their assessment of how likely it is that
  two fault domains will be out simultaneously)
* You get 3 nines if you don't have a load balancer but you are using premium
  SSD disks
* You get 1.5 nines if you don't have a load balancer but you are using
  Standard HDD disks

So that's interesting. Does Microsoft really have that little faith in their
Standard HDD offering? Well, no, as it turns out. According to [the
documentation](https://docs.microsoft.com/en-us/azure/virtual-machines/managed-disks-overview)
on Managed Disks, they're designed for 5 nines of availability. And this
is true regardless of whether they're Standard HDD or Premium SSD.

My interpretation of this, is that Standard HDD and Premium SSD are equally
reliable, but that a VM with Standard HDD is being sold basically at-cost,
and the margins just aren't high enough for Microsoft to be able to offer
much of an SLA on them.

But that's actually kind of awesome. What that means is that a VM with
Standard HDD disks, is cheap, low-margin, and (probably) will have 5 nines
of reliability. That's kind of an exciting thought! We're living in a
post-scarcity future and this is actually really cool.

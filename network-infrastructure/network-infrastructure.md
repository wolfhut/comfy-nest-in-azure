# Network Infrastructure

## Networks

You're going to need a network for each region you want stuff in. These
can be named using their regions, for example:
* `net_westus`
* `net_westus2`

As far as IPv4 space goes, I've found it helpful if each of them has a
/16 from RFC1918 space, and if they don't overlap. For example, one of them
might be `172.16.0.0/16` and another might be `172.17.0.0/16`.

As far as IPv6 space goes, the equivalent to RFC1918 space in IPv6 is
`fd00::/8`. You will sometimes see people referring to it as `fc00::/7`, but
you're not supposed to use the first half of that space, per
[RFC4193](https://tools.ietf.org/rfc/rfc4193.txt). So the way this works is
you come up with 40 bits of randomness and you smush around the octets a
lil bit and you eventually end up with a /48.

My favorite way of generating private IPv6 address space is with an interactive
Python interpreter. For example, if I did:
```
>>> import binascii
>>> import os
>>> binascii.b2a_hex(os.urandom(5))
b'f094359f80'
```

Then I'd stick an `fd` in front, put colons in where appropriate, and I'd
have `fdf0:9435:9f80::/48`.

## Subnets and Security Groups

I like to have subnets for all of the types of hosts I anticipate wanting
to have. If the network in a particular region has a /16 worth of private
IPv4 space, then I can make all my subnets as /24's and I'll have enough
space for 256 of them. (And all the networks in all the other regions,
will, too.)

For example, I might have subnets named:
* web_servers
* recursive_dns_servers
* jumphosts

Even if I don't anticipate having (for example) any web servers in a particular
region, I still make a full complement of subnets in each region, because I
value the consistency. I want each region's network to look like each other
region's network.

Each subnet gets a security group. Now a security group is just a list
of firewall rules, and you'd think they could be global; but in fact they
have to be located in the same region that they're used in. So you have
to have security groups like:
* web_servers_westus
* web_servers_westus2
* recursive_dns_servers_westus
* recursive_dns_servers_westus2
* jumphosts_westus
* jumphosts_westus2

Same argument about consistency applies. I want all the subnets and all
the security groups to exist in all the regions I have a presence in, just
so that I don't have to worry about what exists or doesn't exist where; I
can just assume that if something is a certain way in one region, that it's
that way in all the regions.

## Peering and Private DNS

It would be nice if the networks could all talk to each other and could
all function as one big network.

It would be nice if Azure had support for that. [Oh wait, it
does!](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-peering-overview)

Peering is pretty easy to set up and it basically does exactly what it says
on the label. You go into one side and you add a peering relationship with
the other, and that automatically creates the relationship in the reverse
direction as well.

The only unexpected consequence it has, is that you can no longer refer to
machines by their names anymore, because the automatic DNS naming that Azure
does, only works within a single network.

Thankfully, there's a solution for that too, and it's [really easy to set
up](https://docs.microsoft.com/en-us/azure/dns/private-dns-overview), too.

Basically the tl;dr for setting up a private DNS zone is:
* Make a Private DNS Zone resource in the subscription you want the domain
  to be resolvable in. Let's say you name it "foo.bar".
* From within the private DNS zone, add "virtual network links", one per
  network which you want to participate. Check the "autoregistration" box
  when doing so.

What that does is, it makes any VMs in any of the networks, show up as
entries underneath the foo.bar zone; and it also makes anything within
foo.bar be resolvable through the default Azure resolvers, from any of
the networks.

So it basically makes everything Just Work. You do have to remember to
tack on `.foo.bar` onto everything you ssh into. But that's not so much
of a big deal.

## Load Balancers and Availability Sets

I'd like to make sure there's one load balancer, and at least one vip,
available in every region for just "random stuff" to get added to.

We also have to keep in mind that for everything that there's more than
one of, in a load balancer, there also needs to be an Availability Set
for that thing, in that region. (For reasons that I don't quite understand,
the Availability Set resource needs to be in the same resource group as the
VMs in the set. It's not good enough for it to be in the same resource
group as the load balancer.)

So what this looks like, is you end up with one "basic" load balancer per
region:
* lb_westus
* lb_westus2

We'll start out with one pair of vips in each region:

* vip_westus_1_v4
* vip_westus_1_v6
* vip_westus2_1_v4
* vip_westus2_1_v6

Maybe one pair in each region will be enough; or maybe two things will both
want the same port, and we'll have to undergo mitosis and sprout a second
pair of vips in a particular region. If so, that's fine; it can be attached
to the same load balancer.

Each of the vips will be a basic-sku public IP address, dynamic, with the
timeout set as high as the slider will go. The built-in Azure DNS stuff
[isn't needed](../authoritative-dns/authoritative-dns.md).

And for things that need Availability Sets, we can do something like:
* availability_foobar_westus
* availability_foobar_westus2

Each of them can be configured with 3 fault domains and 3 update domains.
That will work even if there's only 2 machines in each set. If there's 2
machines in the set, then the first two created will be in zones A and B,
then the next two will be in zones C and A. It still works out.

# Recursive DNS

The aim of this is restricted solely to providing recursive DNS service
for my home network.

For stuff running inside Azure, I totally 100% trust the Azure-provided
resolvers.

We [can't use a load
balancer](../rationale-and-motivations/regions-load-balancers-slas.md#load-balancers)
for this. So there's going to be two pairs of public IP addresses. Which is
probably fine.

## Why not BIND?

My 2020 setup did in fact use BIND. But, in 2021, when I was trying to move
everything over [from Debian to
Ubuntu](../virtual-machines/rationale-and-motivations.md#choice-of-operating-system),
I had all kinds of problems (on Ubuntu) with the DNSSEC trust anchors. Nothing
in the logs made any sense.

I was able to get it kinda working, ish. It would probably have been okay.
But it just didn't give me the warm fuzzies. I want my DNS server to be
*rock stable*. I want it to turn on and boom, it works.

Unbound may not have the "cool factor" of BIND, but it seems to work.

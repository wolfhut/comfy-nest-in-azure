# Recursive DNS

The aim of this is restricted solely to providing recursive DNS service
for my home network.

For stuff running inside Azure, I totally 100% trust the Azure-provided
resolvers.

We [can't use a load
balancer](../rationale-and-motivations/regions-load-balancers-slas.md#load-balancers)
for this. So there's going to be two pairs of public IP addresses. Which is
probably fine.

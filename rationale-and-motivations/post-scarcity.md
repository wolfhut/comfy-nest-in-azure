# Post-Scarcity

Sci-fi authors like Charles Stross have proposed this idea of "computronium".
According to them, in the future, there will be these huge computers the
size of a planet where the waste heat from the inner layer will be used
to power the next layer out, and so on, until you get to the outer layer
which is sipping microwatts or nanowatts from the waste heat it's getting,
and it's just adding its own tiny little bit of computing horsepower to the
larger whole, but taken in aggregate, the whole system is immensely efficient
at turning an absolute minimum of electricity, into an absolute maximum of
combined total computing power.

## Are we living in that world? We may be closer than you might expect.

How awesome is it that Azure will just give you, [for
free](https://azure.microsoft.com/en-us/pricing/details/functions/), 400,000
gigabyte-seconds of compute/memory every month? That's not an introductory
offer that will expire, it literally means you get free compute every
month. If you can dream up something and express it in terms of an Azure
Function, you can run what you dreamed up, literally for free.

How awesome is it that a VM with 512 MB of memory can be had for $3/month
including disk? That might seem like a small amount of memory to our jaundiced
eyes today, but remember, in the 1990s if you had a system with 24 MB you could
run basically anything and everything you could think of. Even if we adjust for
the fact that 64-bit stuff uses twice the memory that 32-bit stuff did, there's
just no getting around the fact that 512 MB is way, way more than enough for
anything you could possibly want to do with a machine.

Azure lets you upload stuff to a blob storage account and serve it up as a
static web site, for literally pennies. Add a CDN and it's no more expensive,
*and* you get URL rewriting rules and HTTPS.

Archive-tier blob storage is one third of one cent, per gigabyte per month,
and that's if you go with the absurdly high (16 nines reliability!)
replication factor. If you're content with a "mere" 11 nines of reliability,
it's even cheaper, one tenth of one cent per gigabyte per month.

Basic-tier load balancers are completely free!

With Cosmos DB, you get an allotment of 400 request-units per second completely
free of charge.

And with the Weather API, 50 cents gets you 1,000 weather forecast requests.
That's one twentieth of one cent, per request.

## But...

But it's not all like that in Azure. You can find some astoundingly bad
deals in Azure, and you don't even have to look very hard.

$18/month for a premium-tier load balancer? C'mon.

$10/month for the lowest tier of App Service? You can run the same thing
on a VM that costs a third the price.

$30/month for the lowest tier of container? Are you serious? Not only
could you run the same thing on a VM that costs *one tenth* the price, but --
isn't one of the selling points of containers, the fact that you can pack a
lot of them into a single host machine and so they should be cheap? WTF?

$22/month for Azure Front Door, or $18/month for the lowest tier of Application
Gateway? I'd rather not.

## So:

Rather than get upset about the things in Azure that are bad deals, I'd
rather be happy -- more than happy -- thrilled! -- at all the things in
Azure that are amazing deals.

I don't need a premium-tier load balancer for my recursive DNS servers. I can
run two cheap VM instances in two different regions, and that will be just as
good.

I don't need a container to run my code in. I can write it as an Azure
Function app, and run it for free -- literally free.

The philosophy I've chosen to take is: If I run up against something
where Azure wants me to do something expensive, I just stop. I mull it over
for a while. There is always a better way. There is never going to be a
situation where you're just stuck and you have to pay for the expensive thing.

We are living in the future, I think. It's maybe a little bit of an imperfect,
flawed future. It's got blemishes for sure. But I think I'm happy with it.

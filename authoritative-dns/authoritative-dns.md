# Authoritative DNS

Authoritative DNS works about like how you'd expect.

AXFR/IXFR aren't supported, in either direction. Which is annoying, but
oh well.

The one thing that it's very very much worth calling out, is that in
Azure, an A or AAAA record doesn't have to be configured with a literal
IP address. It can be, of course. But you can also create an A or AAAA
record whose target is a pointer to a specific Azure resource -- whether
that's an Azure public IP address resource, or a CDN endpoint resource,
or whatever.

That's the piece of magic that makes dynamic IP addresses not suck. It
means you don't have to play games with CNAMEs, either. You can just make
an A or AAAA that will magically track whatever the current IP address of
the thing is.

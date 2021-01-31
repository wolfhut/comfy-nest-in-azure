# CDNs and Static Web Sites

## Too many choices!

There's about four different ways of hosting static web sites on Azure:
1. You can [upload your static web site to a blob storage
   account](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website-how-to),
   and it will get served directly from there
2. You can [upload your static web site to a blob storage
   account](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website-how-to),
   but [serve it from a CDN
   instead](https://docs.microsoft.com/en-us/azure/storage/blobs/static-website-content-delivery-network)
3. You can use [Azure Static Web
   Apps](https://docs.microsoft.com/en-us/azure/static-web-apps/)
4. You can use [App
   Service](https://docs.microsoft.com/en-us/azure/app-service/quickstart-html)
   to serve a static web site

Of those four, I nix #4 because it's too expensive. (They lure you in by
claiming the lowest tier is free, but then you find out that the lowest tier
doesn't let you assign a custom domain)

I also nix #3 because it's "weird" if all you want is the 21st century
equivalent of a public_html directory. It's got configurable routes and
stuff, and it's just sort of odd.

#1 is alright as far as it goes. It's cheap. Traffic gets served out of
whatever region you have your storage account in. Plain HTTP works fine
(as long as you turn off the setting that enforces HTTPS); HTTPS also
works fine (with some caveats). But there's no configurability; no way
to serve up a redirect from HTTP to HTTPS, or from `example.com` to
`www.example.com`, or whatever.

I think #2 is the sweet spot. It's exactly the same price as #1 if you
go with the Microsoft CDN option (since Microsoft doesn't charge you
for requests to the origin server that go through their own network, and
you only get charged for the actual serving that is done by the CDN,
to users), and it's configurable and it supports redirects, and it has
*good* support for HTTPS. And there's all kinds of cool caching and
traffic optimization and stuff. And there's actual proper access logging!

So this documentation will be focused on #2.

## HTTPS

Azure CDN has this awesome feature where it can be its own CA, and it can
take care of making the certificates for you and everything.

Unfortunately, it doesn't work for apex domains (e.g. `example.com`). It
only works for e.g. `www.example.com`. [This is a documented
limitation.](https://docs.microsoft.com/en-us/azure/cdn/cdn-custom-ssl#prerequisites)

Rather than have to keep track of "oh, this subdomain can be handled by
Azure, but this apex I have to handle myself", I'd rather just handle
everything myself and not be surprised.

So I'm going to say don't use that feature, and always use the "bring your
own certificate" feature.

## $web

The way Azure wants you to do static web site hosting using blob storage,
might seem a little odd at first. You can have at most one `$web` container
per storage account, not "an arbitrary number". And what makes `$web`
containers so different from if you just made a regular blob container and
turned on anonymous public read access?

It turns out that the special behaviors that are enabled if you use the
Azure-provided magical `$web` container are actually pretty helpful. As
far as I've been able to figure out, these consist of:
* `/foo`, `/foo/`, and `/foo//` all work, and they are all served with content
  from the blob named `/foo/index.html`
* 404s actually look like a human-readable HTML error message, not a bunch of
  XML
* It's friendlier to the kind of quirks about the path that might come from
  humans sitting in front of web browsers.

So there's good reasons to do it the way they want you to. It does mean
that if you want to have multiple web sites then you need multiple storage
accounts. If you really really wanted to pack more than one web site into
a single `$web` container, the workaround you could use would be to name
your files with a directory prefix, like so:
* `example.com/index.html`
* `example.org/index.html`

... and then add the corresponding directory prefix in the CDN endpoint, to
be prepended to all requests to the origin that come through the endpoint.
But that would just be weird and please don't do that.

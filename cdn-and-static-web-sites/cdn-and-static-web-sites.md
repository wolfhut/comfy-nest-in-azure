# CDNs and Static Web Sites

## Prerequisites

* There should be a Log Analytics workspace already created.
* [(doc)](../certificates-and-key-vault/) There should be a Key Vault with
  the key and X.509 certificate you plan to use for HTTPS, already created.

## Make a storage account for the content

From the portal, create a storage account. 

On the Basics tab:
* The default is standard performance, which is what you want. Not premium.
* The default is StorageV2, which is what you want.
* You can choose the replication factor [according to your
  preferences](https://docs.microsoft.com/en-us/azure/storage/common/storage-redundancy?toc=/azure/storage/blobs/toc.json#durability-and-availability-parameters).
  My recommendations/preferences are
  [here](../storage/rationale-and-motivations.md).

On the Networking tab:
* All the defaults are good

On the Data Protection tab:
* Pick whatever combination of stuff you want. More discussion is
  [here](../storage/rationale-and-motivations.md). The defaults are just fine
  too.

On the Advanced tab:
* If you want to be able to actually serve content over HTTP, then turn off
  `Secure transfer required`. Or if you want to redirect HTTP to HTTPS and
  not actually serve any content over HTTP, then it's fine to leave
  `Secure transfer required` enabled. (Yes, this is true even though we're
  going to be using a CDN. It turns out the CDN makes HTTPS requests over
  HTTPS, and HTTP requests over HTTP)
* Leave everything else at the default.
* In particular, it's fine to leave the `Allow Blob public access` setting at
  the default. It turns out it doesn't really much matter what you set it to,
  since the rules for the `$web` container are different anyway.

Click Create.

Now go into the storage account you just created. Under Settings, go to
"Static Website".
* Set it to enabled
* Set the index document name to be "index.html"
* You can leave the error document path alone, the default 404 page without
  any customization is just fine.

## Upload the content

Upload your content to the `$web` blob container, inside the storage account.
[This document](../storage/cli-cookbook.md) may help, or you can just use
the upload feature in the portal.

As discussed [here](rationale-and-motivations.md#web), there can only be one
`$web` container per storage account. If you want to have multiple web sites,
the preferred option is to just create multiple storage accounts. It doesn't
cost any more to do it that way, because everything is charged per gigabyte.

## Create a CDN

Create a CDN profile resource, with pricing tier set to "Standard Microsoft".

Go into the resource you just created.

First step is to enable access logging:
* Under Monitoring, click "Diagnostic Settings"
* Add a new Diagnostic Setting, of type "AzureCdnAccessLog", that sends
  logs to your Log Analytics workspace.

Next step is to create endpoints. Depending on what you want to do, you
may want endpoints that don't serve any content but just serve redirects;
or you may want endpoints that actually serve content.

To create an endpoint that is incapable of serving content, but that
can serve up redirects just fine:
* Add an endpoint, with an arbitrary name (the name has no relation to what
  the hostname will eventually be), with origin type "Custom origin" and
  an origin hostname of "0.0.0.0" (or anything, really, but I prefer that).
* Leave everything else at defaults.

To create an endpoint that is capable of serving actual content:
* Add an endpoint, with an arbitrary name (the name has no relation to what
  the hostname will eventually be), with origin type "Storage static website"
  and the origin hostname set to whatever the storage account you just
  created is.
* Don't fill anything in for "origin path" unless you have a specific (and
  weird) use case. See [here](rationale-and-motivations.md#web) for discussion.
* Leave everything else at defaults.

Now, go into the DNS zone you want to add the domain names to. For each
endpoint that you made:
* Add A and AAAA records that are "alias" records, which point to the
  CDN endpoints you just made.

Next, go back into the CDN profile, and for each endpoint:
* Add a "custom domain", and type in the correct DNS name for that endpoint
* Go into the custom domain you just created
* Turn on HTTPS
* Select "Use my own certificate" (see
  [discussion](rationale-and-motivations.md#https) as to why)
* Choose a Key Vault
* Choose a certificate
* Choose a version (pick the latest one)

Now the endpoints are functional, we just have to set up any redirect rules
you want. You do that by going into the endpoint, clicking "Rules Engine",
and either editing the global rule, or adding a custom rule.
* If you don't have any condition (i.e. if you always want to redirect
  no matter what) then use the global rule, and add an action to it.
* If you want to add a condition (e.g. "protocol != HTTPS") then click Add
  Rule and fill in both the condition and the action.

For URL Redirect rules, any fields you leave blank will be kept the same.
For example, if you fill in the hostname but not the protocol, then the
protocol (HTTP/HTTPS) will be the same as the request was. Or, if you
fill in the protocol but not the hostname, then the hostname will be the
same as the request was.

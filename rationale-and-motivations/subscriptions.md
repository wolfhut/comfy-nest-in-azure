# Subscriptions (and when to use 'em)

I think of Subscriptions as basically being equivalent to "I am setting this
up on behalf of so-and-so".

For example, the web sites I maintain as memorials for my late father, are
running inside their own Subscription, that has his stuff in it, and only his
stuff. His authoritative DNS zones, his storage account, his CDN, his HTTPS
certificates, his access logs.

Or, I also have a Subscription that is for my personal noodling around,
where if I fat-finger something there's no chance of me messing up
house-infrastructure stuff.

I find that that helps me avoid situations where stuff leaks together
in unpleasant ways. I probably don't want my father's web site access
logs to get mixed in with the syslogs from my VMs, for instance. But
since they're partitioned off in their own Subscription, I pay for
whatever resources they consume but other than that they just sit
there and do their thing.

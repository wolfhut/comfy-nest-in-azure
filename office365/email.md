# Office 365: Email

Email (aka Exchange 365, aka whatever else they're calling it this week)
kinda-sorta works "out of the box", but there are a few things that are
very broken and which you have to change from the defaults in order to
make it usable.

## Spoofing protection

Microsoft keeps changing what they call this. Currently this setting seems to
be hiding inside the anti-phishing setting, as a tiny checkbox that you
might otherwise have overlooked.

It is *critical* that you turn off this so-called "spoofing protection".
The false positive rate is nothing short of astounding. There's no good way to
tune it or make it less rabid, you just have to turn it off completely.

If you're seeing emails that are clearly, obviously not spam, but they
have a spam score of 7, this might be why. Check the spoof protection settings
first, before you go messing with spam score thresholds.

## Antispam (undoing it the broken default way)

By default, it comes in a mode where, after the spam score and bulk
score and phishing score are calculated for an email, there are
actually *two* different parts of Exchange that *both* try to take
action based on the spam score. One of them is the antispam rules that
you can see plainly in the admin interface. That's the obvious one. The other
one is a thing that is never mentioned in the documentation except
elliptically in passing, and it's very hard to find any information
about it (the second one). The key word to look for is
"MailboxJunkEmailConfiguration".

See [this
page](https://docs.microsoft.com/en-us/microsoft-365/security/office-365-security/configure-your-spam-filter-policies)
and also [this
page](https://docs.microsoft.com/en-us/microsoft-365/security/office-365-security/configure-junk-email-settings-on-exo-mailboxes).

Where this gets fun is if they conflict with each other. The
"MailboxJunkEmailConfiguration" one will always win, if there is a conflict,
which is maddening and mysterious if you don't realize that it's there,
because you're changing settings in the one you *do* know about (the
regular antispam settings in the admin interface) and nothing is happening,
the behavior isn't changing.

And you start to doubt your sanity, because you can clearly see the settings
you're changing, but the behavior never changes no matter what you set the
settings in the admin interface to...

So here's what you have to do to start untangling this. You have to log into
Azure Cloud Shell (Powershell). You can refer to [this
page](https://docs.microsoft.com/en-us/powershell/exchange/exchange-online/connect-to-exchange-online-powershell/connect-to-exchange-online-powershell)
for help with logging in and installing the modules.

The goal is to set the `MailboxJunkEmailConfiguration` parameter to false,
for every mailbox on the system. This will make it so that each user
can decide for themselves what kind of policy they want to apply, rather
than having a non-configurable, non-tunable part of the system just make
the decision for them in an unfriendly way.

Here's a command that will do that, assuming you're all logged in and
stuff.

```
Get-Mailbox -RecipientTypeDetails UserMailbox -ResultSize Unlimited | foreach {Set-MailboxJunkEmailConfiguration $_.Name -Enabled $false}
```

To check whether all existing mailboxes have it set to false:

```
Get-Mailbox -RecipientTypeDetails UserMailbox -ResultSize Unlimited | foreach {Get-MailboxJunkEmailConfiguration $_.Name}
```

## Antispam (redoing it in a better way, step 1 of 2)

Find the normal Exchange antispam stuff in the admin interface. This might be
[here](https://protection.office.com/antispam), at least if Microsoft hasn't
changed anything recently.

Edit the default spam filter policy (no need to create a new policy, just
edit the default one).

For Spam, High-Confidence Spam, Phishing, and Bulk Email, chose the "add
X-header" option, and pick a header name that will be unique. (I have a UUID in
there so that I know it'll be unique).

High-Confidence Phishing doesn't support an "add X-header" action, so
your second-best choice is to quarantine it. Shrug. That's probably fine too.

I have my bulk threshold set at 3. I'm not sure what the default is, but
that's what mine is set to right now and it seems to be doing pretty good.

## Antispam (redoing it in a better way, step 2 of 2)

Now that you've done the above, Exchange will actually be operating in a
pretty sane way. It won't be going false-positive crazy because of
spoofing protection; and it won't have the weird "two conflicting places
where actions get taken based on spam score" thing. It will be adding a
header if it thinks a message is spammy or "bad" in some way.

Now, as an individual user, this gives you a lot of control. You can
go into the regular user-facing interface (not the administrative interface),
and configure your own server-side mail filtering rules, for yourself as an
individual, which can do things like:
1. If the List-ID is foo@example.com, file it in the foo folder
2. Else if the List-ID is bar@example.com, file it in the bar folder
3. Else if the spam header is present, file it in the junk folder
4. Else, deliver to inbox

That way of doing it also neatly bypasses spam detection for mailing
lists; if a message came through a mailing list, you *will* get it in
the correct folder. It's only if it falls through all the other rules,
and *would* have gone into your inbox, that it might go to junk instead.

# Office 365: Sharepoint

Sharepoint mostly seems to "do what it says on the wrapper".

The only thing I've run into so far where I was able to shoot myself in the
foot and I couldn't fix what I'd done, was where I deleted the root level
page, thinking I could just make a new one.

Oops. Big mistake. If you delete the root level page, you brick the whole
thing. You can't "just make a new root page" because the whole thing is
just broken.

And you also can't undelete what you deleted, because when you get into
this situation, even the normal mechanism for undeleting pages doesn't work.

If you get into that state, here's how you get out of it:
1. Install [Sharepoint Management
Shell](https://www.microsoft.com/en-us/download/details.aspx?id=35588). Yes,
   you do actually need to install this tool on a real Windows machine.
   Azure Cloud Shell isn't good enough. :-(
2. Read [the
   documentation](https://docs.microsoft.com/en-us/powershell/sharepoint/sharepoint-online/introduction-sharepoint-online-management-shell)
3. Look up how to use the `Get-SPODeletedSite` and `Restore-SPODeletedSite`
   commands

[This blog
post](https://www.adamfowlerit.com/2017/06/recover-sharepoint-online-site/)
may also help.

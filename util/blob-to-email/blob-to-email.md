# Blob to email

This describes how to set up an Azure Function app which will send out
email whenever a file is uploaded in a blob container, along with a
signed URL

## Prerequisites

1. A Storage Account, of type GRS, with a container in it that you want to
   monitor
   - ZRS and GZRS are not supported by Azure Functions unfortunately. (LRS is
     supported, but why would you bother?)
2. A Log Analytics workspace to send the logs to

## Procedure

Create a new Function App:
* Runtime stack: `.NET`, latest version
* Attach it to the storage account you created
* Operating System: Windows
* Do *not* let it set up Application Insights!!

Go into the Function app, choose Diagnostic Settings, add a Diagnostic
Setting to send `FunctionAppLogs` data to your Log Analytics Workspace.

Go into Configuration, and add the following new Application Settings:

* `CONTAINER`: The name of the container, within the storage account, that
  is being monitored
* `O365_EMAIL_ADDR`: The Office 365 email address you're sending from
* `O365_EMAIL_PASSWD`: The password for the above-mentioned Office 365 account
* `TO_ADDRS`: Comma-separated email addresses to send notifications to
* `BCC_ADDRS`: Comma-separated email addresses to send notifications to, as bcc
* `GREETING`: A string like perhaps `New blobs for your attention`
* `LINK_VALIDITY_DAYS`: How many days the link given in the email should be
  valid for, e.g. `365`, or `180`

Both `TO_ADDRS` and `BCC_ADDRS` are optional, but you really should specify
at least one, otherwise it's kind of pointless!

Create a new Function:
* Type: blob trigger
* Name it whatever you want, but perhaps "notifier" or something.
* Don't worry about the "path" field, we'll overwrite that later

Go into the new function, choose "Code + Test":
* Upload the [function.proj](function.proj) file. This will cause it to
  install the MailKit and Azure Storage modules from NuGet.
  - You can't do this by copying and pasting. You can only copy/paste what
    is already existing, inside the Function. There is no `function.proj`
    file yet, so you have to upload it.
* Paste the contents of [function.json](function.json) and [run.csx](run.csx)
  overwriting the versions in the Function.
* Click Save.

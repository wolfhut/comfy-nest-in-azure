# VoIP

Prerequisites:
1. [(doc)](../util/blob-to-email) A storage account for holding voice mail,
   with a blob container and an Azure Function for sending out notifications
2. [(doc)](../virtual-machines/virtual-machines.md) A virtual machine to run
   Asterisk on
   * It needs to be in a security group where TCP port 5061 (for SIP) and UDP
     ports 1024-65535 (for RTP) are allowed inbound
3. The VM needs to have a [User-Assigned Managed
   Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal).
   *Save the client ID.* You will need it later.
4. That Managed Identity needs to be assigned the Storage Blob Data Contributor
   role in the voicemail blob storage container
5. The VM needs to have static *private* IPv4 and IPv6 addresses. This is
   because, [per the Asterisk
   docs](https://wiki.asterisk.org/wiki/display/AST/PJSIP+Transport+Selection),
   Asterisk works better if the addresses you give it to bind to, in the
   transport configuration, are not wildcard addresses. So it needs to know
   the exact private IP addresses to bind to.
6. The VM needs to have a public IPv4 and IPv6 address attached to it.
   (Standard-sku, because VMs don't work with basic-sku)
   - I think it's even more important than usual to set the timeouts all the
     way up. You can't do that from the creation screen, but you can do it by
     editing them afterwards.
7. The VM needs to have [the .net
   SDK](https://docs.microsoft.com/en-us/dotnet/core/install/) installed on it.
   This is so that you can build the voicemail uploader utility. See below.

## Setting up the cron job to upload voicemails to Azure Storage

This step assumes that voicemails are being written, in wav format, to the
directory `/var/spool/asterisk-voicemail`. If that isn't the case then
that's still fine, but you have to adjust the command-line arguments since
those are no longer matching the defaults.

First, build [voicemailUploader](voicemail-uploader/) on the VM, and copy the
binary to `/usr/local/bin/voicemailUploader`.

```
sudo crontab -e -u asterisk
```

Add a line:
```
* * * * * /usr/local/bin/voicemailUploader --auth-client-id 6f839c09-ee81-48d6-8eae-fae652b7df02 --storage-account my_storage_account_name > /dev/null 2>&1
```

The client ID should be the client ID of the VM's User-Assigned Managed
Identity. It's needed because there's also a System-Assigned Managed
Identity, and when you get a token, the metadata server doesn't know which
Managed Identity to feed you. So you have to tell it explicitly which one
you want.

The storage account name should be the name of the storage account you set
up. By default the uploader uses the container named "voicemail"; that can
be overridden on the command line.

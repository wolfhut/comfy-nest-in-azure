using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

static readonly string containerName =
    Environment.GetEnvironmentVariable("CONTAINER");
static readonly BlobServiceClient blobServiceClient =
    new BlobServiceClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
static readonly BlobContainerClient blobContainerClient =
    blobServiceClient.GetBlobContainerClient(containerName);
static readonly int linkValidityDays =
    int.Parse(Environment.GetEnvironmentVariable("LINK_VALIDITY_DAYS"));
static readonly string toAddrsCommaSeparated =
    Environment.GetEnvironmentVariable("TO_ADDRS");
static readonly string bccAddrsCommaSeparated =
    Environment.GetEnvironmentVariable("BCC_ADDRS");
static readonly string greeting =
    Environment.GetEnvironmentVariable("GREETING");
static readonly string overrideContentDisposition =
    Environment.GetEnvironmentVariable("OVERRIDE_CONTENT_DISPOSITION");

const string o365ServerName = "smtp.office365.com";
const int o365ServerPort = 587;
static readonly string o365EmailAddr =
    Environment.GetEnvironmentVariable("O365_EMAIL_ADDR");
static readonly string o365EmailPasswd =
    Environment.GetEnvironmentVariable("O365_EMAIL_PASSWD");

public static IList<string> splitEmailAddrs(string s)
{
    var addrs = new List<string>();
    if (s == null) {
        return addrs;
    }

    foreach (var addr in s.Split(',')) {
        string trimmedAddr = addr.Trim();
        if (trimmedAddr.Length > 0) {
            addrs.Add(trimmedAddr);
        }
    }

    return addrs;
}
// It would be nice if we could get it to not give us blobData, just path.
// I don't know of a way. So we just ignore blobData.
public static async Task Run(Stream blobData, string path, ILogger log)
{
    BlobClient blobClient = blobContainerClient.GetBlobClient(path);
    var blobSasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = containerName,
            BlobName = path,
            Resource = "b", // "Specify 'b' if the shared resource is a blob"
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(linkValidityDays),
        };
    blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
    if (overrideContentDisposition != null) {
        blobSasBuilder.ContentDisposition = overrideContentDisposition;
    }
    Uri uri = blobClient.GenerateSasUri(blobSasBuilder);

    log.LogInformation($"Sending notification for {uri}");

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("", o365EmailAddr));
    foreach (var addr in splitEmailAddrs(toAddrsCommaSeparated)) {
        message.To.Add(new MailboxAddress("", addr));
    }
    foreach (var addr in splitEmailAddrs(bccAddrsCommaSeparated)) {
        message.Bcc.Add(new MailboxAddress("", addr));
    }
    message.Subject = greeting;
    message.Body = new TextPart("plain")
    {
        Text = $"{greeting}:\r\n{uri}\r\n"
    };

    using (var client = new SmtpClient()) {
        await client.ConnectAsync(o365ServerName, o365ServerPort,
                                  SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(o365EmailAddr, o365EmailPasswd);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    log.LogInformation("Sent successfully");
}

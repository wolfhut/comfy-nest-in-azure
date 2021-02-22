using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Mkcfg
{
    class MkcfgCommand
    {
        // We have to keep in mind that this regex is going to be matched
        // against full pathnames, not just the basename.
        public static readonly Regex claimedFileRegex =
            new Regex(@"/([0-9]+)-([0-9]+)-[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.[^/]*$",
                RegexOptions.Compiled);

        public class VoicemailUploadFlags
        {
            public string AuthClientId { get; set; }
            public string Directory { get; set; }
            public string FileExtension { get; set; }
            public string MimeType { get; set; }
            public string StorageAccount { get; set; }
            public string Container { get; set; }
            public int MinQuiescentSeconds { get; set; }
            public int RetryAfterSeconds { get; set; }
        }
        public static async Task<int> doVoicemailUpload(
            VoicemailUploadFlags flags)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var credential = new ManagedIdentityCredential(flags.AuthClientId);
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{flags.StorageAccount}.blob.core.windows.net"),
                credential);
            var blobContainerClient =
                blobServiceClient.GetBlobContainerClient(flags.Container);
            string requiredSuffixLowercase =
                "." + flags.FileExtension.ToLower();
            long maxClaimedTimeToRetry = now - flags.RetryAfterSeconds;
            long maxModTimeToUpload = now - flags.MinQuiescentSeconds;
            int numFilesInDir = 0;

            var tasks = new List<Task>();
            foreach (var pathName in
                     Directory.EnumerateFiles(flags.Directory)) {
                // We increment the count even on files that don't match. This
                // is so that if there's ever a misconfiguration and the files
                // being written have a different extension than what we're
                // looking for, that someone will notice and fix it.
                numFilesInDir++;
                if (!pathName.ToLower().EndsWith(requiredSuffixLowercase)) {
                    continue;
                }
                DateTimeOffset voicemailTime;
                var m = claimedFileRegex.Match(pathName);
                if (m.Success) {
                    if (long.Parse(m.Groups[2].Value) >
                        maxClaimedTimeToRetry) {
                        continue;
                    }
                    voicemailTime =
                        DateTimeOffset.FromUnixTimeSeconds(
                            long.Parse(m.Groups[1].Value));
                } else {
                    voicemailTime =
                        new DateTimeOffset(File.GetLastWriteTimeUtc(pathName));
                    // If File.GetLastWriteTimeUtc() fails, it doesn't throw
                    // an exception, it just returns a garbage date. This is
                    // documented behavior! I'm not making this up, I swear!
                    if (voicemailTime.Year < 1000) {
                        Console.WriteLine($"{pathName}: already claimed");
                        continue;
                    } else if (voicemailTime.ToUnixTimeSeconds() >
                               maxModTimeToUpload) {
                        continue;
                    }
                }
                tasks.Add(doOneVoicemail(flags, pathName, now, voicemailTime,
                                         blobContainerClient));
            }

            foreach (var task in tasks) {
                await task;
            }

            return 0;
        }

        public static async Task doOneVoicemail(
            VoicemailUploadFlags flags, string oldPathName,
            long now, DateTimeOffset voicemailTime,
            BlobContainerClient blobContainerClient)
        {
            Guid uuid = Guid.NewGuid();
            string uuidStr = uuid.ToString();
            string newPathName =
                Path.Combine(
                    flags.Directory,
                    $"{voicemailTime.ToUnixTimeSeconds()}-{now}-{uuidStr}.{flags.FileExtension}");
            try {
                File.Move(oldPathName, newPathName);
            } catch (FileNotFoundException ex) {
                Console.WriteLine($"{oldPathName}: already claimed");
                return;
            }
            string fileNameInAzure =
                $"{voicemailTime.ToString("yyyyMMdd-HHmmss")}-{uuidStr}.{flags.FileExtension}";

            BlobUploadOptions blobUploadOptions = new BlobUploadOptions();
            blobUploadOptions.HttpHeaders = new BlobHttpHeaders();
            blobUploadOptions.HttpHeaders.ContentType = flags.MimeType;

            BlobClient blobClient =
                blobContainerClient.GetBlobClient(fileNameInAzure);
            using (var f = File.OpenRead(newPathName)) {
                await blobClient.UploadAsync(f, blobUploadOptions);
                File.Delete(newPathName);
            }
        }

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Description =
                "Upload voicemail from Asterisk to Azure Blob Storage";

            rootCommand.Add(
                new Option<string>(
                        "--auth-client-id",
                        description: "Client ID of the Managed Identity to authenticate as"));
            rootCommand.Add(
                new Option<string>(
                        new string[] { "--directory", "-d" },
                        getDefaultValue: () => "/var/spool/asterisk-voicemail",
                        description: "Directory to upload voicemail from"));
            rootCommand.Add(
                new Option<string>(
                        "--file-extension",
                        getDefaultValue: () => "wav",
                        description: "File extension to look for"));
            rootCommand.Add(
                new Option<string>(
                        "--mime-type",
                        getDefaultValue: () => "audio/wav",
                        description: "MIME type to upload as"));
            rootCommand.Add(
                new Option<string>(
                        "--storage-account",
                        description: "Name of storage account to upload to"));
            rootCommand.Add(
                new Option<string>(
                        "--container",
                        getDefaultValue: () => "voicemail",
                        description: "Name of container within storage account"));
            rootCommand.Add(
                new Option<int>(
                        "--min-quiescent-seconds",
                        getDefaultValue: () => 60,
                        description: "Seconds of quiescence after which we can start an upload"));
            rootCommand.Add(
                new Option<int>(
                        "--retry-after-seconds",
                        getDefaultValue: () => 3600,
                        description: "Seconds after which we can retry"));

            rootCommand.Handler =
                CommandHandler.Create<VoicemailUploadFlags>(doVoicemailUpload);

            return await rootCommand.InvokeAsync(args);
        }
    }
}

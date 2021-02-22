# Voicemail Uploader

This is a thing intended to be run from cron, which uploads any wav files
from a directory, to Azure Blob Storage, and then deletes them.

## Building

1. [Install the .net
   SDK](https://docs.microsoft.com/en-us/dotnet/core/install/)
2. Edit [voicemailUploader.csproj](voicemailUploader.csproj)
   and change the `RuntimeIdentifier` to one of the [valid
   RIDs](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog). This sets
   the target platform and architecture for the build. The .net tools cannot
   autodetect this. You have to explicitly tell it what to target for building.
   - NOTE: Remember to *not* commit this change to the repo. The change is
     valid only for you, based on your choice of target platform.
3. Run `dotnet publish` to actually do the build.
4. Find where it put your binary:
   - `find bin -type d -name publish -print`

### If you ever need to regenerate the [voicemailUploader.csproj](voicemailUploader.csproj) file

You may never have to do this. But if you do, there's no magic involved and
it's actually pretty straightforward. Here is how it was originally generated.

1. Run `dotnet new console`
2. Add the things we need from Nuget:
   - `dotnet add package System.CommandLine`
   - `dotnet add package Azure.Identity`
   - `dotnet add package Azure.Storage.Blobs`

   Either that, or you can just add a `<PackageReference>` for it directly in
   [voicemailUploader.csproj](voicemailUploader.csproj)
   underneath `<ItemGroup>`.
3. Edit [voicemailUploader.csproj](voicemailUploader.csproj).
   Inside the `<PropertyGroup>`, leave the `<OutputType>` and
   `<TargetFramework>` alone, but also add:
   ```
        <RuntimeIdentifier>REPLACE_ME</RuntimeIdentifier>
        <PublishSingleFile>true</PublishSingleFile>
        <PublishReadyToRun>true</PublishReadyToRun>
        <DebugType>embedded</DebugType>
        <SelfContained>true</SelfContained>
   ```
   See [this
   page](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file)
   for more details.

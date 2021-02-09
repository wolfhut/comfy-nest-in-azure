# Monch

Monch stands for Monitoring Checker. It is designed to be run from cron.
It knows how to do checks against various things, and publish the results
as [custom
metrics](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/metrics-custom-overview)
in Azure.

It is written in C#.

## Building

Step 1: VERY IMPORTANT: Edit [monch.csproj](monch.csproj)
and change the `RuntimeIdentifier` to one of the [valid
RIDs](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog). The .net
tools cannot autodetect this. You have to set it every time you want to
build.

Step 2: Run `dotnet publish`

Step 3: Find where it put your binary:
* `find bin -type d -name publish -print`

### If you ever need to regenerate the [monch.csproj](monch.csproj) file

Here is how it was originally generated.

1. Run `dotnet new console`
2. Add the needed Nuget dependencies:
   - `dotnet add package System.CommandLine` - see
     [docs](https://github.com/dotnet/command-line-api)
   - `dotnet add package DnsClient` - see
     [docs](https://dnsclient.michaco.net/)
3. Edit [monch.csproj](monch.csproj).
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

# Key Vault

This is assuming that the main reason to create a key vault is to provide
keys and X.509 certificates to Azure CDN.

## Prerequisites:

* The Powershell command:
  ```
  New-AzADServicePrincipal -ApplicationId "205478c0-bd83-4e1b-a9d6-db63a3e1e1c8"
  ```
  should have been run at least once. (This is one-time setup and does not
  need to be done every time.) If you miss this step, you will not see
  "Microsoft.Azure.Cdn" as an option during the setup steps below, and that
  will be your cue that you need to go and do it.

## When creating a key vault

On the Basics tab:
* There's no particular reason to choose premium. Standard is just fine.

On the Access Policy tab:
* Supposedly role-based is better, but I wasn't able to figure it out.
  The default is "vault access policy" and that seems to work fine.
* Click "Add Access Policy". Under where it says "secret permissions",
  choose "get". Under where it says "select principal", enter
  "Microsoft.Azure.Cdn" into the search box. Then click Add.

On the Networking tab:
* Accept all the defaults

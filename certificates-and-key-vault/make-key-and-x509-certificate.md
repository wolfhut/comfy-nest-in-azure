# Make a key and X.509 certificate in a Key Vault

Weirdly enough, you *don't* go to the "Keys" part of the Key Vault. This
all happens in the "Certificates" part of the Key Vault, even the parts
that relate to keys.

The rest of this document assumes that you are in the "Certificates" part
of the Key Vault.

## Configure contacts

The first step is to make sure contacts are configured. These are email
addresses that will receive notifications when certificates are about to
expire.

From the "Certificates" part of the Key Vault, click "Certificate Contacts"
at the top of the screen. Make sure there are valid email addresses in
there.

## Generate a key and CSR

From the "Certificates" part of the Key Vault, click "Generate/Import":
* Method of Certificate Creation: `Generate`
* Certificate Name: this can be any sensible string, it doesn't relate to
  the DN or the SANs
* Type of Certificate Authority: `Certificate issued by a non-integrated CA`
* Subject: As the hint says, this is the DN you want to put in the CSR.
  For example: `example.com`
* DNS Names: These will be in the SAN part of the CSR. The CA will probably
  ignore them and give you what it wants to give you, regardless of what you
  asked for. But you can fill them in anyway if you're being optimistic.
* Validity period: Nobody will give you more than 12 months, but you can put
  120 or something if you want to be optimistic.
* Content type: For Azure CDN purposes, PKCS#12 is fine.

Click Create.

That will cause the certificate to appear, in a "disabled" state. Don't
panic. Disabled just means you haven't gotten the CSR signed yet.

Click on the certificate entry (in the disabled state), then click
"Certificate Operation". That gets you to a screen where you can
download the CSR.

## Adding the signed certificate

You will need to prepare a PEM file on your local machine. Azure doesn't
support just giving you a place to paste it.

The PEM file you create should have the end-entity certificate first in
the file, followed by any intermediate certificates.

Click on the certificate entry (in the disabled state), then click
"Certificate Operation".

Click "Merge Signed Request". Choose the PEM file to upload.

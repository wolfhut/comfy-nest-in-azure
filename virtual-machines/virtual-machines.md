# Virtual machines

## Prerequisites

* AAD: there should be at least one user with the "Virtual Machine
  Administrator Login" role. (And/or the "Virtual Machine User Login"
  role)
* Networks: There should be a network in the region you want to deploy in,
  and it should have a subnet, and the subnet should have a security group.
* [(doc)](log-analytics-one-time-setup.md) There should be a Log Analytics
  workspace already created, and there should be a monitoring data collection
  rule already created, that sends syslogs and performance metrics to that
  workspace.

## Creating the VM

In the portal, use the "Create a virtual machine" workflow.

On the Basics tab:
* Choose Ubuntu, latest version
* Choose B1ls instance size
* Choose password authentication. Leave the username at the default which is
  "azureuser", but type in a garbage password (you'll be deleting the local
  account anyway, and using AAD authentication). You don't even need to be able
  to remember what garbage you typed in. It could be "mashing on the keyboard"
  type garbage.
* Don't allow any ports yet. (Choose "None"). That part will be inherited
  from the security group in whichever subnet you put it in.

On the Disks tab:
* Use Standard HDD.

On the Networking tab:
* Choose the network and subnet.
* Don't give it a public IP. You can do that later if it needs one.
* Don't override the security group (set to None). You want it to inherit
  the security group from the subnet.

On the Management tab:
* Check the "Login with AAD credentials" checkbox.

On the Advanced tab, paste the following into the "Custom Data" field:
```
#cloud-config
disk_setup:
  ephemeral0:
    table_type: mbr
    layout: [[100, 82]]
    overwrite: true
fs_setup:
  - device: ephemeral0.1
    filesystem: swap
mounts:
  - ["ephemeral0.1", "none", "swap", "sw", "0", "0"]
```

Explanation for humans of what the above does:
* The `disk_setup` block makes a partition table on the scratch disk. The "100"
  means use 100 percent of the disk, and the "82" is the Linux partition type
  for swap.
* The `fs_setup` block refers to partition 1 on the scratch disk, and writes
  the correct magic number to it so that Linux will be okay treating it as a
  swap partition.
* The `mounts` block adds the right stuff to `/etc/fstab`.

## Enabling IPv6

It's best to do this step before getting too much further along in the process.

You'll need the resource id, not of the VM, but of the VM's network interface.
This is a gross-looking string with lots of slashes and path components. It is
visible in the Properties tab of the network interface resource, in the portal.

Now use the portal's "Cloud Shell" feature to get a Powershell window, and
type these commands (substituting the correct resource id in the first line).

```
$rsrc_id = "/subscriptions/5b4361e3-4701-4cf5-84cb-22c0612c2fb7/resourceGroups/foo/providers/Microsoft.Network/networkInterfaces/bar1234"
$if = Get-AzNetworkInterface -ResourceId $rsrc_id
$existing_ipc = Get-AzNetworkInterfaceIpConfig -NetworkInterface $if -Name ipconfig1
$if | Add-AzNetworkInterfaceIpConfig -Name ipconfig_ipv6 -PrivateIpAddressVersion IPv6 -Subnet $existing_ipc.Subnet | Set-AzNetworkInterface
```

Basically what this is doing, is:
* load the network interface object into memory
* Get the ip config named "ipconfig1" out of it (this is the Azure default
  IPv4 one)
* Add a new ip config to it, called "ipconfig_ipv6", copying the subnet from
  the old ip config, but this one will be IPv6
* save the changes back up into Azure

## Post-setup

Log into the jumphost (or if this machine is going to be the jumphost and
you've given it a public IP or added it to a load balancer for the purpose,
then that's okay too!)

Log into the VM as a user which has the "Virtual Machine Administrator Login"
role in AAD. This will look something like this:

```
ssh -l my@email.address.in.aad name-of-vm
```

Double check that the swap got added okay:
```
> sudo swapon --show
NAME      TYPE      SIZE  USED PRIO
/dev/sda1 partition   4G 15.6M   -2
>
```

> Note regarding the above: I have tested doing a "redeploy" which causes
> the data on the temporary disk to be lost. I was initially worried that
> the one-time mkswap done by cloudinit would not be re-run, and the swap
> would fail to be added after redeploy. Not so -- it came through with
> flying colors. So I believe this should work, but it's probably worth
> just a quick check after setup.

Do any updates needed:
```
sudo apt update
sudo apt full-upgrade
sudo apt autoremove
```

(If it asks during upgrade whether you want to run grub-install, say yes, and
choose all disks, but only at the top-level. Don't do the partition-level.
And if this makes no sense, that's fine, don't worry about it. It probably
won't ask you that. But if it does, that's what to do.)

Install some basic packages (note that the "nvi" package will provide
a symlink that makes "vi" work, after the "vim" package has been removed):

```
sudo apt remove vim
sudo apt-mark hold vim
sudo apt install nvi tcsh manpages man-db telnet whois dnsutils binutils
sudo apt autoremove
```

Now find the appropriate data collection rule from the [set of rules you
already created](log-analytics-one-time-setup.md) and add the new VM as
a resource. This will have the effect of installing packages, so you don't
want to be doing anything else to the VM while this is happening.

Note that this is Azure Monitor Agent, which is "the new way", which replaces
"the old way" which was Log Analytics Agent, also known as OMS agent. [This
page](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/azure-monitor-agent-overview)
explains that Azure Monitor Agent supersedes the previous ones.

While you're waiting for that to quiesce, there's one other cleanup task
to take care of, which is the "azureuser" user, which isn't needed because
we enabled Azure Active Directory integration.

```
sudo deluser azureuser
```

After you're pretty sure things are quiescent, you can reboot. That should
pick up the IPv6 interface as well as whatever else needed a reboot to take
effect.

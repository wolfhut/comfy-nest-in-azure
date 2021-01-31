# No ARM, No Puppet, No Terraform

This is an attempt to explain why I'm so big on doing everything through
the Azure portal. I know there's awesome technologies out there to make
creating cloud resources into something that is repeatable, reliable,
and easier to reason about than a system that has been set up by hand.

These technologies include [Microsoft's own
ARM](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/),
Puppet, Terraform, etc.

All of these are awesome technologies. If I wanted to set up hundreds or
even dozens of identical widgets, I would be totally all over them. If I
were a big company, I would be all over them.

For my use case, as "just some guy who got tired of running a machine in
his garage", I don't think they make sense. Here's why:

1. A big company would get much more use out of their economies-of-scale than
   I will. If you're setting up 20 machines it's worth spending more time
   up-front on automation than if you're setting up 2.
2. A big company probably has some amount of infrastructure (such as a CI/CD
   system) already set up. For them, that's a sunk cost. Being able to
   use it for things like deployments to a cloud, is a win because it takes
   advantage of infrastructure that already exists. I don't. Let's say I get
   a new laptop. I reinstall, everything is different now. I don't have any
   of the same tools I used to. But if my workflow for doing stuff in Azure
   is "log into the Azure web site and do stuff" then I'm still just fine.
   Doing it through the web portal ensures that, as long as I have a web
   browser and an ssh client, I will never be without the tools I need to
   maintain my infrastructure.
3. Stuff changes in Azure. Like, *all the time.* Like, I set up my first
   stuff in 2020, and as of the time of this writing it's now 2021, and
   cloudinit is more mature now, and there's a whole other log analytics
   extension, and oh hey AAD authentication works now, whereas it didn't
   used to.

   I care about staying abreast of stuff like that. I don't want it to be
   2022 and I just apply the same ARM template that worked for me in 2021,
   except now I'm using deprecated stuff and I don't even realize I'm using
   deprecated stuff.

   I want to go through the process every year, I want to sand everything down
   with fine-grit emory paper and make sure everything's shiny and smooth
   and my processes are up-to-date. I actually appreciate the chance to take
   stock of my processes and see if there's a new recommended way of doing
   something.

So, my usage of Azure is going to be whatever I can do through the
portal, and by ssh'ing into my VMs. Luckily, the Azure portal is awesome
and it even includes things like Cloud Shell, which basically gives you
a Powershell terminal, already authenticated with your credentials, right
there in the web browser.

# Old way of configuring BIND

As discussed [here](../rationale-and-motivations.md#why-not-bind), I'm
using Unbound now, and I don't regret that decision. It was made for
valid reasons, at the time.

I will probably always prefer BIND, and it's not inconceivable that I might
switch back one day, if circumstances are different in the future.

Here's how I used to have BIND set up:

1. `sudo apt install bind9`
2. `sudo vi /etc/bind/named.conf.options`
   * Remove all listen-on's (you can leave ones that say "any", as they're
     harmless, but might as well delete them)
   * Add into the options section:
     ```
     allow-recursion { any; };
     max-cache-size 128M;
     ```

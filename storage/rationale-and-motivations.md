# Storage

## Redundancy options

[Here's](https://docs.microsoft.com/en-us/azure/storage/common/storage-redundancy)
what Microsoft has to say on the matter.

My perspective is:
1. Nix the RA-* options right from the gate. I'm concerned about durability
   first, and read/write availability second. If I don't have read/write
   availability, I really don't care about having read-only availability,
   especially since the RA-* options add a lot of weirdness and complexity.
2. My preference is ZRS or GZRS. GZRS is twice the cost of ZRS, and it also
   gives a lot more nines of durability. I think all the zones in a region
   going out at once is extremely unlikely; in my mind, ZRS is a very solid
   choice even by itself. I don't see ZRS as being "less than", in any way. I
   totally trust ZRS; but if it really has to be ultra-super-durable, then
   GZRS sounds awesome too.
3. I'm less likely to want LRS or GRS. Availability on those two will suffer
   if a single datacenter has an issue. My sense of things is that that's
   actually not a super unlikely thing to have happen. That said, some things
   such as Azure Functions don't support ZRS or GZRS. For those things, I'm
   likely to choose GRS. I would only choose LRS for data that I considered
   non-important.

## Soft delete/versioning options

(needs to be filled in)

## Deletion protection options

(needs to be filled in)

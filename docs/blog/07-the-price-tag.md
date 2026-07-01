### #7 — The "Price Tag" Revisited: What we might have gotten wrong
*Series: Building a real microservices application, brick by brick — Part I finale.* *Previous:* [#6 The SharedKernel](06-the-shared-kernel.md).

---

In the first five posts, I showed you a warehouse domain built on "pre-paid" modeling decisions. We talked about archetypes, bounded contexts, and a Shared Kernel that is so small it’s practically invisible [5]. But if you remember **Rule #2** of this series: **Every decision shows its price** [1].

Before we move to Part II and start wiring up the infrastructure, we need to take a cold, hard look at the "receipt" for the decisions we’ve made. As an architect, my job isn't just to pick patterns; it’s to admit where they might break under the weight of 10,000 pallets.

#### 1. The Microservices "Tax" (Decision #1)
We chose microservices from day one to force ourselves to deal with distributed system pains—outboxes, sagas, and network partitions—from the first commit [6].
*   **The Criticism:** In a real-world startup, this is often called **over-engineering**. By splitting into three physical services (Warehouse, Logistics, MasterData) now, we are paying for eventual consistency and contract tests before we even have a single customer. 
*   **The Risk:** If we misidentified a boundary—for instance, if *Inventory* and *Logistics* actually need to be one atomic transaction—moving that logic across a service boundary later is a **data migration nightmare**, not just a simple refactor [2].

#### 2. The "Fat" Topology Aggregate (Decision #3)
We modeled `WarehouseSite` (Topology) as a single aggregate to keep structural rules, like unique location codes within a site, trivially consistent [3, 7].
*   **The Criticism:** A warehouse with 10,000 locations—a standard size for many businesses—means loading a 10,000-node tree into memory just to change the capacity or temperature of one shelf is an **I/O and memory overhead**.
*   **The Risk:** While we’ve documented this as a "known refactoring point," in high-traffic master-data scenarios, this aggregate will cause performance degradation and potential **concurrency locks** on the database row for the entire warehouse [3].

#### 3. The "Yogurt Gap" (Decision #4)
We chose local replicas (Snapshots) instead of cross-service queries to keep the services decoupled [8].
*   **The Criticism:** The Inventory service validates a put-away against a *local copy* of the product's storage requirements.
*   **The Risk:** There is a window of **eventual consistency** (seconds) where the Catalog might have updated a product's storage requirement (e.g., from ambient to cold room), but the Inventory snapshot hasn't caught up [9]. Physically, this means a forklift operator could put a pallet of yogurt in a warm room because the "bouncer"—the `PutAwayPolicy`—was looking at old data [10, 11].

#### 4. The Shared Kernel vs. The DRY Reflex
We deliberately duplicated `LocationCode` and `Sku` grammar instead of sharing the types [4].
*   **The Criticism:** We are defining the same regex for a location code in two different services. This violates the DRY (Don't Repeat Yourself) principle that every developer learns early on.
*   **The Risk:** If the business decides to change the format of a location code, we have to **manually update two files**. If we forget one, the system won't fail at compile-time; it will fail at runtime when an integration event carries a string that one service can't parse [4, 12].

#### 5. Archetype Ceremony (Decision #3)
We used the `Quantity` value object and the `Party/Role` pattern to avoid "naive" models [13, 14].
*   **The Criticism:** This adds significant **indirection and "ceremony"** to the code. Every UI screen now has to understand that a "Supplier" is actually a `Party` playing a `SupplierRole` [15].
*   **The Risk:** We might be solving problems the business doesn't have yet. If we never have a company that is both a supplier and a customer—though the business says we already do—we’ve made our database queries and UI logic twice as complex for zero return [16].

---

> **Trade-off: Honesty over Perfection.** 
> We accept these risks because the alternative—a modular monolith with a "Common.dll" shared by everyone—leads to a different kind of death: the **distributed monolith** where nothing can ever change [17]. We’ve chosen "expensive to change boundaries" over "impossible to change code".

#### What's next
We’ve finished the "Whiteboard" phase. We know the domain, we’ve drawn the contexts, and we’ve admitted our sins. 

Next, [post #8](08-wrapping-up-part-i.md) closes Part I with a short retrospective. Then **Part II — From understanding to delivery** turns the whiteboard into a plan and a design ([story mapping](09-story-mapping-epics-and-tasks.md), the [architectural drivers](10-architectural-drivers.md), then the [design pass](11-design-nfr-adr-and-design-system.md)) — and only after that do we earn the right to talk about infrastructure, in the foundations build that continues Part II.
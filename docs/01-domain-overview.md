# Domain: Warehouse Management (WMS)

## 1. Vision

A warehouse management system (WMS) for a company operating **multiple warehouses**, each
consisting of **multiple rooms** (including temperature-controlled **cold rooms**). The system
covers the full lifecycle of goods:

```
Delivery announcement (ASN) → Goods Receipt → Put-away → Storage / Stock
→ Reservation → Picking → Packing → Dispatch
```

We are not building an ERP or a shop — **the core domain is precise knowledge of
what is stored where and in what quantity, plus the flow of goods through the warehouse.**

## 2. Subdomains

| Subdomain | Type | Rationale |
|---|---|---|
| **Inventory** (stock, movements, reservations) | **Core** | This is the business edge — accuracy of stock and locations |
| **Logistics** (inbound and outbound flows) | **Core** | The flow of goods is the second pillar of value |
| Product Catalog (product definitions) | Supporting | Necessary, but not a differentiator |
| Warehouse Topology (warehouses, rooms, locations) | Supporting | Physical structure, changes rarely |
| Partners (suppliers, customers, carriers) | Generic | Classic Party problem — a well-known, reusable pattern |
| Identity & Access | Generic | Off-the-shelf solution (e.g. Keycloak / ASP.NET Core Identity) |

## 3. Actors (our clients / users)

| Actor | Who they are | What they need |
|---|---|---|
| **Warehouse Manager** | Runs a single warehouse | Topology, stock reports, stocktakes, adjustments |
| **Warehouse Operator** | Operational worker (scanner/terminal) | Simple tasks: receive, put away, pick, pack |
| **Forklift Operator** | Moves pallets | Put-away tasks and location-to-location transfers |
| **Logistics Coordinator** | Plans deliveries and shipments | ASNs, dock slots, inbound/outbound statuses |
| **Quality Inspector** | Inspects deliveries | Blocking/releasing batches of goods |
| **Supplier** (external) | Ships goods to us | Announces deliveries (ASN), sees receipt status |
| **Customer / Consignee** (external) | Orders goods | Places outbound orders, tracks shipments |
| **Carrier** (external) | Transports goods | Picks up shipments, confirms collection |
| **External system** (ERP / e-commerce) | Integration | API/events: stock levels, orders, confirmations |

## 4. Ubiquitous Language (glossary)

| Term | Definition |
|---|---|
| **Warehouse** | Physical facility at a single address; has rooms and docks |
| **Room** | Separated part of a warehouse; has a type (standard, **cold room**, freezer, hazmat zone) and environmental parameters |
| **Location** | Smallest addressable storage place (rack/shelf/bin) with capacity and load limit |
| **ProductType** | Product definition: name, SKU, dimensions, weight, category, storage requirements |
| **Batch / Lot** | Units of a product from a single delivery/production run, with an expiry date |
| **Stock Item** | Quantity of a specific product (and batch) at a specific location |
| **Reservation** | Quantity set aside for an outbound order; `available = on hand − reserved` |
| **ASN** (Advance Shipping Notice) | Supplier's announcement of a delivery (what, how much, when) |
| **Goods Receipt** | The fact of receiving goods at the dock — compared against the ASN |
| **Put-away** | Moving received goods from the dock buffer to target storage locations |
| **Pick List** | Tasks to collect goods for an outbound order |
| **Shipment** | Picked and packed goods handed over to a carrier |
| **Stock Movement** | Every change of quantity/place — an immutable record (ledger) |
| **Stocktake** | Counting actual stock and reconciling differences |

## 5. Key business rules (invariants)

1. **Environment compatibility:** a product with a refrigeration requirement may only be stored
   in a location whose room maintains a compatible temperature range (e.g. dairy 2–6 °C → cold room only).
2. **Location capacity:** total volume and weight of goods at a location ≤ the location's capacity/load limit.
3. **Non-negative stock:** quantity at any location never drops below zero.
4. **Reservation ≤ availability:** you cannot reserve more than is available.
5. **Movement ledger:** every movement of goods leaves an immutable entry — current stock is a projection of movements.
6. **FEFO for expiring batches:** picking proposes batches with the nearest expiry date (First-Expired-First-Out).
7. **Quality hold:** a batch blocked by QC is not available for reservation or picking.

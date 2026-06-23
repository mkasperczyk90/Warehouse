# Application layer

Use cases (commands/queries) as vertical slices — one folder per slice (ADR-0007).

Slices are grouped by **aggregate** (the primary write root), one folder per group:

- `Deliveries/` — `InboundDelivery`: AnnounceDelivery, RegisterArrival, AssignDockSlot,
  StartReceiving, RecordReceiptLine, ConfirmReceipt, CancelDelivery, GetDelivery, ListDeliveries
- `Orders/` — `OutboundOrder`: CreateOutboundOrder, StartPicking, MarkPacked, ConfirmDispatch,
  CancelOrder, GetOrder, ListOrders
- `PickLists/` — `PickList`: ConfirmPick, ReportShortPick, GetPickList

The group is a navigation aid, not a boundary: it follows the aggregate (DDD), not the front end's
screens (see ADR-0007 — front and back slice on different axes). Group folders are named after the
domain area in plural (`Deliveries`, not `InboundDelivery`) so the namespace segment never collides
with the aggregate type of the same name.

Cross-cutting bits stay put: repository ports in `Abstractions/`, domain policies in
`Domain/Services`, event consumers in `Consumers/`. Depends on Domain only.

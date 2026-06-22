# Partners (MasterData)

`src/Services/MasterData/Modules/Warehouse.MasterData.Partners` — the Party/PartyRole
archetype: companies we do business with and the parts they play.

```mermaid
classDiagram
    class Party {
        <<AggregateRoot 🟩Party>>
        +PartyId Id
        +string Name
        +TaxId TaxId
        +ContactInfo Contact
        +IReadOnlyCollection~PartyRole~ Roles
        +Register(name, taxId, contact)$ Party
        +BecomeSupplier(code) SupplierRole
        +BecomeCustomer(code) CustomerRole
        +BecomeCarrier(code) CarrierRole
        +UpdateContact(contact)
    }
    class PartyRole {
        <<abstract Entity 🟥Role>>
        +PartyRoleId Id
        +string Code
    }
    class SupplierRole
    class CustomerRole {
        +IReadOnlyCollection~Address~ ShippingAddresses
        +AddShippingAddress(address)
    }
    class CarrierRole {
        +IReadOnlyCollection~ServiceLevel~ Services
        +AddService(level)
    }
    class ServiceLevel {
        <<enum>>
        Standard | Express | Refrigerated | HazardousGoods
    }
    class TaxId {
        <<ValueObject>>
        normalized, 8-15 alphanumeric
    }
    class ContactInfo {
        <<ValueObject>>
        +Email? +Phone?
        at least one channel required
    }
    Party "1" *-- "*" PartyRole
    PartyRole <|-- SupplierRole
    PartyRole <|-- CustomerRole
    PartyRole <|-- CarrierRole
    CarrierRole --> ServiceLevel
    Party *-- TaxId
    Party *-- ContactInfo
```

## Invariants

| Rule | Error code |
|---|---|
| A party never holds two roles of the same kind (it can be supplier **and** customer, never twice supplier) | `party_role_duplicate` |
| Contact requires at least e-mail or phone | `contact_info_required` |
| Tax id normalized to 8–15 alphanumeric chars | `tax_id_invalid` |

## How other contexts refer to parties

Logistics stores `PartyRoleRef(Guid)` — the **role** id, not the party id. An inbound delivery
points at a *supplier role*; if the same company also buys from us, its customer role is a
different ref. Role-specific data (shipping addresses, service levels) stays here.

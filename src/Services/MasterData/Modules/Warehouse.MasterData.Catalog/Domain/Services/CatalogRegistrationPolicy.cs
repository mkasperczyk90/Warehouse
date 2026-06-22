using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Catalog.Domain.Services;

/// <summary>
/// SKU and EAN uniqueness are <i>set-level</i> invariants: a single <see cref="ProductType"/>
/// cannot see its siblings, so the aggregate can't enforce them on its own. The application layer
/// probes the catalog (via <c>IProductTypeRepository</c>) and passes the answer in here — the same
/// pattern Inventory's <c>ReservationService</c> uses for available-to-promise. Keeping the rule
/// in a domain policy (instead of inline in a handler) means "what makes a registration legal"
/// stays in the domain and is unit-testable without a database.
/// </summary>
public static class CatalogRegistrationPolicy
{
    /// <summary>Guards the uniqueness rules before a product is defined. Throws on a clash.</summary>
    public static void EnsureCanRegister(Sku sku, Ean? ean, bool skuAlreadyExists, bool eanAlreadyExists)
    {
        ArgumentNullException.ThrowIfNull(sku);

        if (skuAlreadyExists)
        {
            throw new DomainException("product_sku_duplicate", $"A product with SKU {sku} is already registered.");
        }

        if (ean is not null && eanAlreadyExists)
        {
            throw new DomainException("product_ean_duplicate", $"A product with EAN {ean} is already registered.");
        }
    }
}

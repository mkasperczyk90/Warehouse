using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>
/// Delivery-specific packaging: for THIS inbound delivery, 1 <see cref="Unit"/> =
/// <see cref="FactorToBase"/> base units. The same SKU arrives palletized differently
/// depending on the supplier and pallet type (industrial vs euro), so "how many on a
/// pallet" is frequently a fact of the delivery, not encyclopedic master data. When a line
/// carries no pack, receiving falls back to the catalog's default conversion.
/// </summary>
public sealed record DeliveryPack
{
    private DeliveryPack(UnitOfMeasure unit, decimal factorToBase)
    {
        Unit = unit;
        FactorToBase = factorToBase;
    }

    public UnitOfMeasure Unit { get; }

    public decimal FactorToBase { get; }

    public static DeliveryPack Of(UnitOfMeasure unit, decimal factorToBase)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return factorToBase <= 0
            ? throw new DomainException(
                "delivery_pack_factor_invalid",
                $"Delivery pack factor for {unit} must be positive, got {factorToBase}.")
            : new DeliveryPack(unit, factorToBase);
    }

    /// <summary>Translates a quantity announced in <see cref="Unit"/> into the product's base unit.</summary>
    public Quantity ToBaseUnits(Quantity announced, UnitOfMeasure baseUnit)
    {
        ArgumentNullException.ThrowIfNull(announced);
        ArgumentNullException.ThrowIfNull(baseUnit);
        return announced.Unit != Unit
            ? throw new DomainException(
                "delivery_pack_unit_mismatch",
                $"Pack is defined for {Unit} but the announced quantity is in {announced.Unit}.")
            : Quantity.Of(announced.Amount * FactorToBase, baseUnit);
    }
}

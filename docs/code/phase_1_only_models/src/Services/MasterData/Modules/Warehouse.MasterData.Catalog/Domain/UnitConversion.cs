using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Domain;

/// <summary>
/// Per-product conversion factor: 1 <see cref="Unit"/> = <see cref="FactorToBase"/> base units
/// (e.g. for a given SKU: 1 plt = 48 pcs, 1 ctn = 12 pcs). Conversions are product master
/// data — never global arithmetic — which is why <c>Quantity</c> itself refuses to mix units.
/// </summary>
public sealed record UnitConversion
{
    private UnitConversion(UnitOfMeasure unit, decimal factorToBase)
    {
        Unit = unit;
        FactorToBase = factorToBase;
    }

    public UnitOfMeasure Unit { get; }

    public decimal FactorToBase { get; }

    public static UnitConversion Of(UnitOfMeasure unit, decimal factorToBase)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return factorToBase <= 0
            ? throw new DomainException(
                "conversion_factor_invalid",
                $"Conversion factor for {unit} must be positive, got {factorToBase}.")
            : new UnitConversion(unit, factorToBase);
    }
}

using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>
/// Quantity archetype (Arlow/Neustadt): the unit half of a Quantity. Closed set of units —
/// extend deliberately, conversions between units are a catalog concern (per-product factors).
/// </summary>
public sealed record UnitOfMeasure
{
    public static readonly UnitOfMeasure Piece = new("pcs", "Piece");
    public static readonly UnitOfMeasure Kilogram = new("kg", "Kilogram");
    public static readonly UnitOfMeasure Liter = new("l", "Liter");
    public static readonly UnitOfMeasure CubicMeter = new("m3", "Cubic meter");
    public static readonly UnitOfMeasure Pallet = new("plt", "Pallet");
    public static readonly UnitOfMeasure Carton = new("ctn", "Carton");

    private static readonly Dictionary<string, UnitOfMeasure> ByCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Piece.Code] = Piece,
            [Kilogram.Code] = Kilogram,
            [Liter.Code] = Liter,
            [CubicMeter.Code] = CubicMeter,
            [Pallet.Code] = Pallet,
            [Carton.Code] = Carton,
        };

    private UnitOfMeasure(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; }

    public string Name { get; }

    public static UnitOfMeasure FromCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return ByCode.TryGetValue(code, out var unit)
            ? unit
            : throw new DomainException("unit_unknown", $"Unknown unit of measure '{code}'.");
    }

    public override string ToString() => Code;
}

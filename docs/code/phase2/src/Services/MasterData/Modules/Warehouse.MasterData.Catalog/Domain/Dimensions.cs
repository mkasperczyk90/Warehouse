using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Domain;

/// <summary>Outer dimensions of a single sales unit, in centimeters.</summary>
public sealed record Dimensions
{
    private Dimensions(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }

    public decimal LengthCm { get; }

    public decimal WidthCm { get; }

    public decimal HeightCm { get; }

    public static Dimensions Of(decimal lengthCm, decimal widthCm, decimal heightCm) =>
        lengthCm <= 0 || widthCm <= 0 || heightCm <= 0
            ? throw new DomainException(
                "dimensions_invalid",
                $"All dimensions must be positive, got {lengthCm}×{widthCm}×{heightCm} cm.")
            : new Dimensions(lengthCm, widthCm, heightCm);

    public Volume UnitVolume() => Volume.FromCubicMeters(LengthCm * WidthCm * HeightCm / 1_000_000m);

    public override string ToString() => $"{LengthCm}×{WidthCm}×{HeightCm} cm";
}

using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Domain;

/// <summary>
/// Rule archetype: how a product must be stored. The Catalog owns the requirement;
/// the Inventory context holds its own replica and performs the compatibility check
/// against room environments.
/// </summary>
public sealed record StorageRequirement
{
    private StorageRequirement(TemperatureRange? temperature, bool requiresColdChain, bool isHazardous)
    {
        Temperature = temperature;
        RequiresColdChain = requiresColdChain;
        IsHazardous = isHazardous;
    }

    /// <summary>
    /// EF Core materialization constructor: the nested <see cref="Temperature"/> is an owned value
    /// object and cannot be bound to the parameterized constructor, so EF builds the instance via
    /// field access. Mirrors the private parameterless constructors the aggregates use.
    /// </summary>
    private StorageRequirement()
    {
    }

    /// <summary>Required storage temperature; null = no specific requirement (ambient).</summary>
    public TemperatureRange? Temperature { get; }

    public bool RequiresColdChain { get; }

    public bool IsHazardous { get; }

    public static StorageRequirement Ambient { get; } = new(null, requiresColdChain: false, isHazardous: false);

    public static StorageRequirement ColdChain(TemperatureRange temperature)
    {
        ArgumentNullException.ThrowIfNull(temperature);
        return new StorageRequirement(temperature, requiresColdChain: true, isHazardous: false);
    }

    public static StorageRequirement Hazardous(TemperatureRange? temperature = null) =>
        new(temperature, requiresColdChain: false, isHazardous: true);

    public static StorageRequirement Of(TemperatureRange? temperature, bool requiresColdChain, bool isHazardous) =>
        requiresColdChain && temperature is null
            ? throw new DomainException(
                "storage_requirement_invalid",
                "A cold-chain product must declare its required temperature range.")
            : new StorageRequirement(temperature, requiresColdChain, isHazardous);
}

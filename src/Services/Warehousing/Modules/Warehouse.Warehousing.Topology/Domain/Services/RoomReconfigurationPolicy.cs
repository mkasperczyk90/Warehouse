using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Topology.Domain.Services;

/// <summary>
/// The reciprocal of Inventory's <c>PutAwayPolicy</c>. Put-away asks "may this product go into
/// this room?"; this policy asks the other direction — "if we re-tune this room (or try to remove
/// it), what happens to the stock already inside?". Stock lives in the Inventory context, so the
/// occupants are passed in (as <see cref="StoredProduct"/>) rather than queried here. Changing a
/// room never moves goods automatically (UC-14); it reports the incompatibilities for a human to
/// resolve.
/// </summary>
public static class RoomReconfigurationPolicy
{
    /// <summary>
    /// Lists the SKUs that would become incompatible if the room switched to
    /// <paramref name="newEnvironment"/> — i.e. whose required temperature no longer covers the
    /// room's maintained temperature. An empty result means the change strands nothing.
    /// </summary>
    public static ReconfigurationImpact CheckEnvironmentChange(
        RoomEnvironment newEnvironment,
        IEnumerable<StoredProduct> storedProducts)
    {
        ArgumentNullException.ThrowIfNull(newEnvironment);
        ArgumentNullException.ThrowIfNull(storedProducts);

        var stranded = storedProducts
            .Where(p => p.RequiredTemperature is { } required &&
                        !required.Contains(newEnvironment.MaintainedTemperature))
            .Select(p => p.Sku)
            .ToList();

        return new ReconfigurationImpact(stranded);
    }

    /// <summary>
    /// Guards decommissioning: a room or location holding stock cannot be removed until it is
    /// emptied (UC-14). Whether it holds stock is an Inventory fact, supplied by the caller.
    /// </summary>
    public static void EnsureRemovable(string nodeCode, bool holdsStock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeCode);

        if (holdsStock)
        {
            throw new DomainException(
                "topology_node_not_empty",
                $"{nodeCode} still holds stock and cannot be removed; move the stock out first.");
        }
    }
}

/// <summary>A product currently stored in the room, as known by the Inventory context.</summary>
/// <param name="Sku">The stored product's SKU (as a plain string — contexts don't share the type).</param>
/// <param name="RequiredTemperature">Its required storage range, or <c>null</c> for ambient.</param>
public sealed record StoredProduct(string Sku, TemperatureRange? RequiredTemperature);

/// <summary>Outcome of a proposed room-environment change.</summary>
/// <param name="StrandedSkus">SKUs that would no longer be compatible with the new environment.</param>
public sealed record ReconfigurationImpact(IReadOnlyCollection<string> StrandedSkus)
{
    /// <summary>True when the change strands no existing stock.</summary>
    public bool IsClean => StrandedSkus.Count == 0;
}

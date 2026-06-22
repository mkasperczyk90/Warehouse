using Warehouse.SharedKernel.Domain;

namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>Place archetype (🟩): a ramp where trucks are loaded/unloaded.</summary>
public sealed class Dock : Entity<DockCode>
{
    internal Dock(DockCode code, DockDirection direction) : base(code)
    {
        Direction = direction;
    }

    private Dock()
    {
    }

    public DockCode Code => Id;

    public DockDirection Direction { get; private set; }
}

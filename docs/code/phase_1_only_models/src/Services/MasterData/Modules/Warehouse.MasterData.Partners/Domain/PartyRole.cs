using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Partners.Domain;

/// <summary>
/// Role archetype (🟥): a part a party plays in our processes. One company can hold
/// several roles at once (supplier and customer), but never two roles of the same kind.
/// </summary>
public abstract class PartyRole : Entity<PartyRoleId>
{
    protected PartyRole(PartyRoleId id, string code) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim().ToUpperInvariant();
    }

    protected PartyRole()
    {
    }

    /// <summary>Short business code used on documents (e.g. SUP-001).</summary>
    public string Code { get; protected set; } = null!;
}

/// <summary>The party ships goods to us (inbound side).</summary>
public sealed class SupplierRole : PartyRole
{
    public SupplierRole(PartyRoleId id, string code) : base(id, code)
    {
    }

    private SupplierRole()
    {
    }
}

/// <summary>The party orders goods from us (outbound side).</summary>
public sealed class CustomerRole : PartyRole
{
    private readonly List<Address> _shippingAddresses = [];

    public CustomerRole(PartyRoleId id, string code) : base(id, code)
    {
    }

    private CustomerRole()
    {
    }

    public IReadOnlyCollection<Address> ShippingAddresses => _shippingAddresses.AsReadOnly();

    public void AddShippingAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!_shippingAddresses.Contains(address))
        {
            _shippingAddresses.Add(address);
        }
    }
}

/// <summary>The party transports goods on our behalf.</summary>
public sealed class CarrierRole : PartyRole
{
    private readonly List<ServiceLevel> _services = [];

    public CarrierRole(PartyRoleId id, string code) : base(id, code)
    {
    }

    private CarrierRole()
    {
    }

    public IReadOnlyCollection<ServiceLevel> Services => _services.AsReadOnly();

    public void AddService(ServiceLevel service)
    {
        if (!_services.Contains(service))
        {
            _services.Add(service);
        }
    }
}

public enum ServiceLevel
{
    Standard = 0,
    Express = 1,
    Refrigerated = 2,
    HazardousGoods = 3,
}

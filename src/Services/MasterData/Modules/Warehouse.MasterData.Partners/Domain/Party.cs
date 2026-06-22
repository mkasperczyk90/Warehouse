using Warehouse.MasterData.Partners.Domain.Events;
using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Partners.Domain;

/// <summary>
/// Party archetype (🟩): a company we do business with. Roles model what the party
/// does for us; identity and legal data live here, role-specific data on the role.
/// </summary>
public sealed class Party : AggregateRoot<PartyId>
{
    private readonly List<PartyRole> _roles = [];

    private Party(PartyId id, string name, TaxId taxId, ContactInfo contact) : base(id)
    {
        Name = name;
        TaxId = taxId;
        Contact = contact;
    }

    private Party()
    {
    }

    public string Name { get; private set; } = null!;

    public TaxId TaxId { get; private set; } = null!;

    public ContactInfo Contact { get; private set; } = null!;

    public IReadOnlyCollection<PartyRole> Roles => _roles.AsReadOnly();

    public static Party Register(string name, TaxId taxId, ContactInfo contact)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(taxId);
        ArgumentNullException.ThrowIfNull(contact);
        var party = new Party(PartyId.New(), name.Trim(), taxId, contact);
        party.Raise(new PartyRegistered(party.Id, party.Name, DateTimeOffset.UtcNow));
        return party;
    }

    public SupplierRole BecomeSupplier(string code) => AddRole(new SupplierRole(PartyRoleId.New(), code));

    public CustomerRole BecomeCustomer(string code) => AddRole(new CustomerRole(PartyRoleId.New(), code));

    public CarrierRole BecomeCarrier(string code) => AddRole(new CarrierRole(PartyRoleId.New(), code));

    public void UpdateContact(ContactInfo contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        Contact = contact;
    }

    private TRole AddRole<TRole>(TRole role)
        where TRole : PartyRole
    {
        if (_roles.Any(r => r.GetType() == role.GetType()))
        {
            throw new DomainException(
                "party_role_duplicate",
                $"Party '{Name}' already has the {role.GetType().Name} role.");
        }

        _roles.Add(role);
        return role;
    }
}

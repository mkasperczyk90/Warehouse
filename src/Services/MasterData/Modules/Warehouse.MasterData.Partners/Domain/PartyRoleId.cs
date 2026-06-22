namespace Warehouse.MasterData.Partners.Domain;

public readonly record struct PartyRoleId(Guid Value)
{
    public static PartyRoleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

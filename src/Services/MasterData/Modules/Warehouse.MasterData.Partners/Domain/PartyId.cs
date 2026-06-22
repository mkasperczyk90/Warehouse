namespace Warehouse.MasterData.Partners.Domain;

public readonly record struct PartyId(Guid Value)
{
    public static PartyId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

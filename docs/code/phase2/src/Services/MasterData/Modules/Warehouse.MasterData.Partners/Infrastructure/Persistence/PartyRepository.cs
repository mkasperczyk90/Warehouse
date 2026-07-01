using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Partners.Application.Abstractions;
using Warehouse.MasterData.Partners.Domain;

namespace Warehouse.MasterData.Partners.Infrastructure.Persistence;

/// <summary>EF Core implementation of the party persistence port. Roles (and their owned
/// collections) are loaded with the aggregate.</summary>
internal sealed class PartyRepository(PartnersDbContext context) : IPartyRepository
{
    public Task<Party?> GetByIdAsync(PartyId id, CancellationToken cancellationToken = default) =>
        context.Parties.Include(p => p.Roles).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Party aggregate) => context.Parties.Add(aggregate);

    public void Update(Party aggregate) => context.Parties.Update(aggregate);

    public Task<bool> ExistsByTaxIdAsync(TaxId taxId, CancellationToken cancellationToken = default) =>
        context.Parties.AnyAsync(p => p.TaxId == taxId, cancellationToken);
}

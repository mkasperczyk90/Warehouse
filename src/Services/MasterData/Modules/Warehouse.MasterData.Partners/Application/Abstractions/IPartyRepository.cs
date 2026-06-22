using Warehouse.MasterData.Partners.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Partners.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Party"/> aggregate (with its roles).</summary>
public interface IPartyRepository : IRepository<Party, PartyId>
{
    /// <summary>Uniqueness probe for <c>PartyRegistrationPolicy</c>: is this tax id already registered?</summary>
    Task<bool> ExistsByTaxIdAsync(TaxId taxId, CancellationToken cancellationToken = default);
}

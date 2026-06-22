using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Partners.Domain.Services;

/// <summary>
/// One legal entity = one <see cref="Party"/>: the tax id must be unique across all parties.
/// That's a set-level rule a single aggregate can't see, so the application layer probes
/// (via <c>IPartyRepository</c>) and passes the result in — mirroring the catalog's
/// <c>CatalogRegistrationPolicy</c>. Roles (supplier/customer/carrier) are added to the party
/// afterwards; this policy only guards the identity-level uniqueness.
/// </summary>
public static class PartyRegistrationPolicy
{
    /// <summary>Guards tax-id uniqueness before a party is registered. Throws on a clash.</summary>
    public static void EnsureCanRegister(TaxId taxId, bool taxIdAlreadyExists)
    {
        ArgumentNullException.ThrowIfNull(taxId);

        if (taxIdAlreadyExists)
        {
            throw new DomainException("party_tax_id_duplicate", $"A party with tax id {taxId} is already registered.");
        }
    }
}

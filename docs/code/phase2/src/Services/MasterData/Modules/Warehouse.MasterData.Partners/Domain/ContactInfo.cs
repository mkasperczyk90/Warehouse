using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Partners.Domain;

/// <summary>Contact details of a party; at least one channel is required.</summary>
public sealed record ContactInfo
{
    private ContactInfo(string? email, string? phone)
    {
        Email = email;
        Phone = phone;
    }

    public string? Email { get; }

    public string? Phone { get; }

    public static ContactInfo Of(string? email, string? phone)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (normalizedEmail is null && normalizedPhone is null)
        {
            throw new DomainException("contact_info_required", "A party needs at least an e-mail or a phone number.");
        }

        if (normalizedEmail is not null &&
            (normalizedEmail.Count(c => c == '@') != 1 || normalizedEmail.StartsWith('@') || normalizedEmail.EndsWith('@')))
        {
            throw new DomainException("contact_email_invalid", $"'{email}' is not a valid e-mail address.");
        }

        return new ContactInfo(normalizedEmail, normalizedPhone);
    }
}

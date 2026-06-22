using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.ValueObjects;

/// <summary>
/// Money archetype: amount + ISO 4217 currency. Used for stock valuation on adjustments
/// and stocktake reconciliation. Cross-currency arithmetic is forbidden by design.
/// </summary>
public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>ISO 4217 code, e.g. "PLN", "EUR".</summary>
    public string Currency { get; }

    public static Money Of(decimal amount, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var normalized = currency.Trim().ToUpperInvariant();
        return normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper)
            ? throw new DomainException("currency_invalid", $"'{currency}' is not a valid ISO 4217 currency code.")
            : new Money(amount, normalized);
    }

    public static Money Zero(string currency) => Of(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public static Money operator +(Money left, Money right) => Add(left, right);

    public static Money operator -(Money left, Money right) => Subtract(left, right);

    public override string ToString() => $"{Amount:0.00} {Currency}";

    private static Money Add(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Add(right);
    }

    private static Money Subtract(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Subtract(right);
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Currency != other.Currency)
        {
            throw new DomainException(
                "currency_mismatch",
                $"Cannot combine money in different currencies: {Currency} and {other.Currency}.");
        }
    }
}

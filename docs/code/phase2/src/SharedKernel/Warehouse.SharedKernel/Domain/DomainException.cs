namespace Warehouse.SharedKernel.Domain;

/// <summary>
/// A business rule violation. Carries a stable <see cref="ErrorCode"/> so APIs can map it
/// to ProblemDetails and clients/translations don't depend on the message text.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string errorCode, string message) : base(message) =>
        ErrorCode = errorCode;

    public DomainException()
    {
    }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public string ErrorCode { get; } = "domain_rule_violated";
}

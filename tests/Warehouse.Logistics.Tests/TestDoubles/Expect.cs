using Warehouse.SharedKernel.Domain;
using Xunit;

namespace Warehouse.Logistics.Tests.TestDoubles;

/// <summary>Asserts a <see cref="DomainException"/> with a specific stable error code — the codes are
/// part of the domain contract (they drive API ProblemDetails and translations), so the tests pin them.</summary>
internal static class Expect
{
    public static DomainException DomainError(string code, Action act)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Equal(code, ex.ErrorCode);
        return ex;
    }

    public static async Task<DomainException> DomainErrorAsync(string code, Func<Task> act)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(act);
        Assert.Equal(code, ex.ErrorCode);
        return ex;
    }
}

using Warehouse.SharedKernel.Domain;
using Xunit;

namespace Warehouse.SharedKernel.Tests;

/// <summary>Asserts a <see cref="DomainException"/> with a specific stable error code.</summary>
internal static class Expect
{
    public static void DomainError(string code, Action act)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Equal(code, ex.ErrorCode);
    }
}

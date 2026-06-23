using NetArchTest.Rules;
using Warehouse.Contracts.Logistics;
using Warehouse.SharedKernel.Domain;
using Xunit;

namespace Warehouse.ArchitectureTests;

/// <summary>
/// Guards the two foundation packages every service builds on. Their csproj comments promise purity
/// ("no EF, no messaging, no ASP.NET", "primitives only: no dependencies"); these tests enforce it.
/// </summary>
public sealed class FoundationRulesTests
{
    private static readonly string[] ServiceNamespaces =
        ["Warehouse.Logistics", "Warehouse.Warehousing", "Warehouse.MasterData"];

    private static readonly string[] InfrastructureConcerns =
        ["Microsoft.EntityFrameworkCore", "Wolverine", "Microsoft.AspNetCore"];

    [Fact]
    public void SharedKernel_stays_free_of_infrastructure_and_service_code()
    {
        var result = Types.InAssembly(typeof(DomainException).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([.. InfrastructureConcerns, "Warehouse.Contracts", .. ServiceNamespaces])
            .GetResult();

        ArchTest.Assert(result);
    }

    [Fact]
    public void Contracts_are_primitives_only_independent_of_kernel_services_and_infrastructure()
    {
        var result = Types.InAssembly(typeof(OutboundOrderPlacedV1).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([.. InfrastructureConcerns, "Warehouse.SharedKernel", .. ServiceNamespaces])
            .GetResult();

        ArchTest.Assert(result);
    }
}

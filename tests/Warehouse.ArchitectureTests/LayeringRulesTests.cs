using NetArchTest.Rules;
using Xunit;

namespace Warehouse.ArchitectureTests;

/// <summary>
/// Clean Architecture boundaries inside each bounded-context module: the Domain is the innermost ring,
/// so it must not reach out to Application, Infrastructure, persistence (EF Core), messaging (Wolverine)
/// or the cross-service integration Contracts. One marker type pins each module's assembly + namespace.
/// </summary>
public sealed class LayeringRulesTests
{
    [Theory]
    [InlineData(typeof(Warehouse.Logistics.Core.Domain.OutboundOrder))]
    [InlineData(typeof(Warehouse.Warehousing.Inventory.Domain.StockItem))]
    [InlineData(typeof(Warehouse.Warehousing.Topology.Domain.Dock))]
    [InlineData(typeof(Warehouse.MasterData.Catalog.Domain.Ean))]
    [InlineData(typeof(Warehouse.MasterData.Partners.Domain.Party))]
    public void Domain_does_not_depend_on_outer_rings(Type domainMarker)
    {
        var domainNamespace = domainMarker.Namespace!;            // e.g. Warehouse.Logistics.Core.Domain
        var module = domainNamespace[..^".Domain".Length];        // e.g. Warehouse.Logistics.Core

        var result = Types.InAssembly(domainMarker.Assembly)
            .That()
            .ResideInNamespaceStartingWith(domainNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(
                $"{module}.Application",
                $"{module}.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Wolverine",
                "Warehouse.Contracts")
            .GetResult();

        ArchTest.Assert(result);
    }
}

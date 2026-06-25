using Warehouse.Warehousing.Topology.Application.Warehouses.EstablishWarehouse;
using Xunit;

namespace Warehouse.Warehousing.Tests.Topology;

public sealed class EstablishWarehouseHandlerTests
{
    [Fact]
    public async Task Establish_persists_the_warehouse_and_returns_its_normalized_code()
    {
        var warehouses = new FakeWarehouseRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new EstablishWarehouseHandler(warehouses, unitOfWork);

        var code = await handler.HandleAsync(Build.EstablishCommand("waw1", "Warsaw DC"));

        Assert.Equal("WAW1", code);
        var saved = Assert.Single(warehouses.Saved);
        Assert.Equal("WAW1", saved.Code.Value);
        Assert.Equal("Warsaw DC", saved.Name);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Establish_rejects_a_duplicate_code()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new EstablishWarehouseHandler(warehouses, new FakeUnitOfWork());

        await Expect.DomainErrorAsync(
            "warehouse_code_duplicate", () => handler.HandleAsync(Build.EstablishCommand("WAW1")));
    }
}

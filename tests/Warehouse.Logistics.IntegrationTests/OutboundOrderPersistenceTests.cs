using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Application.Orders.GetOrder;
using Warehouse.Logistics.Core.Application.Orders.ListOrders;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.Application;
using Xunit;

namespace Warehouse.Logistics.IntegrationTests;

public sealed class OutboundOrderPersistenceTests(LogisticsDatabaseFixture fixture)
    : LogisticsIntegrationTest(fixture)
{
    private async Task<OrderId> PersistAsync(OutboundOrder order)
    {
        await using var scope = Fixture.NewScope();
        scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>().Add(order);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return order.Id;
    }

    [Fact]
    public async Task OutboundOrder_round_trips_through_postgres_with_its_owned_value_objects()
    {
        var id = await PersistAsync(Sample.Order(quantity: 10, sku: "SKU-1"));

        await using var scope = Fixture.NewScope();
        var reloaded = await scope.ServiceProvider.GetRequiredService<IOutboundOrderRepository>().GetByIdAsync(id);

        Assert.NotNull(reloaded);
        Assert.Equal(OrderStatus.Created, reloaded!.Status);
        Assert.Equal("WH01", reloaded.Warehouse.Code);
        Assert.Equal("Wrocław", reloaded.ShipTo.City);
        var line = Assert.Single(reloaded.Lines);
        Assert.Equal("SKU-1", line.Product.Value);
        Assert.Equal(10, line.Ordered.Amount);
        Assert.Equal("pcs", line.Ordered.Unit.Code);
    }

    [Fact]
    public async Task GetOrderHandler_reads_the_order_back_as_a_dto()
    {
        var id = await PersistAsync(Sample.Order(quantity: 24, sku: "SKU-9"));

        await using var scope = Fixture.NewScope();
        var handler = new GetOrderHandler(scope.ServiceProvider.GetRequiredService<LogisticsDbContext>());

        var dto = await handler.HandleAsync(new GetOrderQuery(id.Value));

        Assert.NotNull(dto);
        Assert.Equal(id.Value, dto!.Id);
        Assert.Equal("WH01", dto.WarehouseCode);
        Assert.Equal("Created", dto.Status);
        var line = Assert.Single(dto.Lines);
        Assert.Equal("SKU-9", line.ProductCode);
        Assert.Equal(24, line.Quantity);
    }

    [Fact]
    public async Task ListOrdersHandler_filters_by_lifecycle_status()
    {
        await PersistAsync(Sample.Order(sku: "AAA"));            // stays Created

        var reserved = Sample.Order(sku: "BBB");
        reserved.MarkReserved(fully: true);
        await PersistAsync(reserved);                            // Reserved

        await using var scope = Fixture.NewScope();
        var handler = new ListOrdersHandler(scope.ServiceProvider.GetRequiredService<LogisticsDbContext>());

        var all = await handler.HandleAsync(new ListOrdersQuery(null));
        var onlyReserved = await handler.HandleAsync(new ListOrdersQuery(OrderStatus.Reserved));

        Assert.Equal(2, all.Count);
        var single = Assert.Single(onlyReserved);
        Assert.Equal("Reserved", single.Status);
        Assert.Equal(1, single.LineCount);
    }

    [Fact]
    public async Task Concurrent_updates_are_rejected_by_the_xmin_concurrency_token()
    {
        var id = await PersistAsync(Sample.Order());

        await using var scopeA = Fixture.NewScope();
        await using var scopeB = Fixture.NewScope();
        var ordersA = scopeA.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();
        var ordersB = scopeB.ServiceProvider.GetRequiredService<IOutboundOrderRepository>();

        // Both load the same row (same xmin), then both try to advance it.
        var orderA = await ordersA.GetByIdAsync(id);
        var orderB = await ordersB.GetByIdAsync(id);

        orderA!.MarkReserved(fully: true);
        await scopeA.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        orderB!.MarkReserved(fully: true);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => scopeB.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
    }
}

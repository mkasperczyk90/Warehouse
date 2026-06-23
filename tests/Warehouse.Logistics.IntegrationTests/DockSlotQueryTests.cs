using Microsoft.Extensions.DependencyInjection;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.SharedKernel.Application;
using Xunit;

namespace Warehouse.Logistics.IntegrationTests;

public sealed class DockSlotQueryTests(LogisticsDatabaseFixture fixture)
    : LogisticsIntegrationTest(fixture)
{
    private static readonly DateTimeOffset Day = new(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);

    private async Task SeedSlotAsync(string dock, int fromHour, int toHour)
    {
        await using var scope = Fixture.NewScope();
        scope.ServiceProvider.GetRequiredService<IInboundDeliveryRepository>()
            .Add(Sample.DeliveryWithSlot(dock, Day.AddHours(fromHour), Day.AddHours(toHour)));
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    [Fact]
    public async Task ListBookedSlots_returns_a_slot_overlapping_the_window_at_the_same_dock()
    {
        await SeedSlotAsync("D-1", 9, 11);

        await using var scope = Fixture.NewScope();
        var deliveries = scope.ServiceProvider.GetRequiredService<IInboundDeliveryRepository>();

        var clashing = await deliveries.ListBookedSlotsAsync("D-1", Day.AddHours(10), Day.AddHours(12));

        var slot = Assert.Single(clashing);
        Assert.Equal("D-1", slot.DockCode);
    }

    [Fact]
    public async Task ListBookedSlots_ignores_other_docks_and_non_overlapping_windows()
    {
        await SeedSlotAsync("D-1", 9, 11);

        await using var scope = Fixture.NewScope();
        var deliveries = scope.ServiceProvider.GetRequiredService<IInboundDeliveryRepository>();

        Assert.Empty(await deliveries.ListBookedSlotsAsync("D-2", Day.AddHours(10), Day.AddHours(12))); // other dock
        Assert.Empty(await deliveries.ListBookedSlotsAsync("D-1", Day.AddHours(11), Day.AddHours(12))); // after the slot
    }
}

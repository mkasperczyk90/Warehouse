using Microsoft.Extensions.DependencyInjection;
using Warehouse.Logistics.Core.Application.AnnounceDelivery;
using Warehouse.Logistics.Core.Application.AssignDockSlot;
using Warehouse.Logistics.Core.Application.CancelDelivery;
using Warehouse.Logistics.Core.Application.ConfirmReceipt;
using Warehouse.Logistics.Core.Application.GetDelivery;
using Warehouse.Logistics.Core.Application.ListDeliveries;
using Warehouse.Logistics.Core.Application.RecordReceiptLine;
using Warehouse.Logistics.Core.Application.RegisterArrival;
using Warehouse.Logistics.Core.Application.StartReceiving;

namespace Warehouse.Logistics.Core.Application;

/// <summary>
/// Registers the inbound use-case handlers (one per vertical slice, ADR-0007). Endpoints resolve
/// these directly; the RabbitMQ consumers are discovered by Wolverine, not registered here.
/// </summary>
public static class LogisticsApplication
{
    public static IServiceCollection AddLogisticsApplication(this IServiceCollection services)
    {
        services.AddScoped<AnnounceDeliveryHandler>();
        services.AddScoped<AssignDockSlotHandler>();
        services.AddScoped<RegisterArrivalHandler>();
        services.AddScoped<StartReceivingHandler>();
        services.AddScoped<RecordReceiptLineHandler>();
        services.AddScoped<ConfirmReceiptHandler>();
        services.AddScoped<CancelDeliveryHandler>();
        services.AddScoped<GetDeliveryHandler>();
        services.AddScoped<ListDeliveriesHandler>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Warehouse.Logistics.Core.Application.Deliveries.AnnounceDelivery;
using Warehouse.Logistics.Core.Application.Deliveries.AssignDockSlot;
using Warehouse.Logistics.Core.Application.Deliveries.CancelDelivery;
using Warehouse.Logistics.Core.Application.Orders.CancelOrder;
using Warehouse.Logistics.Core.Application.Orders.ConfirmDispatch;
using Warehouse.Logistics.Core.Application.PickLists.ConfirmPick;
using Warehouse.Logistics.Core.Application.Deliveries.ConfirmReceipt;
using Warehouse.Logistics.Core.Application.Orders.CreateOutboundOrder;
using Warehouse.Logistics.Core.Application.Deliveries.GetDelivery;
using Warehouse.Logistics.Core.Application.Orders.GetOrder;
using Warehouse.Logistics.Core.Application.PickLists.GetPickList;
using Warehouse.Logistics.Core.Application.Deliveries.ListDeliveries;
using Warehouse.Logistics.Core.Application.Orders.ListOrders;
using Warehouse.Logistics.Core.Application.Orders.MarkPacked;
using Warehouse.Logistics.Core.Application.PickLists.ReportShortPick;
using Warehouse.Logistics.Core.Application.Deliveries.RecordReceiptLine;
using Warehouse.Logistics.Core.Application.Deliveries.RegisterArrival;
using Warehouse.Logistics.Core.Application.Orders.StartPicking;
using Warehouse.Logistics.Core.Application.Deliveries.StartReceiving;

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

        // Outbound (UC-09…UC-12)
        services.AddScoped<CreateOutboundOrderHandler>();
        services.AddScoped<StartPickingHandler>();
        services.AddScoped<MarkPackedHandler>();
        services.AddScoped<ConfirmDispatchHandler>();
        services.AddScoped<CancelOrderHandler>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<ListOrdersHandler>();

        // Picking (UC-10)
        services.AddScoped<ConfirmPickHandler>();
        services.AddScoped<ReportShortPickHandler>();
        services.AddScoped<GetPickListHandler>();
        return services;
    }
}

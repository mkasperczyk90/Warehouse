using Microsoft.EntityFrameworkCore;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;
using Warehouse.Warehousing.Inventory.Domain.Services;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Dev-only seed for the Inventory context: the local Product/Location replicas plus a spread of stock
/// items across both warehouses, shaped so the admin Stock view shows every status variant (available,
/// reserved, expiring, blocked). Idempotent — it no-ops once any stock exists. Stock is seeded through
/// the domain (Receive → Allocate / MarkBlocked) so the ledger and on-hand stay consistent, exactly as
/// they would in production. Mirrors the warehouse codes/locations from <see cref="TopologySeeder"/>.
/// </summary>
internal static class InventorySeeder
{
    private static readonly UnitOfMeasure Pcs = UnitOfMeasure.Piece;
    private static readonly UnitOfMeasure Kg = UnitOfMeasure.Kilogram;

    public static async Task SeedAsync(InventoryDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.StockItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        SeedProducts(db, now);
        SeedLocations(db, now);
        SeedStock(db);
        SeedStocktakes(db);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void SeedStocktakes(InventoryDbContext db)
    {
        var pcs = Pcs;
        Quantity Q(decimal n) => Quantity.Of(n, pcs);

        // A counted stocktake awaiting review — cold-room aisle A: one match, one shortage. Opening it in
        // the admin shows the difference; approving it posts the −12 as a ledger adjustment.
        var review = Stocktake.Order(
            [LocationCode.Of("WH01-CR1-A03-R2-S1"), LocationCode.Of("WH01-CR1-A01-R1-S4")],
            "operator-12", "Cold room 1, aisle A");
        review.RecordCount(LocationCode.Of("WH01-CR1-A03-R2-S1"), Sku.Of("4006381333931"), BatchNumber.Of("LOT-0425-A"), Q(240), Q(240), "operator-12");
        review.RecordCount(LocationCode.Of("WH01-CR1-A01-R1-S4"), Sku.Of("5900512331027"), BatchNumber.Of("LOT-0331"), Q(588), Q(600), "operator-12");
        db.Stocktakes.Add(review);

        // A historical, already-approved stocktake — freezer (one prior shortage, posted earlier).
        var done = Stocktake.Order([LocationCode.Of("WH01-FZ1-B02-R4-S1")], "operator-07", "Freezer 1");
        done.RecordCount(LocationCode.Of("WH01-FZ1-B02-R4-S1"), Sku.Of("5601012009873"), BatchNumber.Of("LOT-0288"), Q(318), Q(320), "operator-07");
        done.Approve();
        db.Stocktakes.Add(done);
    }

    private static void SeedProducts(InventoryDbContext db, DateTimeOffset now)
    {
        Product("4006381333931", "Whole milk 3.2% 1 L", Pcs, 1.03m, 0.0010m, TemperatureRange.Of(2, 6), coldChain: true);
        Product("5901234123457", "Greek yoghurt 400 g", Pcs, 0.41m, 0.0006m, TemperatureRange.Of(2, 6), coldChain: true);
        Product("5900512331027", "Butter block 250 g", Pcs, 0.25m, 0.0003m, TemperatureRange.Of(2, 6), coldChain: true);
        Product("5601012009873", "Frozen berries 1 kg", Pcs, 1.00m, 0.0017m, TemperatureRange.Of(-25, -18), coldChain: true);
        Product("5601012009880", "Frozen peas 1 kg", Pcs, 1.00m, 0.0017m, TemperatureRange.Of(-25, -18), coldChain: true);
        Product("5902860004417", "Cheese wheel 5 kg", Kg, 5.00m, 0.0090m, TemperatureRange.Of(2, 8), coldChain: true);

        void Product(
            string sku, string name, UnitOfMeasure unit, decimal weightKg, decimal volumeM3,
            TemperatureRange temperature, bool coldChain) =>
            db.ProductSnapshots.Add(new ProductSnapshot(
                Sku.Of(sku), name, unit, Weight.FromKilograms(weightKg), Volume.FromCubicMeters(volumeM3),
                temperature, requiresColdChain: coldChain, isHazardous: false, isBatchTracked: true,
                hasExpiryDate: true, updatedAt: now));
    }

    private static void SeedLocations(InventoryDbContext db, DateTimeOffset now)
    {
        Location("WH01-CR1-A03-R2-S1", "WH01", "CR1", TemperatureRange.Of(2, 6));
        Location("WH01-CR1-A01-R1-S4", "WH01", "CR1", TemperatureRange.Of(2, 6));
        Location("WH01-CR1-PICKFACE-12", "WH01", "CR1", TemperatureRange.Of(2, 6)); // cold pick face (UC-06 move target)
        Location("WH01-STD-A07-R3-S2", "WH01", "STD", TemperatureRange.Ambient);
        Location("WH01-FZ1-B02-R4-S1", "WH01", "FZ1", TemperatureRange.Of(-25, -18));
        Location("WH01-QC-HOLD-02", "WH01", "QC", TemperatureRange.Ambient);
        Location("WH02-CR1-A01-R1-S1", "WH02", "CR1", TemperatureRange.Of(2, 6));
        Location("WH02-FZ1-B01-R2-S3", "WH02", "FZ1", TemperatureRange.Of(-25, -18));

        void Location(string code, string warehouse, string room, TemperatureRange temperature) =>
            db.LocationSnapshots.Add(new LocationSnapshot(
                LocationCode.Of(code), WarehouseCode.Of(warehouse), room, temperature, isHazmatZone: false,
                Volume.FromCubicMeters(1.5m), Weight.FromKilograms(600m), updatedAt: now));
    }

    private static void SeedStock(InventoryDbContext db)
    {
        // WH01
        Reserved("4006381333931", "LOT-0425-A", new DateOnly(2026, 7, 20), "WH01-CR1-A03-R2-S1", Pcs, 240, 24);
        Reserved("5901234123457", "LOT-0419", new DateOnly(2026, 7, 19), "WH01-STD-A07-R3-S2", Pcs, 1_440, 480);
        Available("5900512331027", "LOT-0331", new DateOnly(2026, 6, 15), "WH01-CR1-A01-R1-S4", Pcs, 600); // expired
        Available("5601012009873", "LOT-0288", new DateOnly(2027, 2, 10), "WH01-FZ1-B02-R4-S1", Pcs, 320);
        // On QC hold — the inspector's worklist (UC-03): a held batch + its quarantined stock.
        Quarantined("5902860004417", "LOT-0402", new DateOnly(2026, 9, 1), "WH01-QC-HOLD-02", Kg, 48, "GR-2206 · Dairy Farms");
        Quarantined("4006381333931", "LOT-0307-QC", new DateOnly(2026, 7, 5), "WH01-QC-HOLD-02", Pcs, 96, "GR-2207 · Dairy Farms");
        // WH02
        Available("4006381333931", "LOT-0512-PZ", new DateOnly(2026, 7, 20), "WH02-CR1-A01-R1-S1", Pcs, 480);
        Reserved("5601012009880", "LOT-0490-PZ", new DateOnly(2027, 3, 1), "WH02-FZ1-B01-R2-S3", Pcs, 150, 60);

        // Stock enters the way it does in production (UC-02 → UC-04): a goods receipt lands in the dock
        // buffer, then put-away moves it into storage. That writes two honest ledger entries per item
        // (receipt @ buffer, put-away @ storage); the empty buffer line is transient and not persisted.
        (StockItem Item, Batch Batch) Receive(
            string sku, string batchNo, DateOnly expiry, string location, UnitOfMeasure unit, decimal onHand,
            string supplierRef = "Seed")
        {
            var s = Sku.Of(sku);
            var batchNumber = BatchNumber.Of(batchNo);
            var batch = Batch.Register(s, batchNumber, expiry, supplierRef);
            db.Batches.Add(batch);

            var storageLocation = LocationCode.Of(location);
            var warehouse = WarehouseCode.Of(location[..location.IndexOf('-', StringComparison.Ordinal)]);
            var buffer = StockItem.CreateAt(DockBuffer.For(warehouse), s, batchNumber, unit);
            // Each movement gets its own Quantity instance: the receipt and the put-away below are both
            // persisted (owned qty_amount), and EF Core cannot back two owned dependents with one shared
            // CLR instance — the second would persist as null. Same value, distinct instances.
            var receipt = buffer.Receive(Quantity.Of(onHand, unit), MovementType.GoodsReceipt, "seed", reason: $"GR-{batchNo}");

            var storage = StockItem.CreateAt(storageLocation, s, batchNumber, unit);
            var putAway = StockTransferService.Transfer(
                buffer, storage, Quantity.Of(onHand, unit), MovementType.PutAway, "seed", reason: $"PA-{batchNo}");

            db.StockItems.Add(storage);
            db.StockMovements.Add(receipt);
            db.StockMovements.Add(putAway);
            return (storage, batch);
        }

        void Available(string sku, string batchNo, DateOnly expiry, string location, UnitOfMeasure unit, decimal onHand) =>
            Receive(sku, batchNo, expiry, location, unit, onHand);

        void Reserved(
            string sku, string batchNo, DateOnly expiry, string location, UnitOfMeasure unit,
            decimal onHand, decimal allocated)
        {
            var (item, _) = Receive(sku, batchNo, expiry, location, unit, onHand);
            item.Allocate(Quantity.Of(allocated, unit), OrderRef.Of($"SO-SEED-{batchNo}"), StockReservationId.New());
        }

        void Quarantined(
            string sku, string batchNo, DateOnly expiry, string location, UnitOfMeasure unit,
            decimal onHand, string supplierRef)
        {
            var (item, batch) = Receive(sku, batchNo, expiry, location, unit, onHand, supplierRef);
            item.MarkQuarantine();
            batch.Quarantine();
        }
    }
}

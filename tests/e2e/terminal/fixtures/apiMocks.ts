import type { Page, Route } from '@playwright/test';

/**
 * Network stubs for the terminal e2e suite.
 *
 * The app no longer ships MSW — it always calls the real Gateway. For e2e we intercept `**​/api/**` at the
 * Playwright layer and answer with deterministic JSON shaped like the real Gateway DTOs (the same shapes
 * the feature models consume). Writes are stateful within a test: a confirm/receipt mutates the in-memory
 * counts so the next `GET terminal/tasks` reflects it (the hub pile drops), and a short pick advances the
 * pick list — exactly the behaviour the scenarios assert.
 *
 * Install per page (see fixtures/index.ts). State is per-install, so it resets between tests.
 */
export async function installApiMocks(page: Page): Promise<void> {
  // --- Mutable state (one delivery / order drives the demo flow) -------------------------------------
  const taskCounts = { receive: 2, putaway: 14, pick: 31, move: 5 };

  type PickTask = {
    sequence: number;
    location: string;
    productCode: string;
    batchNumber: string | null;
    quantity: number;
    unit: string;
    status: string;
  };
  const pickTasks: PickTask[] = [
    { sequence: 1, location: 'WH01-A2-A09-R1-S2', productCode: '4006381333931', batchNumber: 'LOT-0425-A', quantity: 12, unit: 'ea', status: 'Pending' },
    { sequence: 2, location: 'WH01-CR1-A01-R1-S4', productCode: '4006381333931', batchNumber: 'LOT-0331', quantity: 6, unit: 'ea', status: 'Pending' },
  ];

  // --- Static fixtures ------------------------------------------------------------------------------
  const delivery = {
    id: 'ASN-2206',
    supplierRoleId: 'Dairy Farms Ltd',
    warehouseCode: 'WH01',
    status: 'Receiving',
    slot: { dockCode: 'D-3' },
    lines: [
      {
        lineNo: 1,
        productCode: '4006381333931',
        expectedQuantity: 240,
        expectedUnit: 'ea',
        batchNumber: 'LOT-0425-A',
        expiryDate: '2026-07-02',
      },
    ],
  };

  const order = { id: 'SO-4471', customerRoleId: 'Fresh Market sp. z o.o.' };

  const putAwayTasks = [
    { sku: '4006381333931', batchNumber: 'LOT-0425-A', quantity: 240, unit: 'ea', fromLocation: 'WH01-DOCK-BUFFER' },
  ];

  const moveTasks = [
    {
      sourceItemId: '11111111-1111-1111-1111-111111111111',
      sku: '4006381333931',
      batchNumber: 'LOT-0425-A',
      quantity: 216,
      unit: 'ea',
      fromLocation: 'WH01-CR1-A03-R2-S1',
      toLocation: 'WH01-CR1-PICKFACE-12',
      bestBefore: '2026-07-02',
      requiresColdChain: true,
      checks: ['Destination is temperature-compatible (same room)', 'Capacity & load limit OK at destination'],
    },
  ];

  const stockRows = [
    { id: 'r1', product: 'Whole milk 3.2% — 1 L carton', sku: '4006381333931', batch: 'LOT-0425-A', bestBefore: '2026-07-02', location: 'WH01-CR1-A03-R2-S1', room: 'CR1', onHand: 240, atp: 216, unit: 'ea', status: 'available' },
    { id: 'r2', product: 'Greek yoghurt 400 g', sku: '5901234123457', batch: 'LOT-0419', bestBefore: '2026-06-28', location: 'WH01-STD-A07-R3-S2', room: 'STD', onHand: 148, atp: 148, unit: 'ea', status: 'reserved' },
    { id: 'r3', product: 'Butter block 250 g', sku: '5900512331027', batch: 'LOT-0331', bestBefore: '2026-06-15', location: 'WH01-CR1-A01-R1-S4', room: 'CR1', onHand: 90, atp: 0, unit: 'ea', status: 'blocked' },
  ];

  const locations = [
    { address: 'WH01-CR1-A03-R2-S1', room: 'CR1', roomType: 'cold' },
    { address: 'WH01-CR1-PICKFACE-12', room: 'CR1', roomType: 'cold' },
    { address: 'WH01-STD-A07-R3-S2', room: 'STD', roomType: 'standard' },
  ];

  const operators: Record<string, { name: string; language: string }> = {
    '7700': { name: 'M. Operator', language: 'en' },
    '7701': { name: 'J. Forklift', language: 'en' },
  };

  // --- Helpers --------------------------------------------------------------------------------------
  const json = (route: Route, data: unknown) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(data) });
  const noContent = (route: Route) => route.fulfill({ status: 204, body: '' });
  const error = (route: Route, status: number, code: string) =>
    route.fulfill({ status, contentType: 'application/json', body: JSON.stringify({ code, message: code }) });

  const tasks = () => [
    { kind: 'receive', detail: `${taskCounts.receive} deliveries to receive`, count: taskCounts.receive },
    { kind: 'putaway', detail: `${taskCounts.putaway} pallets in dock buffer`, count: taskCounts.putaway },
    { kind: 'pick', detail: `${taskCounts.pick} lines to pick`, count: taskCounts.pick },
    { kind: 'move', detail: `${taskCounts.move} tasks to replenish`, count: taskCounts.move },
  ];

  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const method = request.method();
    // Path after the `/api/` prefix, without query string.
    const path = new URL(request.url()).pathname.replace(/^\/api\//, '');

    // Sign-in (anonymous).
    if (method === 'POST' && path === 'auth/login') {
      const badge = (request.postDataJSON() as { badge?: string } | null)?.badge?.trim() ?? '';
      const op = operators[badge];
      if (!op) return error(route, 401, 'unknown_badge');
      return json(route, {
        accessToken: 'e2e-token',
        user: { id: `OP-${badge}`, badge, name: op.name, role: 'operator', email: '', defaultWarehouseId: 'WH01', language: op.language },
      });
    }

    if (method === 'GET' && path === 'terminal/tasks') return json(route, tasks());

    // Inbound / goods receipt.
    if (method === 'GET' && path === 'logistics/deliveries')
      return json(route, [{ id: delivery.id, warehouseCode: delivery.warehouseCode, status: delivery.status, lineCount: delivery.lines.length }]);
    if (method === 'GET' && path === `logistics/deliveries/${delivery.id}`) return json(route, delivery);
    if (method === 'POST' && path === `logistics/deliveries/${delivery.id}/lines/1/receipt`) {
      taskCounts.receive = Math.max(0, taskCounts.receive - 1);
      return noContent(route);
    }
    if (method === 'POST' && path.startsWith(`logistics/deliveries/${delivery.id}/`)) return noContent(route);

    // Put-away.
    if (method === 'GET' && path === 'inventory/put-away/tasks') return json(route, putAwayTasks);
    if (method === 'POST' && path === 'inventory/put-away/confirm') {
      taskCounts.putaway = Math.max(0, taskCounts.putaway - 1);
      return noContent(route);
    }

    // Replenishment moves.
    if (method === 'GET' && path === 'inventory/moves') return json(route, moveTasks);
    if (method === 'POST' && path === 'inventory/moves/confirm') {
      taskCounts.move = Math.max(0, taskCounts.move - 1);
      return noContent(route);
    }

    // Outbound: orders, pick list, packing.
    if (method === 'GET' && path === 'logistics/orders')
      return json(route, [{ id: order.id, warehouseCode: 'WH01', status: 'Picking', lineCount: 1 }]);
    if (method === 'GET' && path === `logistics/orders/${order.id}`) return json(route, order);
    if (method === 'GET' && path === `logistics/orders/${order.id}/pick-list`)
      return json(route, {
        orderId: order.id,
        picked: pickTasks.filter((t) => t.status === 'Picked').length,
        total: pickTasks.length,
        tasks: pickTasks,
      });
    if (method === 'POST' && /^logistics\/orders\/.+\/picks\/\d+\/confirm$/.test(path)) {
      const seq = Number(path.match(/picks\/(\d+)\//)![1]);
      const task = pickTasks.find((t) => t.sequence === seq);
      if (task) task.status = 'Picked';
      taskCounts.pick = Math.max(0, taskCounts.pick - (task?.quantity ?? 1));
      return noContent(route);
    }
    if (method === 'POST' && /^logistics\/orders\/.+\/picks\/\d+\/short$/.test(path)) {
      const seq = Number(path.match(/picks\/(\d+)\//)![1]);
      const task = pickTasks.find((t) => t.sequence === seq);
      if (task) task.status = 'ShortPick';
      return noContent(route);
    }
    if (method === 'POST' && path === `logistics/orders/${order.id}/packed`) return noContent(route);

    // Look up.
    if (method === 'GET' && path === 'inventory/stock/rows') return json(route, stockRows);
    if (method === 'GET' && path === 'inventory/locations') return json(route, locations);

    // Anything else under /api is unexpected — fail loudly rather than hang.
    return route.fulfill({ status: 404, contentType: 'application/json', body: JSON.stringify({ code: 'not_stubbed', path }) });
  });
}

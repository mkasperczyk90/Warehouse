/** English catalogue. The source of keys — `pl` mirrors these. */
export const en: Record<string, string> = {
  // common
  'common.online': 'Online',
  'common.offline': 'Offline',
  'common.loading': 'Loading…',
  'common.loadError': 'Could not load — check the connection.',
  'common.retry': 'Retry',
  'common.cancel': 'Cancel',

  // bottom nav
  'nav.tasks': 'Tasks',
  'nav.scan': 'Scan',
  'nav.lookup': 'Look up',
  'nav.more': 'More',

  // accessibility labels
  'a11y.back': 'Back',
  'a11y.toggleContrast': 'Toggle high-contrast glare mode',
  'a11y.toggleLanguage': 'Switch language',
  'a11y.toggleConnectivity': 'Toggle connectivity',
  'a11y.clearSearch': 'Clear search',
  'a11y.decrease': 'decrease',
  'a11y.increase': 'increase',
  'a11y.enterQty': 'enter quantity',
  'a11y.backspace': 'backspace',
  'a11y.confirmQty': 'confirm quantity',

  // task hub
  'hub.tasksHeading': 'Tasks assigned to you',
  'hub.scanStart': 'Scan a barcode to start…',
  'hub.offline': 'Working offline — confirmations are saved on this device and sync when signal returns.',
  'hub.queued': '{n} queued',

  // task tiles (by kind)
  'tasks.receive': 'Receive',
  'tasks.putaway': 'Put away',
  'tasks.pick': 'Pick',
  'tasks.move': 'Move stock',

  // numeric keypad
  'keypad.expected': '= Expected · {n} {unit}',

  // goods receipt
  'receive.title': 'Goods receipt',
  'receive.line': 'Line {n} of {total}',
  'receive.confirm': 'Confirm line ✓',
  'receive.discrepancy': 'Report discrepancy',
  'receive.scan': 'Scan item barcode…',
  'receive.skuScanned': 'SKU {sku} · EAN scanned ✓',
  'receive.expected': 'Expected on ASN',
  'receive.counted': 'Counted',
  'receive.keyHint': 'Tap the number for the keypad',
  'receive.batch': 'Batch / lot no.',
  'receive.bbe': 'Best before',
  'receive.keypadLabel': '{product} · counted ({unit})',
  'receive.discrepancyTitle': 'Why does the count differ?',
  'receive.reason.shortage': 'Shortage — fewer than expected',
  'receive.reason.overage': 'Overage — more than expected',
  'receive.reason.damage': 'Damaged goods',
  'receive.reason.damageHint': 'Routes the batch to QC quarantine',

  // picking
  'pick.title': 'Picking — Wave {wave}',
  'pick.order': 'Order {order}',
  'pick.confirm': 'Confirm pick ✓',
  'pick.scanToConfirm': 'Scan to confirm',
  'pick.short': 'Short pick',
  'pick.left': '{n} left',
  'pick.goTo': 'Go to',
  'pick.qty': 'Pick quantity',
  'pick.scanLocation': 'Scan location',
  'pick.scanProduct': 'Scan product',
  'pick.promptLocation': 'Scan location…',
  'pick.promptProduct': 'Scan product…',
  'pick.scanned': 'Scanned ✓',
  'pick.shortTitle': 'Why is the pick short?',
  'pick.reason.shortAtLocation': 'Less stock at this location',
  'pick.reason.batchBlocked': 'Batch blocked / on QC hold',
  'pick.reason.batchBlockedHint': 'Replans onto the next FEFO batch',
  'pick.reason.damaged': 'Damaged at location',

  // put-away
  'putaway.title': 'Put-away',
  'putaway.task': 'Task {n} of {total}',
  'putaway.confirm': 'Confirm put-away ✓',
  'putaway.full': 'Location full — propose another',
  'putaway.lpn': 'Pallet LPN {lpn}',
  'putaway.proposed': 'Proposed location',
  'putaway.scan': 'Scan location to confirm…',

  // move
  'move.title': 'Move stock',
  'move.task': 'Replenishment task {n} of {total}',
  'move.confirm': 'Confirm move ✓',
  'move.transfer': 'Inter-warehouse transfer → in-transit',
  'move.from': 'From',
  'move.to': 'To — scan destination',
  'move.skuBatch': 'SKU {sku} · {batch}',
  'move.qty': 'Move quantity',
  'move.scan': 'Scan destination location…',

  // packing
  'pack.title': 'Packing — {order}',
  'pack.customer': 'Customer: {customer}',
  'pack.close': 'Close package & print label',
  'pack.add': '+ Add another package',
  'pack.active': 'Active package',
  'pack.heading': 'Picked items — scan into package',
  'pack.weight': 'Weight',
  'pack.dimensions': 'Dimensions',
  'pack.scan': 'Scan item into package…',

  // scan dispatcher
  'scan.title': 'Scan',
  'scan.subtitle': 'Scan anything — pallet, location, ASN, order or product',
  'scan.placeholder': 'Pull the trigger or type a code…',
  'scan.noAction': 'Nothing to do — try a different code.',
  'scan.recent': 'Recently scanned',
  'scan.unknownSub': 'No matching ASN, order, product, pallet or location',
  'scan.kind.asn': 'Inbound ASN',
  'scan.kind.wave': 'Pick wave',
  'scan.kind.order': 'Outbound order',
  'scan.kind.product': 'Product (EAN)',
  'scan.kind.lpn': 'Pallet (LPN)',
  'scan.kind.location': 'Location',
  'scan.kind.unknown': 'Unrecognised code',
  'scan.action.receive': 'Open goods receipt →',
  'scan.action.pick': 'Open picking →',
  'scan.action.lookup': 'Look up stock & ATP →',
  'scan.action.putaway': 'Put away this pallet →',
  'scan.action.move': 'Move stock from here →',

  // look up
  'lookup.title': 'Look up',
  'lookup.subtitle': 'Search stock, locations and batches — read-only',
  'lookup.placeholder': 'Search SKU, name, location, batch…',
  'lookup.filter.all': 'All',
  'lookup.filter.product': 'Products',
  'lookup.filter.location': 'Locations',
  'lookup.filter.batch': 'Batches',
  'lookup.count': '{n} results',
  'lookup.empty': 'No matches. Try a different term or filter.',

  // stock status (by StatusKey)
  'status.available': 'Available',
  'status.reserved': 'Reserved',
  'status.blocked': 'Blocked (QC)',
  'status.expired': 'Expired',
  'status.transit': 'In transit',
};

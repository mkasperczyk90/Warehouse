/* ============================================================================
   Warehouse WMS prototypes — clickable navigation.
   Shared by every screen. Wires up the admin sidebar, the terminal bottom nav,
   the hub task cards and the back arrows so the mockups are walkable.
   Menu items that have no screen yet are dimmed rather than left as dead links.
   ============================================================================ */
(function () {
  // Menu label (substring) -> target prototype file.
  var ROUTES = {
    'Stock view':    'admin-1-stock.html',
    'Quality holds': 'admin-8-qc.html',
    'Stocktakes':    'admin-3-stocktake.html',
    'Adjustments':   'admin-9-adjustment.html',
    'Inbound':       'admin-2-asn.html',
    'Outbound':      'admin-5-outbound.html',
    'Dispatch':      'admin-6-dispatch.html',
    'Products':      'admin-4-product.html',
    'Topology':      'admin-7-topology.html',
    // terminal task-hub cards
    'Receive':       'terminal-2-receive.html',
    'Put away':      'terminal-3-putaway.html',
    'Move stock':    'terminal-5-move.html',
    'Pick':          'terminal-4-pick.html'
  };
  var HOME = 'terminal-1-hub.html';

  function targetFor(text) {
    text = (text || '').trim();
    for (var key in ROUTES) {
      if (text.indexOf(key) !== -1) return ROUTES[key];
    }
    return null;
  }
  function dim(el) {
    el.style.opacity = '0.45';
    el.style.cursor = 'default';
    el.title = 'Not part of this prototype yet';
  }
  function go(url) { if (url) window.location.href = url; }

  document.addEventListener('DOMContentLoaded', function () {
    // Admin sidebar
    document.querySelectorAll('aside.side a').forEach(function (a) {
      var t = targetFor(a.textContent);
      if (t) { a.setAttribute('href', t); a.style.cursor = 'pointer'; }
      else { dim(a); }
    });

    // Terminal bottom nav (only "Tasks" routes — to the hub)
    document.querySelectorAll('nav.nav a').forEach(function (a) {
      if (a.textContent.indexOf('Tasks') !== -1) {
        a.setAttribute('href', HOME); a.style.cursor = 'pointer';
      } else { dim(a); }
    });

    // Terminal task-hub cards
    document.querySelectorAll('.task').forEach(function (card) {
      var b = card.querySelector('b');
      var t = targetFor(b ? b.textContent : card.textContent);
      if (t) {
        card.style.cursor = 'pointer';
        card.addEventListener('click', function () { go(t); });
      }
    });

    // Back arrows on terminal sub-screens -> hub
    document.querySelectorAll('.bar .back').forEach(function (el) {
      el.style.cursor = 'pointer';
      el.addEventListener('click', function () { go(HOME); });
    });
  });
})();

/* ============================================================================
   High-contrast (glare) theme — operator terminal only.
   Glare on the cold-store floor is a primary driver, so the terminal carries a
   high-contrast toggle. The choice is remembered per device and re-applied on
   every screen (the prototypes navigate with full page loads).
   ============================================================================ */
(function () {
  var KEY = 'wms-hc';
  var root = document.documentElement;
  if (!root.classList.contains('terminal')) return;     // glare theme is the terminal's concern

  function on() { return root.classList.contains('hc'); }
  function apply(v) { root.classList.toggle('hc', v); }
  try { apply(localStorage.getItem(KEY) === '1'); } catch (e) {}

  document.addEventListener('DOMContentLoaded', function () {
    var bar = document.querySelector('.bar');
    if (!bar) return;
    var btn = document.createElement('button');
    btn.type = 'button';
    btn.textContent = '◐';
    btn.title = 'High-contrast (glare) mode';
    btn.setAttribute('aria-label', 'Toggle high-contrast glare mode');
    btn.style.cssText = 'margin-left:auto;flex:0 0 auto;width:40px;height:40px;border-radius:50%;'
      + 'border:2px solid currentColor;color:inherit;font-size:20px;font-weight:800;line-height:1;cursor:pointer;';
    function sync() { btn.style.background = on() ? 'rgba(255,255,255,.28)' : 'transparent'; }
    btn.addEventListener('click', function () {
      apply(!on());
      try { localStorage.setItem(KEY, on() ? '1' : '0'); } catch (e) {}
      sync();
    });
    bar.appendChild(btn);
    sync();
  });
})();

/* ============================================================================
   Inline-SVG icon set — shared by every screen.
   The earlier mockups used bare Unicode glyphs (▣ ▦ ↗ ⚇) as icons, which render
   inconsistently and risk "tofu" boxes on rugged Android handhelds. Any element
   with data-icon="<name>" gets a crisp currentColor SVG, sized by font-size
   (1em). Add new screens/icons by name; nothing else to wire.
   ============================================================================ */
(function () {
  var ICONS = {
    scan:    'M4 8V5.5A1.5 1.5 0 0 1 5.5 4H8 M16 4h2.5A1.5 1.5 0 0 1 20 5.5V8 M20 16v2.5a1.5 1.5 0 0 1-1.5 1.5H16 M8 20H5.5A1.5 1.5 0 0 1 4 18.5V16 M4 12h16',
    receive: 'M12 3v9 M8.5 8.5 12 12l3.5-3.5 M4 14v4.5A1.5 1.5 0 0 0 5.5 20h13a1.5 1.5 0 0 0 1.5-1.5V14',
    putaway: 'M4 4h16v16H4z M4 10h16 M4 15h16 M12 4v16',
    pick:    'M12 21v-9 M8.5 15.5 12 12l3.5 3.5 M4 10V5.5A1.5 1.5 0 0 1 5.5 4h13A1.5 1.5 0 0 1 20 5.5V10',
    move:    'M4 9h13 M14 6l3 3-3 3 M20 15H7 M10 12l-3 3 3 3',
    tasks:   'M9 6h11 M9 12h11 M9 18h11 M4.5 6h.01 M4.5 12h.01 M4.5 18h.01',
    search:  'M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14z M20 20l-3.8-3.8',
    more:    'M4 7h16 M4 12h16 M4 17h16',
    print:   'M7 9V4h10v5 M7 17H5.5A1.5 1.5 0 0 1 4 15.5v-4A1.5 1.5 0 0 1 5.5 10h13a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5H17 M7 14h10v6H7z'
  };
  function svg(name) {
    return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" '
      + 'stroke-linecap="round" stroke-linejoin="round" '
      + 'style="width:1em;height:1em;display:inline-block;vertical-align:middle">'
      + '<path d="' + ICONS[name] + '"/></svg>';
  }
  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-icon]').forEach(function (el) {
      var n = el.getAttribute('data-icon');
      if (ICONS[n]) el.innerHTML = svg(n);
    });
  });
})();

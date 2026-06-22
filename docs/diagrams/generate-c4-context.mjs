// Generates docs/diagrams/c4-context.excalidraw — C4 level 1 (System Context) from post #9.
// Run: node docs/diagrams/generate-c4-context.mjs
import { reset, box, label, arrow, legend, write } from "./_kit.mjs";
reset();

label(40, -90, "C4 · System Context — Warehouse WMS", 32, "#212529");
label(40, -46, "who uses the system, and which external systems it talks to (the view for everyone, incl. sponsors)", 18, "#868e96");

const wms = box(540, 280, 280, 130, "Warehouse WMS\n(this system)", { kind: "command", fontSize: 20, shape: "ellipse" });

// actors (left)
const ax = 120, aw = 220;
const op = box(ax, 110, aw, 64, "Operator\n(scanner terminal)", { kind: "actor", fontSize: 14 });
const mg = box(ax, 230, aw, 64, "Manager /\nCoordinator", { kind: "actor", fontSize: 14 });
const sup = box(ax, 350, aw, 64, "Supplier", { kind: "actor", fontSize: 14 });
const cust = box(ax, 470, aw, 64, "Customer / ERP", { kind: "actor", fontSize: 14 });

// external systems (right)
const ex = 980, ew = 240;
const idp = box(ex, 150, ew, 64, "Identity Provider", { kind: "ext", fontSize: 14 });
const carr = box(ex, 300, ew, 64, "Carrier APIs", { kind: "ext", fontSize: 14 });
const erp = box(ex, 450, ew, 64, "External ERP /\ne-commerce", { kind: "ext", fontSize: 14 });

[op, mg, sup, cust].forEach((a) => arrow(a.x + a.w, a.cy, wms.x, a.cy < wms.cy ? wms.y + 40 : wms.cy, { color: "#868e96" }));
arrow(wms.x + wms.w, wms.cy - 30, idp.x, idp.cy, { color: "#9c36b5", label: "authN/Z" });
arrow(wms.x + wms.w, wms.cy, carr.x, carr.cy, { color: "#9c36b5", label: "book / track" });
arrow(wms.x + wms.w, wms.cy + 30, erp.x, erp.cy, { color: "#9c36b5", label: "orders" });
arrow(erp.x, erp.cy + 18, wms.x + wms.w, wms.cy + 48, { color: "#9c36b5", label: "sync" });

legend(120, 600, [["actor", "Person / role"], ["command", "The system"], ["ext", "External system"]]);

write(new URL("./c4-context.excalidraw", import.meta.url));

// Generates docs/diagrams/clean-architecture.excalidraw — the per-module layering from post #9.
// Run: node docs/diagrams/generate-clean-architecture.mjs
import { reset, box, label, zone, arrow, legend, write } from "./_kit.mjs";
reset();

label(40, -90, "Clean Architecture — one module (e.g. Inventory)", 32, "#212529");
label(40, -46, "the dependency rule: arrows point INWARD, toward a domain that depends on nothing", 18, "#868e96");

const X = 170, W = 380;
zone(90, 80, 540, 540, "module boundary", "#f08c00", { titleSize: 18 });

const api = box(X, 120, W, 70, "API / Endpoints\n(ASP.NET · DTOs · ProblemDetails)", { kind: "command", fontSize: 14 });
const app = box(X, 240, W, 70, "Application\n(use cases · commands · handlers · ports)", { kind: "read", fontSize: 14 });
const dom = box(X, 360, W, 96, "Domain\naggregates · value objects · invariants · domain events\n— zero dependencies", { kind: "domain", fontSize: 14 });
const inf = box(X, 510, W, 70, "Infrastructure\n(EF Core · repositories · messaging · outbox)", { kind: "command", fontSize: 14 });
const sk = box(X + 40, 670, W - 80, 70, "SharedKernel\n(value objects · base types — no deps)", { kind: "policy", fontSize: 14 });

// dependencies point inward / down toward the domain
arrow(api.cx, api.y + api.h, app.cx, app.y, { color: "#1971c2", width: 2 });
arrow(app.cx, app.y + app.h, dom.cx, dom.y, { color: "#2f9e44", width: 2 });
arrow(inf.x + 90, inf.y, app.x + 90, app.y + app.h, { color: "#1971c2", width: 2 });          // INF → APP
arrow(inf.x + W - 90, inf.y, dom.x + W - 90, dom.y + dom.h, { color: "#1971c2", width: 2 });   // INF → DOM
arrow(dom.cx, dom.y + dom.h + 0, sk.cx, sk.y, { color: "#6741d9", width: 2, style: "dashed" });
arrow(app.x + 30, app.y + app.h, sk.x + 30, sk.y, { color: "#6741d9", width: 1.6, style: "dashed" });

legend(700, 130, [["command", "API · Infrastructure (plug-ins)"], ["read", "Application"], ["domain", "Domain (the core)"], ["policy", "SharedKernel"]]);
label(700, 270, "Infrastructure is a plug, not a\nfoundation: the domain and the\nledger don't know EF, the broker\nor ASP.NET exist. Enforced by\narchitecture tests (Part III).", 15, "#495057");

write(new URL("./clean-architecture.excalidraw", import.meta.url));

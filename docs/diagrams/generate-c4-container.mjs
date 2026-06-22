// Generates docs/diagrams/c4-container.excalidraw — C4 level 2 (Containers) from post #9.
// Run: node docs/diagrams/generate-c4-container.mjs
import { reset, box, label, arrow, legend, write } from "./_kit.mjs";
reset();

label(40, -90, "C4 · Containers — Warehouse WMS", 32, "#212529");
label(40, -46, "the independently deployable / runnable pieces and their stores (the view for the dev team)", 18, "#868e96");

// frontends
const spa1 = box(180, 110, 230, 70, "Admin SPA\n(React)", { kind: "ui", fontSize: 14 });
const spa2 = box(470, 110, 230, 70, "Operator terminal\n(React, scanner-first)", { kind: "ui", fontSize: 14 });
// gateway
const gw = box(320, 250, 240, 64, "API Gateway\n(YARP)", { kind: "inf", fontSize: 14 });
// services
const sx = [80, 360, 640], sy = 390, sw = 240;
const svcs = [
  box(sx[0], sy, sw, 84, "warehouse-service\nInventory + Topology", { kind: "svc", fontSize: 14 }),
  box(sx[1], sy, sw, 84, "logistics-service\nLogistics", { kind: "svc", fontSize: 14 }),
  box(sx[2], sy, sw, 84, "masterdata-service\nCatalog + Partners", { kind: "svc", fontSize: 14 }),
];
// databases
const dbs = sx.map((x, i) =>
  box(x, 540, sw, 64, ["Postgres\n(warehouse)", "Postgres\n(logistics)", "Postgres\n(masterdata)"][i], { kind: "db", fontSize: 13 }));
// broker
const mq = box(360, 660, 240, 70, "RabbitMQ\n(integration events · outbox)", { kind: "inf", fontSize: 13, shape: "diamond" });

arrow(spa1.cx, spa1.y + spa1.h, gw.cx - 30, gw.y, { color: "#868e96" });
arrow(spa2.cx, spa2.y + spa2.h, gw.cx + 30, gw.y, { color: "#868e96" });
svcs.forEach((s) => arrow(gw.cx, gw.y + gw.h, s.cx, s.y, { color: "#868e96" }));
svcs.forEach((s, i) => arrow(s.cx, s.y + s.h, dbs[i].cx, dbs[i].y, { color: "#1971c2", head: null, width: 2 }));
svcs.forEach((s) => arrow(s.x + 20, s.y + s.h, mq.cx, mq.y, { color: "#6741d9", style: "dashed", startHead: "arrow" }));

legend(960, 130, [["ui", "Frontend (React SPA)"], ["inf", "Gateway / broker"], ["svc", "Microservice"], ["db", "Database (per service)"]]);

write(new URL("./c4-container.excalidraw", import.meta.url));

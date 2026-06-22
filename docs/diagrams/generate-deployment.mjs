// Generates docs/diagrams/deployment.excalidraw — the deployment diagram from post #9.
// Run: node docs/diagrams/generate-deployment.mjs
import { reset, box, label, zone, arrow, legend, write } from "./_kit.mjs";
reset();

label(40, -90, "Deployment — where the containers run", 32, "#212529");
label(40, -46, "nodes, replicas and failure domains (the view ops/SRE need — the one teams most often forget)", 18, "#868e96");

const cdn = box(120, 110, 250, 64, "Static hosting / CDN\n(the two SPAs)", { kind: "inf", fontSize: 13 });
const idp = box(640, 110, 240, 64, "Identity Provider\n(managed)", { kind: "ext", fontSize: 13 });

// kubernetes cluster
zone(80, 220, 840, 360, "Kubernetes cluster", "#1971c2", { titleSize: 18 });
const ing = box(120, 260, 260, 60, "Ingress + API Gateway", { kind: "inf", fontSize: 14 });
const px = [120, 400, 680], py = 380, pw = 200;
const pods = [
  box(px[0], py, pw, 80, "warehouse-service\n(n replicas)", { kind: "svc", fontSize: 13 }),
  box(px[1], py, pw, 80, "logistics-service\n(n replicas)", { kind: "svc", fontSize: 13 }),
  box(px[2], py, pw, 80, "masterdata-service\n(n replicas)", { kind: "svc", fontSize: 13 }),
];
const mq = box(360, 500, 200, 60, "RabbitMQ", { kind: "inf", fontSize: 14, shape: "diamond" });
const otel = box(640, 500, 200, 60, "OTel Collector", { kind: "inf", fontSize: 13 });

// managed data stores (outside the cluster)
const dbs = px.map((x, i) =>
  box(x, 640, pw, 64, ["Postgres\n(warehouse)", "Postgres\n(logistics)", "Postgres\n(masterdata)"][i], { kind: "db", fontSize: 13 }));

arrow(cdn.cx, cdn.y + cdn.h, ing.cx, ing.y, { color: "#868e96" });
pods.forEach((p) => arrow(ing.cx, ing.y + ing.h, p.cx, p.y, { color: "#868e96" }));
pods.forEach((p, i) => arrow(p.cx, p.y + p.h, dbs[i].cx, dbs[i].y, { color: "#1971c2", head: null, width: 2 }));
pods.forEach((p) => arrow(p.x + 20, p.y + p.h, mq.cx, mq.y, { color: "#6741d9", style: "dashed", startHead: "arrow" }));
pods.forEach((p) => arrow(p.x + p.w - 20, p.y + p.h, otel.cx, otel.y, { color: "#2f9e44", style: "dotted" }));
arrow(ing.x + ing.w, ing.cy, idp.x, idp.cy, { color: "#9c36b5", label: "authN/Z" });

legend(960, 240, [["inf", "Edge / ingress / broker"], ["svc", "Service pod"], ["db", "Managed Postgres"], ["ext", "Managed identity"]]);

write(new URL("./deployment.excalidraw", import.meta.url));

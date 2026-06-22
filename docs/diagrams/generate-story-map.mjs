// Generates docs/diagrams/story-map.excalidraw — the user story map from blog post #8.
// backbone (user activities, business time) × stories × release slices.
// Run: node docs/diagrams/generate-story-map.mjs
import { reset, box, label, zone, arrow, legend, write } from "./_kit.mjs";
reset();

const COLW = 220, X0 = 250, CW = 198, CH = 80;
const acts = [
  "Announce\ndelivery", "Receive\ngoods", "Quality\ncheck", "Put\naway", "Place\norder",
  "Reserve &\nallocate", "Pick", "Pack &\ndispatch", "Stocktake",
];

label(20, -130, "User Story Map — Warehouse WMS", 34, "#212529");
label(20, -82, "backbone = user activities in business time →   ·   columns = stories   ·   rows = release slices", 18, "#868e96");
legend(20, -46, [["command", "Backbone activity"], ["read", "Story (a slice of behaviour)"]]);

const BBY = 150, R1 = 270, R2 = 380, R3 = 490;
const fullW = acts.length * COLW;

// Release-1 = walking skeleton: highlight band.
zone(X0 - 12, R1 - 14, fullW, CH + 28, "", "#2f9e44", { bg: "#ebfbee", strokeStyle: "solid", opacity: 100 });
// lane labels (left margin)
label(20, R1 + 22, "Release 1\nwalking skeleton", 15, "#2f9e44");
label(20, R2 + 28, "Release 2", 15, "#1971c2");
label(20, R3 + 30, "Later", 15, "#868e96");
// lane dividers
arrow(X0 - 12, R2 - 16, X0 - 12 + fullW, R2 - 16, { color: "#ced4da", width: 1.5, head: null, style: "dashed" });
arrow(X0 - 12, R3 - 16, X0 - 12 + fullW, R3 - 16, { color: "#ced4da", width: 1.5, head: null, style: "dashed" });

// backbone row
const bb = acts.map((t, i) => box(X0 + i * COLW, BBY, CW, 64, t, { kind: "command", fontSize: 14 }));
for (let i = 0; i < bb.length - 1; i++)
  arrow(bb[i].x + bb[i].w, bb[i].cy, bb[i + 1].x, bb[i + 1].cy, { color: "#adb5bd", width: 2 });

// [activityIndex, release 1..3, text]
const stories = [
  [0, 1, "Create ASN\n(supplier / coordinator)"],
  [1, 1, "Receive announced delivery,\nrecord batches"], [1, 2, "Discrepancies / shortages;\nad-hoc ASN"], [1, 3, "Inter-warehouse transfer"],
  [2, 1, "Quarantine / release batch"],
  [3, 1, "PutAwayPolicy\n(temperature hard-stop)"], [3, 2, "Capacity reroute;\nconsolidation"], [3, 3, "Handling units (LPN) moves"],
  [4, 1, "Create order,\nsoft-reserve within ATP"], [4, 2, "Partial / backorder"], [4, 3, "Customer-specific rules"],
  [5, 1, "Hard-allocate FEFO\nat wave"], [5, 2, "Re-check batch quality\nat commit"], [5, 3, "Reservation expiry"],
  [6, 1, "Confirm pick (scan)"], [6, 2, "Short-pick replan;\nrouting"], [6, 3, "Wave optimisation"],
  [7, 1, "Confirm dispatch,\ndrop stock"], [7, 2, "Carrier integration,\ntracking"], [7, 3, "Multi-parcel, labels,\ncustoms"],
  [8, 3, "Blind count →\nledger adjustment"],
];
const RY = { 1: R1, 2: R2, 3: R3 };
stories.forEach(([ai, rel, t]) => box(X0 + ai * COLW, RY[rel], CW, CH, t, { kind: "read", fontSize: 12 }));

write(new URL("./story-map.excalidraw", import.meta.url));

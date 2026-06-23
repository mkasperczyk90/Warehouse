import { chromium } from 'playwright';
const b = await chromium.launch();
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
const p = await ctx.newPage();
const pages = ['/today', '/stock', '/products', '/topology', '/quality'];
const lefts = [];
for (const path of pages) {
  await p.goto('http://localhost:5173' + path, { waitUntil: 'networkidle' });
  await p.waitForTimeout(400);
  const box = await p.locator('input[aria-label*="ASN, order"]').boundingBox();
  lefts.push({ path, left: Math.round(box.left), width: Math.round(box.width) });
}
console.log('global search box per page (left should be identical):');
for (const l of lefts) console.log(`  ${l.path.padEnd(12)} left=${l.left} width=${l.width}`);
const uniq = new Set(lefts.map(l => l.left));
console.log(uniq.size === 1 ? 'OK — search bar STABLE across pages' : `STILL SHIFTS — lefts: ${[...uniq].join(', ')}`);
const ctx2 = await b.newContext({ viewport: { width: 1280, height: 800 } });
const p2 = await ctx2.newPage();
await p2.goto('http://localhost:5173/inbound', { waitUntil: 'networkidle' });
await p2.waitForTimeout(500);
const o = await p2.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
console.log(`[1280] inbound document overflowX = ${o} (0 = no page break)`);
await b.close();

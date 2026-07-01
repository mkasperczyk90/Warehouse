#!/usr/bin/env node
// Turn the Playwright recordings (test-results/**/*.webm) into shareable GIFs +
// MP4s in ./output. Two-pass palette for clean GIF colour. Optionally stitches
// the admin + terminal takes into one end-to-end clip.
//
// Usage: node make-gif.mjs            # per-app gif+mp4, and a combined gif if both exist
//        DEMO_GIF_FPS=12 DEMO_GIF_W=720 node make-gif.mjs

import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readdirSync, statSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const RESULTS = path.join(HERE, 'test-results');
// Final GIFs/MP4s land in the repo's docs/media (override with DEMO_OUT_DIR).
const APP_ROOT = process.env.DEMO_APP_ROOT ? path.resolve(process.env.DEMO_APP_ROOT) : path.resolve(HERE, '..', '..');
const OUT = process.env.DEMO_OUT_DIR ? path.resolve(process.env.DEMO_OUT_DIR) : path.join(APP_ROOT, 'docs', 'media');

const FPS = Number(process.env.DEMO_GIF_FPS ?? '12');
const GIF_W = Number(process.env.DEMO_GIF_W ?? '0'); // 0 = native (per app default below)

/** Recursively collect every .webm under a directory. */
function findWebm(dir) {
  if (!existsSync(dir)) return [];
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...findWebm(full));
    else if (entry.name.endsWith('.webm')) out.push(full);
  }
  return out;
}

function newest(files) {
  return files.sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs)[0];
}

function ff(args) {
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', ...args], { stdio: 'inherit' });
}

function toGif(src, dest, width) {
  const vf = `fps=${FPS},scale=${width}:-1:flags=lanczos`;
  const palette = dest.replace(/\.gif$/, '.palette.png');
  ff(['-i', src, '-vf', `${vf},palettegen=stats_mode=diff`, palette]);
  ff(['-i', src, '-i', palette, '-lavfi', `${vf}[x];[x][1:v]paletteuse=dither=bayer:bayer_scale=3`, dest]);
  try {
    execFileSync(process.platform === 'win32' ? 'cmd' : 'rm', process.platform === 'win32' ? ['/c', 'del', '/q', palette] : [palette]);
  } catch {
    /* leftover palette is harmless */
  }
  console.log(`  → ${path.relative(HERE, dest)}`);
}

function toMp4(src, dest) {
  ff(['-i', src, '-movflags', '+faststart', '-pix_fmt', 'yuv420p', '-vf', 'scale=trunc(iw/2)*2:trunc(ih/2)*2', dest]);
  console.log(`  → ${path.relative(HERE, dest)}`);
}

function main() {
  const webms = findWebm(RESULTS);
  if (webms.length === 0) {
    console.error('No recordings found under test-results/. Run `npm run record` first.');
    process.exit(1);
  }
  mkdirSync(OUT, { recursive: true });

  const adminSrc = newest(webms.filter((f) => /admin/i.test(f)));
  const terminalSrc = newest(webms.filter((f) => /terminal/i.test(f)));

  if (adminSrc) {
    console.log('Admin take:');
    toGif(adminSrc, path.join(OUT, 'admin-golden-path.gif'), GIF_W || 760);
    toMp4(adminSrc, path.join(OUT, 'admin-golden-path.mp4'));
  }
  if (terminalSrc) {
    console.log('Terminal take:');
    toGif(terminalSrc, path.join(OUT, 'terminal-golden-path.gif'), GIF_W || 390);
    toMp4(terminalSrc, path.join(OUT, 'terminal-golden-path.mp4'));
  }

  // Combined end-to-end: admin act, then terminal act, padded to one canvas.
  // It's the longest clip, so keep it modest (a lower fps/size) — the per-app
  // MP4s are the high-quality artefacts.
  if (adminSrc && terminalSrc) {
    console.log('Combined end-to-end:');
    const W = 720;
    const H = 450;
    const cfps = Math.min(FPS, 10);
    const combined = path.join(OUT, 'golden-path-full.gif');
    const palette = combined.replace(/\.gif$/, '.palette.png');
    const pad = (i) =>
      `[${i}:v]scale=${W}:${H}:force_original_aspect_ratio=decrease,pad=${W}:${H}:(ow-iw)/2:(oh-ih)/2:color=#0b0b0e,setsar=1,fps=${cfps}[v${i}]`;
    const chain = `${pad(0)};${pad(1)};[v0][v1]concat=n=2:v=1:a=0[c]`;
    ff(['-i', adminSrc, '-i', terminalSrc, '-filter_complex', `${chain};[c]palettegen=stats_mode=diff[p]`, '-map', '[p]', palette]);
    ff([
      '-i', adminSrc, '-i', terminalSrc, '-i', palette,
      '-filter_complex', `${chain};[c][2:v]paletteuse=dither=bayer:bayer_scale=3[g]`,
      '-map', '[g]', combined,
    ]);
    try {
      execFileSync(process.platform === 'win32' ? 'cmd' : 'rm', process.platform === 'win32' ? ['/c', 'del', '/q', palette] : [palette]);
    } catch {
      /* ignore */
    }
    console.log(`  → ${path.relative(HERE, combined)}`);
  }

  console.log('\nDone. GIFs + MP4s are in ./output');
}

main();

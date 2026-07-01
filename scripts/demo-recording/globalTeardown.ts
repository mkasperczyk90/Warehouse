import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));

/** After the take(s), convert the webm recordings to GIFs + MP4s. */
export default function globalTeardown(): void {
  if (process.env.DEMO_NO_GIF) return;
  try {
    execFileSync('node', [path.join(here, 'make-gif.mjs')], { stdio: 'inherit', env: process.env });
  } catch (err) {
    console.warn('GIF conversion failed (recordings are still in test-results/):', (err as Error).message);
  }
}

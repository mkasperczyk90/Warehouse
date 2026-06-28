/**
 * Friendly, human-readable reference for a backend aggregate id (mirrors the admin helper).
 *
 * Deliveries/orders are keyed by GUID server-side; operators shouldn't read a GUID, so the terminal
 * shows a short prefixed code (e.g. `ASN-4821`) derived deterministically from the id. The GUID stays
 * the source of truth for API calls — this is presentation only. A non-GUID id (e.g. an e2e stub or an
 * already-friendly code) passes through unchanged.
 */
export function humanRef(prefix: string, id: string | null | undefined): string {
  if (!id) return '—';
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-/i.test(id)) return id;
  let hash = 0;
  for (let i = 0; i < id.length; i += 1) {
    hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
  }
  return `${prefix}-${(hash % 9000) + 1000}`;
}

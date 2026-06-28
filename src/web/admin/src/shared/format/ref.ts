/**
 * Friendly, human-readable reference for a backend aggregate id.
 *
 * The services key deliveries/orders/shipments by GUID (UUID v7). Operators don't want to read a GUID,
 * so the UI shows a short prefixed code (e.g. `ASN-4821`) derived deterministically from the id — the
 * same record reads the same everywhere (list, detail, terminal), without a backend round-trip. The
 * GUID stays the source of truth for routing and API calls; this is presentation only.
 */
export function humanRef(prefix: string, id: string | null | undefined): string {
  if (!id) return '—';
  // If it isn't a GUID (already a friendly code, e.g. from MSW fixtures), show it as-is.
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-/i.test(id)) return id;
  let hash = 0;
  for (let i = 0; i < id.length; i += 1) {
    hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
  }
  return `${prefix}-${(hash % 9000) + 1000}`; // stable 4-digit code, 1000–9999
}

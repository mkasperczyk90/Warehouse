import type { Href } from 'expo-router';

/**
 * Route definitions — the single source of truth for terminal paths.
 *
 * With expo-router the file tree under `app/` *is* the router, so the route
 * files there stay thin (they just re-export a feature screen). This map keeps
 * the actual path strings in one typed place so features navigate by name
 * (`ROUTES.pack`) instead of sprinkling string literals.
 */
export const ROUTES = {
  hub: '/',
  receive: '/receive',
  putaway: '/putaway',
  pick: '/pick',
  move: '/move',
  pack: '/pack',
  scan: '/scan',
  lookup: '/lookup',
} satisfies Record<string, Href>;

export type RouteName = keyof typeof ROUTES;

/** The three top-level tabs surfaced in the bottom nav. */
export type TabKey = 'tasks' | 'scan' | 'lookup';

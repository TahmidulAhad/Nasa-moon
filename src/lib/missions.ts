import missionsData from '../data/missions.json';
import type { Mission } from './types';
import { getMissions, isApiConfigured } from './api';

// Hides the JSON import: components never touch the raw file (RULES §3).
// The cast is safe because validate.test.ts checks every record against the schema.
//
// Data source: `missions` starts as the bundled static data, so the site
// always works. When VITE_API_URL is set, main.tsx awaits initMissions()
// BEFORE the app renders — the API data replaces the static data and every
// module that reads `missions` at import time sees the API version. If the
// API is unreachable or returns nothing, the static data stays in place.
const staticMissions: Mission[] = missionsData as unknown as Mission[];

export let missions: Mission[] = staticMissions;

/** Where the current `missions` came from: 'static' (bundled JSON) or 'api'. */
export let missionsSource: 'static' | 'api' = 'static';

/** Try to load missions from the API; silently keep the static data on failure. */
export async function initMissions(): Promise<void> {
  if (!isApiConfigured()) return;
  try {
    const apiMissions = await getMissions();
    if (apiMissions.length > 0) {
      missions = apiMissions;
      missionsSource = 'api';
    }
  } catch {
    // API unreachable — keep the bundled static data.
  }
}

export function getMissionById(id: string): Mission | undefined {
  return missions.find((m) => m.id === id);
}

export function launchYear(m: Mission): number | null {
  return m.launchDate ? Number(m.launchDate.slice(0, 4)) : null;
}

export function payloadCount(m: Mission): number | null {
  return m.payloads.length > 0 ? m.payloads.length : m.payloadCountReported;
}

export function payloadCountDiffers(m: Mission): boolean {
  return m.payloadCountReported !== null && m.payloadCountReported !== m.payloads.length;
}

export function hasMapPosition(m: Mission): boolean {
  return m.landingSite?.latitude != null && m.landingSite.longitude != null;
}

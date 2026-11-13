import type { Mission, MissionStatus, Payload, Source } from './types';

// ---------------------------------------------------------------------------
// API client for the ASP.NET Core backend (backend/src/Api).
// Base URL comes from VITE_API_URL (see .env.example). When it is unset the
// app never calls the API — src/lib/missions.ts keeps the bundled static
// missions.json as the data source, so the site never breaks.
// ---------------------------------------------------------------------------

/** Base URL of the API, e.g. http://localhost:5083. Empty when not configured. */
export function getApiBaseUrl(): string {
  return ((import.meta.env.VITE_API_URL as string | undefined) ?? '').replace(/\/+$/, '');
}

export function isApiConfigured(): boolean {
  return getApiBaseUrl().length > 0;
}

/** How long to wait for the API before falling back to the static data. */
const FETCH_TIMEOUT_MS = 3000;

// --- API DTO shapes (backend MissionDto / PayloadDto / SourceDto, camelCase) ---

interface LandingSiteDto {
  name: string | null;
  latitude: number | null;
  longitude: number | null;
  region: string | null;
  coordinatesApproximate: boolean;
}

interface PayloadDto {
  id: number;
  name: string;
  organization: string | null;
  purpose: string | null;
  type: string | null;
}

interface SourceDto {
  id: number;
  label: string;
  url: string;
  accessed: string | null;
}

export interface MissionDto {
  id: string;
  name: string;
  taskOrder: string | null;
  provider: string | null;
  lander: string | null;
  status: string;
  launchDate: string | null;
  launchDatePrecision: string | null;
  landingDate: string | null;
  landingDatePrecision: string | null;
  landingSite: LandingSiteDto | null;
  payloadCountReported: number | null;
  objective: string | null;
  outcomeSummary: string | null;
  notes: string | null;
  lastVerified: string | null;
  payloads: PayloadDto[];
  sources: SourceDto[];
}

async function fetchJson<T>(path: string): Promise<T> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);
  try {
    const res = await fetch(`${getApiBaseUrl()}${path}`, { signal: controller.signal });
    if (!res.ok) {
      throw new Error(`API ${res.status} for ${path}`);
    }
    return (await res.json()) as T;
  } finally {
    clearTimeout(timeout);
  }
}

const VALID_STATUSES: MissionStatus[] = [
  'upcoming',
  'in_flight',
  'completed',
  'partial',
  'failed',
  'cancelled',
];

/** Map a backend MissionDto to the exact Mission shape the UI already expects. */
export function mapMissionDto(dto: MissionDto): Mission {
  const status: MissionStatus = (VALID_STATUSES as string[]).includes(dto.status)
    ? (dto.status as MissionStatus)
    : 'upcoming';
  const payloads: Payload[] = (dto.payloads ?? []).map((p) => ({
    name: p.name,
    organization: p.organization,
    purpose: p.purpose,
    type: p.type,
  }));
  const sources: Source[] = (dto.sources ?? []).map((s) => ({
    label: s.label,
    url: s.url,
    accessed: s.accessed ?? '',
  }));
  return {
    id: dto.id,
    name: dto.name,
    taskOrder: dto.taskOrder,
    provider: dto.provider,
    lander: dto.lander,
    status,
    launchDate: dto.launchDate,
    launchDatePrecision: (dto.launchDatePrecision ?? null) as Mission['launchDatePrecision'],
    landingDate: dto.landingDate,
    landingDatePrecision: (dto.landingDatePrecision ?? null) as Mission['landingDatePrecision'],
    landingSite: dto.landingSite
      ? {
          name: dto.landingSite.name,
          latitude: dto.landingSite.latitude,
          longitude: dto.landingSite.longitude,
          region: dto.landingSite.region,
          coordinatesApproximate: dto.landingSite.coordinatesApproximate,
        }
      : null,
    payloads,
    payloadCountReported: dto.payloadCountReported,
    objective: dto.objective,
    outcomeSummary: dto.outcomeSummary,
    notes: dto.notes,
    sources,
    lastVerified: dto.lastVerified ?? '',
  };
}

export interface MissionQuery {
  search?: string;
  status?: string;
  provider?: string;
  launchYear?: number;
  region?: string;
}

/** All missions from the API (optionally server-side searched/filtered). */
export async function getMissions(query: MissionQuery = {}): Promise<Mission[]> {
  const params = new URLSearchParams();
  if (query.search) params.set('search', query.search);
  if (query.status) params.set('status', query.status);
  if (query.provider) params.set('provider', query.provider);
  if (query.launchYear !== undefined) params.set('launchYear', String(query.launchYear));
  if (query.region) params.set('region', query.region);
  const qs = params.toString();
  const dtos = await fetchJson<MissionDto[]>(`/api/missions${qs ? `?${qs}` : ''}`);
  return dtos.map(mapMissionDto);
}

/** One mission from the API; undefined when it does not exist. */
export async function getMissionById(id: string): Promise<Mission | undefined> {
  try {
    const dto = await fetchJson<MissionDto>(`/api/missions/${encodeURIComponent(id)}`);
    return mapMissionDto(dto);
  } catch (err) {
    if (err instanceof Error && err.message.includes('API 404')) return undefined;
    throw err;
  }
}

/** Payloads of one mission from the API. */
export async function getPayloads(missionId: string): Promise<Payload[]> {
  const dtos = await fetchJson<PayloadDto[]>(
    `/api/missions/${encodeURIComponent(missionId)}/payloads`,
  );
  return dtos.map((p) => ({
    name: p.name,
    organization: p.organization,
    purpose: p.purpose,
    type: p.type,
  }));
}

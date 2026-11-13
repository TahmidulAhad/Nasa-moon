import type { Mission } from './types';
import { authFetch, authFetchRaw } from './auth';
import { getApiBaseUrl, mapMissionDto } from './api';
import type { MissionDto, SnapshotSummary } from './api';

// ---------------------------------------------------------------------------
// My Library, collections, and admin views — the authenticated API calls
// behind /my-library, /collections, /profile and /admin. Every row the API
// returns here belongs to the logged-in researcher; other researchers'
// rows are invisible (the API enforces that, not the UI).
// ---------------------------------------------------------------------------

export interface SavedItem {
  id: string;
  missionId: string;
  missionName: string;
  missionStatus: string;
  missionProvider: string | null;
  note: string | null;
  tags: string[];
  createdAt: string;
}

export interface Collection {
  id: string;
  name: string;
  description: string | null;
  itemCount: number;
  createdAt: string;
}

export interface CollectionDetail extends Collection {
  items: SavedItem[];
}

export interface AuditLog {
  id: string;
  userId: string | null;
  userEmail: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  createdAt: string;
}

export type ExportFormat = 'json' | 'csv';

/** My saved missions, newest first. `search` matches name, note or tag. */
export async function listSaved(search?: string): Promise<SavedItem[]> {
  const q = search?.trim()
    ? `?search=${encodeURIComponent(search.trim())}`
    : '';
  return authFetch<SavedItem[]>(`/api/library${q}`);
}

/** Save a mission (idempotent — saving twice returns the same row). */
export async function saveMission(
  missionId: string,
  note?: string,
  tags?: string[],
): Promise<SavedItem> {
  return authFetch<SavedItem>('/api/library', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ missionId, note, tags }),
  });
}

/** Change my note / replace my tags on a saved item. */
export async function updateSavedItem(
  id: string,
  note: string | null,
  tags: string[],
): Promise<SavedItem> {
  return authFetch<SavedItem>(`/api/library/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ note, tags }),
  });
}

/** Remove a mission from my library. */
export async function unsaveMission(id: string): Promise<void> {
  await authFetch(`/api/library/${encodeURIComponent(id)}`, { method: 'DELETE' });
}

/** My collections (folders of missions), newest first. */
export async function listCollections(): Promise<Collection[]> {
  return authFetch<Collection[]>('/api/collections');
}

export async function createCollection(
  name: string,
  description?: string,
): Promise<Collection> {
  return authFetch<Collection>('/api/collections', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, description }),
  });
}

export async function getCollection(id: string): Promise<CollectionDetail> {
  return authFetch<CollectionDetail>(`/api/collections/${encodeURIComponent(id)}`);
}

export async function updateCollection(
  id: string,
  name: string,
  description: string | null,
): Promise<Collection> {
  return authFetch<Collection>(`/api/collections/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, description }),
  });
}

export async function deleteCollection(id: string): Promise<void> {
  await authFetch(`/api/collections/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function addToCollection(
  collectionId: string,
  missionId: string,
): Promise<void> {
  await authFetch(`/api/collections/${encodeURIComponent(collectionId)}/items`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ missionId }),
  });
}

export async function removeFromCollection(
  collectionId: string,
  missionId: string,
): Promise<void> {
  await authFetch(
    `/api/collections/${encodeURIComponent(collectionId)}/items/${encodeURIComponent(missionId)}`,
    { method: 'DELETE' },
  );
}

/** Download a file the API returns, using the filename it suggests. */
async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const res = await authFetchRaw(path);
  const blob = await res.blob();
  const disposition = res.headers.get('content-disposition') ?? '';
  const match = /filename[^;=\n]*=(?:UTF-8'')?"?([^";\n]+)"?/.exec(disposition);
  const name = match?.[1] ? decodeURIComponent(match[1]) : fallbackName;
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

/** Download my whole library (saved items + collections + notes). */
export async function exportLibrary(format: ExportFormat): Promise<void> {
  await downloadFile(
    `/api/library/export?format=${format}`,
    `my-library.${format}`,
  );
}

/** Download one collection. */
export async function exportCollection(
  collectionId: string,
  format: ExportFormat,
): Promise<void> {
  await downloadFile(
    `/api/collections/${encodeURIComponent(collectionId)}/export?format=${format}`,
    `collection.${format}`,
  );
}

/** Download one snapshot's missions. */
export async function exportSnapshot(
  snapshotId: string,
  format: ExportFormat,
): Promise<void> {
  await downloadFile(
    `/api/snapshots/${encodeURIComponent(snapshotId)}/export?format=${format}`,
    `snapshot.${format}`,
  );
}

// --- Admin only ------------------------------------------------------------

/** All researcher accounts (no password hashes — the API never returns them). */
export async function listUsers(): Promise<
  Array<{ id: string; email: string; displayName: string; role: string; createdAt: string }>
> {
  return authFetch('/api/admin/users');
}

/** Newest audit entries first. */
export async function listAuditLogs(count = 100): Promise<AuditLog[]> {
  return authFetch<AuditLog[]>(`/api/admin/audit-logs?count=${count}`);
}

/** All missions from the API (admin view; same data as the public list). */
export async function listAllMissions(): Promise<Mission[]> {
  const dtos = await authFetch<MissionDto[]>('/api/missions');
  return dtos.map(mapMissionDto);
}

export async function createMission(
  mission: Omit<MissionDto, 'payloads' | 'sources'> & {
    payloads?: MissionDto['payloads'];
    sources?: MissionDto['sources'];
  },
): Promise<Mission> {
  const dto = await authFetch<MissionDto>('/api/missions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(mission),
  });
  return mapMissionDto(dto);
}

export async function updateMission(
  id: string,
  mission: Partial<MissionDto>,
): Promise<Mission> {
  const dto = await authFetch<MissionDto>(`/api/missions/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(mission),
  });
  return mapMissionDto(dto);
}

export async function deleteMission(id: string): Promise<void> {
  await authFetch(`/api/missions/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

/** A researcher's own snapshots (for the profile page counts). */
export async function listMySnapshots(): Promise<SnapshotSummary[]> {
  return authFetch<SnapshotSummary[]>('/api/snapshots');
}

/** The public API base URL — shown on the login page when it is unset. */
export function apiBaseUrl(): string {
  return getApiBaseUrl();
}

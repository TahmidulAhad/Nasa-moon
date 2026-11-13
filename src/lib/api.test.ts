import { afterEach, describe, expect, it, vi } from 'vitest';
import { getMissions, getMissionById, getPayloads, mapMissionDto } from './api';
import type { MissionDto } from './api';
import { initMissions, missions, missionsSource } from './missions';

const dto: MissionDto = {
  id: 'test-1',
  name: 'Test Mission 1',
  taskOrder: 'TO-TEST',
  provider: 'Test Provider',
  lander: 'Test Lander',
  status: 'upcoming',
  launchDate: '2027-03-15',
  launchDatePrecision: 'day',
  landingDate: null,
  landingDatePrecision: null,
  landingSite: {
    name: 'Test Site',
    latitude: -85.0,
    longitude: 10.0,
    region: 'south polar',
    coordinatesApproximate: true,
  },
  payloadCountReported: 2,
  objective: 'Test objective',
  outcomeSummary: null,
  notes: null,
  lastVerified: '2026-11-13',
  payloads: [
    { id: 1, name: 'Payload A', organization: 'Org A', purpose: 'P', type: 'T' },
    { id: 2, name: 'Payload B', organization: null, purpose: null, type: null },
  ],
  sources: [{ id: 1, label: 'NASA', url: 'https://example.com', accessed: '2026-11-13' }],
};

function mockFetch(body: unknown, ok = true, status = 200) {
  return vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue({
      ok,
      status,
      json: async () => body,
    } as Response),
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('mapMissionDto', () => {
  it('maps the API shape to the exact Mission shape the UI expects', () => {
    const m = mapMissionDto(dto);
    expect(m.id).toBe('test-1');
    expect(m.status).toBe('upcoming');
    expect(m.launchDate).toBe('2027-03-15');
    expect(m.landingSite?.region).toBe('south polar');
    expect(m.payloads).toHaveLength(2);
    // API-only ids are stripped: UI Payload/Source have no id field.
    expect(m.payloads[0]).toEqual({ name: 'Payload A', organization: 'Org A', purpose: 'P', type: 'T' });
    expect(m.sources[0]).toEqual({ label: 'NASA', url: 'https://example.com', accessed: '2026-11-13' });
    expect(m.lastVerified).toBe('2026-11-13');
  });

  it('falls back to upcoming for an unknown status and nulls for a missing site', () => {
    const m = mapMissionDto({ ...dto, status: 'weird', landingSite: null, lastVerified: null });
    expect(m.status).toBe('upcoming');
    expect(m.landingSite).toBeNull();
    expect(m.lastVerified).toBe('');
  });
});

describe('API client (mocked fetch)', () => {
  it('getMissions calls /api/missions and maps the list', async () => {
    mockFetch([dto]);
    const list = await getMissions();
    expect(list).toHaveLength(1);
    expect(list[0].name).toBe('Test Mission 1');
    expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/api/missions'), expect.anything());
  });

  it('getMissions forwards search/filter as query params', async () => {
    mockFetch([]);
    await getMissions({ search: 'peregrine', status: 'failed', launchYear: 2024 });
    const url = String((fetch as unknown as ReturnType<typeof vi.fn>).mock.calls[0][0]);
    expect(url).toContain('search=peregrine');
    expect(url).toContain('status=failed');
    expect(url).toContain('launchYear=2024');
  });

  it('getMissionById returns undefined on 404', async () => {
    mockFetch(null, false, 404);
    await expect(getMissionById('nope')).resolves.toBeUndefined();
  });

  it('getPayloads calls /api/missions/:id/payloads', async () => {
    mockFetch([{ id: 1, name: 'Payload A', organization: null, purpose: null, type: null }]);
    const payloads = await getPayloads('test-1');
    expect(payloads).toEqual([{ name: 'Payload A', organization: null, purpose: null, type: null }]);
  });

  it('initMissions keeps the static data when the API is unreachable', async () => {
    vi.stubEnv('VITE_API_URL', 'http://localhost:5083');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network down')));
    const before = missions.length;
    await initMissions();
    expect(missions).toHaveLength(before);
    expect(missionsSource).toBe('static');
    vi.unstubAllEnvs();
  });
});

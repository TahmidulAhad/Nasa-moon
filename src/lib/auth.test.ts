import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import {
  authFetch,
  clearSession,
  getAccessToken,
  getCurrentUser,
  isAdmin,
  isLoggedIn,
  saveSession,
} from './auth';
import type { AuthResponse } from './auth';

const SESSION: AuthResponse = {
  accessToken: 'access-1',
  refreshToken: 'refresh-1',
  user: {
    id: 'user-1',
    email: 'researcher@test.local',
    displayName: 'Researcher Two',
    role: 'Researcher',
    createdAt: '2026-11-13T00:00:00Z',
  },
};

const ADMIN_SESSION: AuthResponse = {
  ...SESSION,
  accessToken: 'access-admin',
  user: { ...SESSION.user, id: 'user-0', email: 'admin@test.local', role: 'Admin' },
};

// The API base is read from import.meta.env at call time; vitest sets it
// through the Vite config env, so pin it here for the fetch tests.
vi.stubEnv('VITE_API_URL', 'http://localhost:5083');

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(typeof body === 'string' ? body : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('session storage', () => {
  beforeEach(() => {
    clearSession();
  });

  it('starts logged out', () => {
    expect(isLoggedIn()).toBe(false);
    expect(getCurrentUser()).toBeNull();
    expect(getAccessToken()).toBeNull();
  });

  it('saveSession stores tokens and the user', () => {
    saveSession(SESSION);
    expect(isLoggedIn()).toBe(true);
    expect(getAccessToken()).toBe('access-1');
    expect(getCurrentUser()?.email).toBe('researcher@test.local');
    expect(isAdmin()).toBe(false);
  });

  it('isAdmin is true for the Admin role', () => {
    saveSession(ADMIN_SESSION);
    expect(isAdmin()).toBe(true);
  });

  it('clearSession forgets everything', () => {
    saveSession(SESSION);
    clearSession();
    expect(isLoggedIn()).toBe(false);
    expect(getCurrentUser()).toBeNull();
  });
});

describe('authFetch with refresh retry', () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    clearSession();
    saveSession(SESSION);
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends the Bearer token and parses JSON', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ ok: true }));
    const result = await authFetch<{ ok: boolean }>('/api/library');
    expect(result).toEqual({ ok: true });
    const [, init] = fetchMock.mock.calls[0];
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer access-1');
  });

  it('refreshes once on 401 and retries with the new token', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response('', { status: 401 }))
      .mockResolvedValueOnce(
        jsonResponse({ ...SESSION, accessToken: 'access-2', refreshToken: 'refresh-2' }),
      )
      .mockResolvedValueOnce(jsonResponse({ ok: true }));
    const result = await authFetch<{ ok: boolean }>('/api/library');
    expect(result).toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledTimes(3);
    const refreshCall = fetchMock.mock.calls[1];
    expect(refreshCall[0]).toContain('/api/auth/refresh');
    expect(getAccessToken()).toBe('access-2');
    const [, retryInit] = fetchMock.mock.calls[2];
    expect((retryInit.headers as Headers).get('Authorization')).toBe('Bearer access-2');
  });

  it('clears the session when the refresh token is rejected', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response('', { status: 401 }))
      .mockResolvedValueOnce(new Response('bad token', { status: 401 }));
    await expect(authFetch('/api/library')).rejects.toMatchObject({ status: 401 });
    expect(isLoggedIn()).toBe(false);
    expect(getCurrentUser()).toBeNull();
  });

  it('throws ApiError with the status for other failures', async () => {
    fetchMock.mockResolvedValueOnce(new Response('Forbidden', { status: 403 }));
    await expect(authFetch('/api/missions')).rejects.toMatchObject({ status: 403 });
  });

  it('resolves undefined for 204 No Content', async () => {
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    await expect(authFetch('/api/library/abc')).resolves.toBeUndefined();
  });
});

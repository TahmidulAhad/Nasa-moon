import { getApiBaseUrl, isApiConfigured } from './apiConfig';

// ---------------------------------------------------------------------------
// Researcher accounts — auth client for the ASP.NET Core backend.
// The access token (JWT, 1h) and refresh token (7d) live in localStorage;
// the refresh token is rotated on every use and only its hash is stored
// server-side. When a request gets a 401, authFetch tries one refresh and
// retries once; if the refresh also fails the session is cleared and the
// caller sees a 401 (pages send the user to /login).
// ---------------------------------------------------------------------------

export interface User {
  id: string;
  email: string;
  displayName: string;
  role: 'Researcher' | 'Admin';
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

const ACCESS_KEY = 'nasamoon_access_token';
const REFRESH_KEY = 'nasamoon_refresh_token';
const USER_KEY = 'nasamoon_user';

// localStorage is unavailable in some environments (vitest's node env,
// private mode). Fall back to an in-memory store so the session logic
// still works — it just will not survive a page reload there.
const memoryStore = new Map<string, string>();
type Store = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;
function store(): Store {
  try {
    if (typeof localStorage !== 'undefined') return localStorage;
  } catch {
    // localStorage exists but is blocked (private mode) — use memory.
  }
  return {
    getItem: (key: string) => memoryStore.get(key) ?? null,
    setItem: (key: string, value: string) => {
      memoryStore.set(key, value);
    },
    removeItem: (key: string) => {
      memoryStore.delete(key);
    },
  };
}

/** How long to wait for an auth call before giving up (Render cold start). */
const AUTH_TIMEOUT_MS = 15000;

export function isLoggedIn(): boolean {
  return getAccessToken() !== null;
}

export function getAccessToken(): string | null {
  try {
    return store().getItem(ACCESS_KEY);
  } catch {
    return null;
  }
}

export function getCurrentUser(): User | null {
  try {
    const raw = store().getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as User) : null;
  } catch {
    return null;
  }
}

export function isAdmin(): boolean {
  return getCurrentUser()?.role === 'Admin';
}

/** Store a fresh login/register/refresh response (tokens + who is logged in). */
export function saveSession(auth: AuthResponse): void {
  try {
    store().setItem(ACCESS_KEY, auth.accessToken);
    store().setItem(REFRESH_KEY, auth.refreshToken);
    store().setItem(USER_KEY, JSON.stringify(auth.user));
  } catch {
    // Private mode / storage full: the caller still gets the tokens in
    // memory for this page load, but the session will not survive reload.
  }
}

/** Forget everything about the current session (logout / expired refresh). */
export function clearSession(): void {
  try {
    store().removeItem(ACCESS_KEY);
    store().removeItem(REFRESH_KEY);
    store().removeItem(USER_KEY);
  } catch {
    // Nothing to do — the keys are already gone or unreachable.
  }
}

async function authRequest<T>(path: string, body: unknown): Promise<T> {
  if (!isApiConfigured()) {
    throw new ApiError(0, 'API is not configured (VITE_API_URL).');
  }
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), AUTH_TIMEOUT_MS);
  try {
    const res = await fetch(`${getApiBaseUrl()}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      signal: controller.signal,
    });
    const text = await res.text();
    if (!res.ok) {
      // The API returns a plain-text reason for 400/401/409; use it.
      throw new ApiError(res.status, text || `API ${res.status} for ${path}`);
    }
    return JSON.parse(text) as T;
  } finally {
    clearTimeout(timeout);
  }
}

/**
 * Register a new researcher. Registration is open; the FIRST registered
 * user becomes Admin automatically (bootstrap rule — see backend README).
 */
export async function register(
  email: string,
  password: string,
  displayName: string,
): Promise<AuthResponse> {
  const auth = await authRequest<AuthResponse>('/api/auth/register', {
    email,
    password,
    displayName,
  });
  saveSession(auth);
  return auth;
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const auth = await authRequest<AuthResponse>('/api/auth/login', { email, password });
  saveSession(auth);
  return auth;
}

/** Who is logged in right now (re-read from the API). */
export async function fetchMe(): Promise<User> {
  return authFetch<User>('/api/auth/me');
}

/** Revoke the refresh token on the server and forget the session locally. */
export async function logout(): Promise<void> {
  const refreshToken = store().getItem(REFRESH_KEY);
  if (refreshToken && isApiConfigured()) {
    try {
      await authRequest('/api/auth/logout', { refreshToken });
    } catch {
      // Server-side revoke is best-effort: the local session is cleared
      // either way, and the token dies on its own after 7 days.
    }
  }
  clearSession();
}

/** Change the password; revokes every refresh token (logout everywhere). */
export async function changePassword(
  currentPassword: string,
  newPassword: string,
): Promise<void> {
  await authFetch('/api/auth/change-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ currentPassword, newPassword }),
  });
}

/**
 * One refresh-token rotation. Returns the new session, or null when the
 * refresh token is missing/invalid — the caller then treats the user as
 * logged out.
 */
async function tryRefresh(): Promise<AuthResponse | null> {
  const refreshToken = store().getItem(REFRESH_KEY);
  if (!refreshToken) return null;
  try {
    const auth = await authRequest<AuthResponse>('/api/auth/refresh', { refreshToken });
    saveSession(auth);
    return auth;
  } catch {
    return null;
  }
}

/**
 * fetch() against the API with the Bearer token attached. On a 401 it
 * refreshes once and retries; on a second 401 it clears the session and
 * throws an ApiError(401) so the page can send the user to /login.
 * A 204 response resolves to undefined (cast by the caller).
 */
export async function authFetch<T>(
  path: string,
  init: RequestInit = {},
  timeoutMs: number = AUTH_TIMEOUT_MS,
): Promise<T> {
  if (!isApiConfigured()) {
    throw new ApiError(0, 'API is not configured (VITE_API_URL).');
  }
  const run = async (token: string | null): Promise<Response> => {
    const headers = new Headers(init.headers);
    if (token) headers.set('Authorization', `Bearer ${token}`);
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMs);
    try {
      return await fetch(`${getApiBaseUrl()}${path}`, {
        ...init,
        headers,
        signal: controller.signal,
      });
    } finally {
      clearTimeout(timeout);
    }
  };

  const parse = async (res: Response): Promise<T> => {
    if (res.status === 204) return undefined as T;
    const text = await res.text();
    if (!res.ok) {
      throw new ApiError(res.status, text || `API ${res.status} for ${path}`);
    }
    return text ? (JSON.parse(text) as T) : (undefined as T);
  };

  let res = await run(getAccessToken());
  if (res.status === 401) {
    const refreshed = await tryRefresh();
    if (!refreshed) {
      clearSession();
      throw new ApiError(401, 'Your session has expired — please log in again.');
    }
    res = await run(refreshed.accessToken);
    if (res.status === 401) {
      clearSession();
      throw new ApiError(401, 'Your session has expired — please log in again.');
    }
  }
  return parse(res);
}

/**
 * Like authFetch, but returns the raw Response instead of parsed JSON —
 * for endpoints that return a file (library/collection/snapshot exports).
 */
export async function authFetchRaw(
  path: string,
  timeoutMs: number = AUTH_TIMEOUT_MS,
): Promise<Response> {
  if (!isApiConfigured()) {
    throw new ApiError(0, 'API is not configured (VITE_API_URL).');
  }
  const run = async (token: string | null): Promise<Response> => {
    const headers = new Headers();
    if (token) headers.set('Authorization', `Bearer ${token}`);
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMs);
    try {
      return await fetch(`${getApiBaseUrl()}${path}`, {
        headers,
        signal: controller.signal,
      });
    } finally {
      clearTimeout(timeout);
    }
  };

  let res = await run(getAccessToken());
  if (res.status === 401) {
    const refreshed = await tryRefresh();
    if (!refreshed) {
      clearSession();
      throw new ApiError(401, 'Your session has expired — please log in again.');
    }
    res = await run(refreshed.accessToken);
    if (res.status === 401) {
      clearSession();
      throw new ApiError(401, 'Your session has expired — please log in again.');
    }
  }
  if (!res.ok) {
    const text = await res.text();
    throw new ApiError(res.status, text || `API ${res.status} for ${path}`);
  }
  return res;
}

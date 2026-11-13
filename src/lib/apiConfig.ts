// API base URL, read from VITE_API_URL (see .env.example). Kept in its own
// module so the auth client (auth.ts) and the API client (api.ts) can both
// use it without a circular import.

/** Base URL of the API, e.g. http://localhost:5083. Empty when not configured. */
export function getApiBaseUrl(): string {
  return ((import.meta.env.VITE_API_URL as string | undefined) ?? '').replace(/\/+$/, '');
}

export function isApiConfigured(): boolean {
  return getApiBaseUrl().length > 0;
}

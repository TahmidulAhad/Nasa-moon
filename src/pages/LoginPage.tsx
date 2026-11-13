import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../lib/authContext';
import { isApiConfigured } from '../lib/api';
import { ApiError } from '../lib/auth';
import EmptyState from '../components/EmptyState';

export default function LoginPage() {
  const { login, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  // Where to go after login: the page that sent us here, or My Library.
  const from = (location.state as { from?: string } | null)?.from ?? '/my-library';

  if (user) {
    navigate(from, { replace: true });
    return null;
  }

  if (!isApiConfigured()) {
    return (
      <section>
        <h1 className="mb-1 text-2xl font-bold">Log in</h1>
        <EmptyState
          title="Researcher accounts need the live API"
          body="Set VITE_API_URL to the API address (copy .env.example to .env) and reload."
        />
      </section>
    );
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError('Wrong email or password — try again.');
      } else if (err instanceof ApiError && err.status === 429) {
        setError('Too many attempts — wait a minute and try again.');
      } else {
        setError('Could not log in — is the API running?');
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mx-auto max-w-md">
      <h1 className="mb-1 text-2xl font-bold">Log in</h1>
      <p className="mb-5 text-sm text-slate-400">
        Log in to your researcher account to save missions, build collections
        and keep snapshots of the dataset.
      </p>
      <form
        onSubmit={handleSubmit}
        className="rounded-xl border border-space-800 bg-space-900 p-5"
      >
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">Email</span>
          <input
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">Password</span>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Your password"
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
        {error && <p className="mb-4 text-sm text-red-400">{error}</p>}
        <button
          type="submit"
          disabled={busy}
          className="inline-flex min-h-[44px] w-full items-center justify-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
        >
          {busy ? 'Logging in…' : 'Log in'}
        </button>
        <p className="mt-4 text-center text-sm text-slate-400">
          No account yet?{' '}
          <Link to="/register" className="text-accent underline">
            Register as a researcher
          </Link>
        </p>
      </form>
    </section>
  );
}

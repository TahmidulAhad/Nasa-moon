import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../lib/authContext';
import { isApiConfigured } from '../lib/api';
import { ApiError } from '../lib/auth';
import EmptyState from '../components/EmptyState';

export default function RegisterPage() {
  const { register, user } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  if (user) {
    navigate('/my-library', { replace: true });
    return null;
  }

  if (!isApiConfigured()) {
    return (
      <section>
        <h1 className="mb-1 text-2xl font-bold">Register</h1>
        <EmptyState
          title="Researcher accounts need the live API"
          body="Set VITE_API_URL to the API address (copy .env.example to .env) and reload."
        />
      </section>
    );
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (password !== confirm) {
      setError('The two passwords do not match.');
      return;
    }
    if (password.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }
    setError('');
    setBusy(true);
    try {
      await register(email.trim(), password, displayName.trim());
      navigate('/my-library', { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('An account with this email already exists — try logging in.');
      } else if (err instanceof ApiError && err.status === 400) {
        setError(err.message || 'Check the details and try again.');
      } else if (err instanceof ApiError && err.status === 429) {
        setError('Too many attempts — wait a minute and try again.');
      } else {
        setError('Could not register — is the API running?');
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="mx-auto max-w-md">
      <h1 className="mb-1 text-2xl font-bold">Register as a researcher</h1>
      <p className="mb-5 text-sm text-slate-400">
        Your own account for the mission data: save missions with notes, build
        collections, and keep snapshots you can compare later. Your library is
        private — only you (and admins) can see it.
      </p>
      <form
        onSubmit={handleSubmit}
        className="rounded-xl border border-space-800 bg-space-900 p-5"
      >
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">Display name</span>
          <input
            type="text"
            required
            autoComplete="name"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="e.g. Tahmidul Ahad"
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
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
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="At least 8 characters"
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">Confirm password</span>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            placeholder="Repeat your password"
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
        {error && <p className="mb-4 text-sm text-red-400">{error}</p>}
        <button
          type="submit"
          disabled={busy}
          className="inline-flex min-h-[44px] w-full items-center justify-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
        >
          {busy ? 'Creating your account…' : 'Create account'}
        </button>
        <p className="mt-4 text-center text-sm text-slate-400">
          Already have an account?{' '}
          <Link to="/login" className="text-accent underline">
            Log in
          </Link>
        </p>
      </form>
      <p className="mt-4 text-xs text-slate-500">
        The very first account registered on this API becomes the Admin
        (bootstrap rule). No email verification in this phase — your account
        is active immediately.
      </p>
    </section>
  );
}

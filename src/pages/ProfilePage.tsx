import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../lib/authContext';
import { ApiError } from '../lib/auth';
import { listCollections, listMySnapshots, listSaved } from '../lib/library';

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString();
}

export default function ProfilePage() {
  const { user, logout, changePassword } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [counts, setCounts] = useState<{ saved: number; collections: number; snapshots: number } | null>(null);
  const [loadingCounts, setLoadingCounts] = useState(true);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [pwMessage, setPwMessage] = useState('');
  const [pwError, setPwError] = useState('');
  const [pwBusy, setPwBusy] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);

  useEffect(() => {
    if (!user) return;
    let cancelled = false;
    (async () => {
      try {
        const [saved, cols, snaps] = await Promise.all([
          listSaved(),
          listCollections(),
          listMySnapshots(),
        ]);
        if (!cancelled) {
          setCounts({
            saved: saved.length,
            collections: cols.length,
            snapshots: snaps.filter((s) => !s.isLegacy).length,
          });
        }
      } catch {
        if (!cancelled) setCounts({ saved: 0, collections: 0, snapshots: 0 });
      } finally {
        if (!cancelled) setLoadingCounts(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [user]);

  if (!user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  async function handleChangePassword(e: FormEvent) {
    e.preventDefault();
    if (newPassword !== confirm) {
      setPwError('The two new passwords do not match.');
      setPwMessage('');
      return;
    }
    if (newPassword.length < 8) {
      setPwError('New password must be at least 8 characters.');
      setPwMessage('');
      return;
    }
    setPwError('');
    setPwMessage('');
    setPwBusy(true);
    try {
      await changePassword(currentPassword, newPassword);
      setPwMessage('Password changed. You have been logged out on all devices — log in again with the new password.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirm('');
    } catch (err) {
      setPwError(
        err instanceof ApiError && err.status === 400
          ? 'Current password is wrong — try again.'
          : 'Could not change the password — try again.',
      );
    } finally {
      setPwBusy(false);
    }
  }

  async function handleLogout() {
    setLoggingOut(true);
    await logout();
    navigate('/', { replace: true });
  }

  return (
    <section className="mx-auto max-w-2xl pb-10">
      <h1 className="mb-1 text-2xl font-bold">My profile</h1>
      <p className="mb-5 text-sm text-slate-400">Your researcher account.</p>

      <div className="mb-6 rounded-xl border border-space-800 bg-space-900 p-5">
        <dl className="space-y-2 text-sm">
          <div className="flex gap-3">
            <dt className="w-32 shrink-0 font-semibold text-slate-200">Name</dt>
            <dd className="text-slate-100">{user.displayName}</dd>
          </div>
          <div className="flex gap-3">
            <dt className="w-32 shrink-0 font-semibold text-slate-200">Email</dt>
            <dd className="text-slate-100">{user.email}</dd>
          </div>
          <div className="flex gap-3">
            <dt className="w-32 shrink-0 font-semibold text-slate-200">Role</dt>
            <dd>
              <span
                className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                  user.role === 'Admin'
                    ? 'bg-amber-500/20 text-amber-300'
                    : 'bg-sky-500/20 text-sky-300'
                }`}
              >
                {user.role}
              </span>
              {user.role === 'Admin' && (
                <span className="ml-2 text-slate-400">
                  — you can manage missions and see all accounts (
                  <Link to="/admin" className="text-accent underline">
                    Admin
                  </Link>
                  )
                </span>
              )}
            </dd>
          </div>
          <div className="flex gap-3">
            <dt className="w-32 shrink-0 font-semibold text-slate-200">Member since</dt>
            <dd className="text-slate-100">{formatDate(user.createdAt)}</dd>
          </div>
        </dl>
      </div>

      <div className="mb-6 grid grid-cols-3 gap-3">
        {[
          { label: 'Saved missions', value: counts?.saved, to: '/my-library' },
          { label: 'Collections', value: counts?.collections, to: '/collections' },
          { label: 'My snapshots', value: counts?.snapshots, to: '/research' },
        ].map((stat) => (
          <Link
            key={stat.label}
            to={stat.to}
            className="rounded-xl border border-space-800 bg-space-900 p-4 text-center hover:border-space-700"
          >
            <p className="text-2xl font-bold text-slate-100">
              {loadingCounts ? '…' : (stat.value ?? 0)}
            </p>
            <p className="text-xs text-slate-400">{stat.label}</p>
          </Link>
        ))}
      </div>

      <h2 className="mb-3 text-base font-semibold text-slate-100">Change password</h2>
      <form
        onSubmit={handleChangePassword}
        className="mb-6 rounded-xl border border-space-800 bg-space-900 p-5"
      >
        <p className="mb-4 text-sm text-slate-400">
          Changing your password logs you out everywhere (all devices).
        </p>
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">Current password</span>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
          />
        </label>
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">New password</span>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            placeholder="At least 8 characters"
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
        <label className="mb-4 block">
          <span className="mb-1 block text-sm font-semibold text-slate-200">Confirm new password</span>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
          />
        </label>
        {pwError && <p className="mb-4 text-sm text-red-400">{pwError}</p>}
        {pwMessage && <p className="mb-4 text-sm text-emerald-400">{pwMessage}</p>}
        <button
          type="submit"
          disabled={pwBusy}
          className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
        >
          {pwBusy ? 'Changing…' : 'Change password'}
        </button>
      </form>

      <button
        type="button"
        onClick={() => void handleLogout()}
        disabled={loggingOut}
        className="inline-flex min-h-[44px] items-center rounded-md border border-red-900 px-4 text-sm text-red-400 disabled:opacity-50"
      >
        {loggingOut ? 'Logging out…' : 'Log out'}
      </button>
    </section>
  );
}

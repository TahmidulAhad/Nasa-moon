import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  compareSnapshots,
  createSnapshot,
  deleteSnapshot,
  getSnapshot,
  isApiConfigured,
  listSnapshots,
} from '../lib/api';
import type { SnapshotDiff, SnapshotSummary } from '../lib/api';
import type { Mission } from '../lib/types';
import { useAuth } from '../lib/authContext';
import EmptyState from '../components/EmptyState';
import Loading from '../components/Loading';
import StatusBadge from '../components/StatusBadge';

const FIELD_LABELS: Record<string, string> = {
  name: 'Name',
  status: 'Status',
  launchDate: 'Launch date',
  landingDate: 'Landing date',
  landingSite: 'Landing site',
  region: 'Region',
  provider: 'Provider',
  lander: 'Lander',
  payloads: 'Payloads',
  objective: 'Objective',
};

function fieldLabel(field: string): string {
  return FIELD_LABELS[field] ?? field;
}

function valueOrDash(value: string | null): string {
  return value === null || value === '' ? '—' : value;
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString();
}

export default function ResearchPage() {
  const { user } = useAuth();
  const [snapshots, setSnapshots] = useState<SnapshotSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState('');

  const [label, setLabel] = useState('');
  const [source, setSource] = useState<'live' | 'static'>('live');

  const [viewingId, setViewingId] = useState<string | null>(null);
  const [viewingMissions, setViewingMissions] = useState<Mission[] | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const [fromId, setFromId] = useState('');
  const [toId, setToId] = useState('live');
  const [comparing, setComparing] = useState(false);
  const [compareError, setCompareError] = useState('');
  const [diff, setDiff] = useState<SnapshotDiff | null>(null);

  const apiAvailable = isApiConfigured();

  const load = useCallback(async () => {
    if (!apiAvailable) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setLoadError(false);
    try {
      setSnapshots(await listSnapshots());
    } catch {
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [apiAvailable]);

  useEffect(() => {
    void load();
  }, [load]);

  const canCompare = useMemo(
    () => fromId !== '' && toId !== '' && fromId !== toId,
    [fromId, toId],
  );

  async function handleSave() {
    // Snapshots belong to the researcher who saves them — login first.
    if (!user) {
      setSaveError('Log in to save a snapshot — it will be stored in your account.');
      return;
    }
    const trimmed = label.trim();
    if (!trimmed) {
      setSaveError('Give the snapshot a label first.');
      return;
    }
    setSaving(true);
    setSaveError('');
    try {
      const created = await createSnapshot(trimmed, source);
      setSnapshots((prev) => [created, ...prev]);
      setLabel('');
    } catch {
      setSaveError('Could not save the snapshot — is the API running?');
    } finally {
      setSaving(false);
    }
  }

  async function handleView(id: string) {
    if (viewingId === id) {
      setViewingId(null);
      setViewingMissions(null);
      return;
    }
    setViewingId(id);
    setViewingMissions(null);
    try {
      const detail = await getSnapshot(id);
      setViewingMissions(detail?.missions ?? []);
    } catch {
      setViewingMissions([]);
    }
  }

  async function handleDelete(id: string) {
    setDeletingId(id);
    try {
      await deleteSnapshot(id);
      setSnapshots((prev) => prev.filter((s) => s.id !== id));
      if (viewingId === id) {
        setViewingId(null);
        setViewingMissions(null);
      }
      if (fromId === id) setFromId('');
    } catch {
      // Keep the row: the delete did not go through.
    } finally {
      setDeletingId(null);
    }
  }

  async function handleCompare() {
    if (!canCompare) return;
    setComparing(true);
    setCompareError('');
    setDiff(null);
    try {
      setDiff(await compareSnapshots(fromId, toId));
    } catch {
      setCompareError('Could not compare — is the API running?');
    } finally {
      setComparing(false);
    }
  }

  if (!apiAvailable) {
    return (
      <section>
        <h1 className="mb-1 text-2xl font-bold">Research snapshots</h1>
        <p className="mb-5 text-sm text-slate-400">
          Save the mission dataset as it is today, then compare it with a later
          version to see exactly what changed.
        </p>
        <EmptyState
          title="Research needs the live API"
          body="Set VITE_API_URL to the API address (copy .env.example to .env) and reload — snapshots are stored in the API's database."
        />
      </section>
    );
  }

  if (loading) return <Loading />;

  if (loadError) {
    return (
      <section>
        <h1 className="mb-1 text-2xl font-bold">Research snapshots</h1>
        <EmptyState
          title="Could not reach the API"
          body="Snapshots live in the API's database. Check that the API is running and reload."
          action={
            <button
              type="button"
              onClick={() => void load()}
              className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
            >
              Try again
            </button>
          }
        />
      </section>
    );
  }

  const diffEmpty =
    diff !== null &&
    diff.added.length === 0 &&
    diff.removed.length === 0 &&
    diff.changed.length === 0;

  return (
    <section className="pb-28">
      <h1 className="mb-1 text-2xl font-bold">Research snapshots</h1>
      <p className="mb-5 text-sm text-slate-400">
        Save the mission dataset as it is today, then compare two saved moments —
        or a saved moment against the live data — to see exactly what changed.
      </p>

      {/* Save a snapshot — login required: snapshots belong to the
      researcher who saves them. Logged-out visitors can still browse and
      compare the legacy (public) snapshots below. */}
      <div className="mb-6 rounded-xl border border-space-800 bg-space-900 p-4">
        <h2 className="mb-3 text-base font-semibold text-slate-100">Save a snapshot</h2>
        {!user ? (
          <p className="text-sm text-slate-300">
            <Link to="/login" state={{ from: '/research' }} className="text-accent underline">
              Log in
            </Link>{' '}
            to save a snapshot — it is stored in your account and only you can
            see it. Public (legacy) snapshots saved before accounts existed
            stay visible to everyone.
          </p>
        ) : (
          <div className="flex flex-col gap-3 sm:flex-row">
          <label className="flex-1">
            <span className="sr-only">Snapshot label</span>
            <input
              type="text"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              placeholder='e.g. "Before the March update"'
              className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
            />
          </label>
          <label>
            <span className="sr-only">Snapshot source</span>
            <select
              value={source}
              onChange={(e) => setSource(e.target.value as 'live' | 'static')}
              className="min-h-[44px] rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
            >
              <option value="live">Live data (API database)</option>
              <option value="static">Static data (bundled missions.json)</option>
            </select>
          </label>
          <button
            type="button"
            onClick={() => void handleSave()}
            disabled={saving}
            className="inline-flex min-h-[44px] items-center justify-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save snapshot'}
          </button>
          </div>
        )}
        {saveError && <p className="mt-2 text-sm text-red-400">{saveError}</p>}
      </div>

      {/* Saved snapshots */}
      <h2 className="mb-3 text-base font-semibold text-slate-100">Saved snapshots</h2>
      {snapshots.length === 0 ? (
        <EmptyState
          title="No snapshots yet"
          body="Save your first snapshot above — it freezes the dataset as it is right now."
        />
      ) : (
        <ul className="mb-8 space-y-3">
          {snapshots.map((s) => (
            <li key={s.id} className="rounded-xl border border-space-800 bg-space-900 p-4">
              <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                <span className="text-base font-semibold text-slate-100">{s.label}</span>
                <span
                  className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                    s.source === 'live'
                      ? 'bg-sky-500/20 text-sky-300'
                      : 'bg-slate-500/20 text-slate-300'
                  }`}
                >
                  {s.source === 'live' ? 'Live data' : 'Static data'}
                </span>
                <span className="text-sm text-slate-400">{formatDate(s.createdAt)}</span>
                <span className="text-sm text-slate-400">
                  {s.missionCount} mission{s.missionCount === 1 ? '' : 's'}
                </span>
                <span
                  className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                    s.isLegacy
                      ? 'bg-slate-500/20 text-slate-300'
                      : s.userId === user?.id
                        ? 'bg-emerald-500/20 text-emerald-300'
                        : 'bg-slate-500/20 text-slate-300'
                  }`}
                >
                  {s.isLegacy
                    ? 'Public (legacy)'
                    : s.userId === user?.id
                      ? 'Yours'
                      : 'Another researcher'}
                </span>
                <span className="ml-auto flex gap-2">
                  <button
                    type="button"
                    onClick={() => void handleView(s.id)}
                    className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-3 text-sm text-slate-200"
                  >
                    {viewingId === s.id ? 'Hide' : 'View'}
                  </button>
                  {(s.userId === user?.id || user?.role === 'Admin') && (
                    <button
                      type="button"
                      onClick={() => void handleDelete(s.id)}
                      disabled={deletingId === s.id}
                      className="inline-flex min-h-[44px] items-center rounded-md border border-red-900 px-3 text-sm text-red-400 disabled:opacity-50"
                    >
                      Delete
                    </button>
                  )}
                </span>
              </div>
              {viewingId === s.id && (
                <div className="mt-3 border-t border-space-800 pt-3">
                  {viewingMissions === null ? (
                    <p className="text-sm text-slate-400">Loading missions…</p>
                  ) : viewingMissions.length === 0 ? (
                    <p className="text-sm text-slate-400">No missions in this snapshot.</p>
                  ) : (
                    <ul className="grid gap-1 sm:grid-cols-2">
                      {viewingMissions.map((m) => (
                        <li key={m.id} className="flex items-center gap-2 text-sm">
                          <Link
                            to={`/mission/${m.id}`}
                            className="text-slate-200 underline decoration-slate-600 underline-offset-2"
                          >
                            {m.name}
                          </Link>
                          <StatusBadge status={m.status} />
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {/* Compare */}
      <h2 className="mb-3 text-base font-semibold text-slate-100">Compare</h2>
      <div className="mb-6 rounded-xl border border-space-800 bg-space-900 p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <label className="flex-1">
            <span className="mb-1 block text-sm text-slate-400">From</span>
            <select
              value={fromId}
              onChange={(e) => setFromId(e.target.value)}
              className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
            >
              <option value="">Pick a snapshot…</option>
              {snapshots.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.label} ({formatDate(s.createdAt)})
                </option>
              ))}
            </select>
          </label>
          <label className="flex-1">
            <span className="mb-1 block text-sm text-slate-400">To</span>
            <select
              value={toId}
              onChange={(e) => setToId(e.target.value)}
              className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
            >
              <option value="live">Live data (current database)</option>
              {snapshots.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.label} ({formatDate(s.createdAt)})
                </option>
              ))}
            </select>
          </label>
          <button
            type="button"
            onClick={() => void handleCompare()}
            disabled={!canCompare || comparing}
            className="inline-flex min-h-[44px] items-center justify-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
          >
            {comparing ? 'Comparing…' : 'Compare'}
          </button>
        </div>
        {compareError && <p className="mt-2 text-sm text-red-400">{compareError}</p>}
        {!canCompare && fromId !== '' && toId !== '' && (
          <p className="mt-2 text-sm text-slate-400">Pick two different snapshots to compare.</p>
        )}
      </div>

      {/* Diff result */}
      {diff && (
        <div>
          <h2 className="mb-3 text-base font-semibold text-slate-100">
            {diff.fromLabel} → {diff.toLabel}
          </h2>
          {diffEmpty ? (
            <EmptyState
              title="No differences"
              body="These two datasets are identical — nothing was added, removed or changed."
            />
          ) : (
            <div className="space-y-6">
              {diff.added.length > 0 && (
                <div>
                  <h3 className="mb-2 text-sm font-semibold text-emerald-400">
                    Added ({diff.added.length})
                  </h3>
                  <ul className="space-y-2">
                    {diff.added.map((m) => (
                      <li
                        key={m.id}
                        className="rounded-xl border border-emerald-900 bg-emerald-950/40 p-3 text-sm"
                      >
                        <Link to={`/mission/${m.id}`} className="font-semibold text-slate-100 underline decoration-slate-600 underline-offset-2">
                          {m.name}
                        </Link>
                        <span className="ml-2 text-slate-400">
                          {m.provider ?? ''} · {m.status}
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {diff.removed.length > 0 && (
                <div>
                  <h3 className="mb-2 text-sm font-semibold text-red-400">
                    Removed ({diff.removed.length})
                  </h3>
                  <ul className="space-y-2">
                    {diff.removed.map((m) => (
                      <li
                        key={m.id}
                        className="rounded-xl border border-red-900 bg-red-950/40 p-3 text-sm"
                      >
                        <span className="font-semibold text-slate-100">{m.name}</span>
                        <span className="ml-2 text-slate-400">
                          {m.provider ?? ''} · {m.status}
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {diff.changed.length > 0 && (
                <div>
                  <h3 className="mb-2 text-sm font-semibold text-amber-400">
                    Changed ({diff.changed.length} mission{diff.changed.length === 1 ? '' : 's'})
                  </h3>
                  <div className="overflow-x-auto rounded-xl border border-space-800">
                    <table className="w-full text-left text-sm">
                      <thead>
                        <tr className="border-b border-space-800 bg-space-900 text-slate-400">
                          <th className="px-3 py-2 font-medium">Mission</th>
                          <th className="px-3 py-2 font-medium">Field</th>
                          <th className="px-3 py-2 font-medium">Before</th>
                          <th className="px-3 py-2 font-medium">After</th>
                        </tr>
                      </thead>
                      <tbody>
                        {diff.changed.flatMap((c) =>
                          c.changes.map((change, i) => (
                            <tr key={`${c.missionId}-${change.field}`} className="border-b border-space-800 last:border-0">
                              {i === 0 && (
                                <td rowSpan={c.changes.length} className="px-3 py-2 align-top font-semibold text-slate-100">
                                  <Link
                                    to={`/mission/${c.missionId}`}
                                    className="underline decoration-slate-600 underline-offset-2"
                                  >
                                    {c.missionName}
                                  </Link>
                                </td>
                              )}
                              <td className="px-3 py-2 text-slate-300">{fieldLabel(change.field)}</td>
                              <td className="px-3 py-2 text-red-300">{valueOrDash(change.oldValue)}</td>
                              <td className="px-3 py-2 text-emerald-300">{valueOrDash(change.newValue)}</td>
                            </tr>
                          )),
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </section>
  );
}

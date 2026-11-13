import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../lib/authContext';
import { ApiError } from '../lib/auth';
import {
  createCollection,
  exportLibrary,
  listCollections,
  listSaved,
  unsaveMission,
  updateSavedItem,
} from '../lib/library';
import type { Collection, ExportFormat, SavedItem } from '../lib/library';
import EmptyState from '../components/EmptyState';
import Loading from '../components/Loading';
import StatusBadge from '../components/StatusBadge';

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString();
}

function SavedItemRow({
  item,
  onChanged,
}: {
  item: SavedItem;
  onChanged: () => void;
}) {
  const [editing, setEditing] = useState(false);
  const [note, setNote] = useState(item.note ?? '');
  const [tags, setTags] = useState(item.tags.join(', '));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function handleSave() {
    setBusy(true);
    setError('');
    try {
      await updateSavedItem(
        item.id,
        note.trim() === '' ? null : note.trim(),
        tags
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean),
      );
      setEditing(false);
      onChanged();
    } catch {
      setError('Could not save — try again.');
    } finally {
      setBusy(false);
    }
  }

  async function handleUnsave() {
    setBusy(true);
    try {
      await unsaveMission(item.id);
      onChanged();
    } catch {
      setError('Could not remove — try again.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className="rounded-xl border border-space-800 bg-space-900 p-4">
      <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
        <Link
          to={`/mission/${item.missionId}`}
          className="text-base font-semibold text-slate-100 underline decoration-slate-600 underline-offset-2 hover:text-white"
        >
          {item.missionName}
        </Link>
        <StatusBadge status={item.missionStatus as never} />
        {item.missionProvider && (
          <span className="text-sm text-slate-400">{item.missionProvider}</span>
        )}
        <span className="ml-auto flex gap-2">
          <button
            type="button"
            onClick={() => setEditing((e) => !e)}
            className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-3 text-sm text-slate-200"
          >
            {editing ? 'Cancel' : 'Edit note'}
          </button>
          <button
            type="button"
            onClick={() => void handleUnsave()}
            disabled={busy}
            className="inline-flex min-h-[44px] items-center rounded-md border border-red-900 px-3 text-sm text-red-400 disabled:opacity-50"
          >
            Remove
          </button>
        </span>
      </div>
      {item.note && !editing && (
        <p className="mt-2 text-sm text-slate-300">{item.note}</p>
      )}
      {item.tags.length > 0 && !editing && (
        <p className="mt-2 flex flex-wrap gap-1">
          {item.tags.map((t) => (
            <span
              key={t}
              className="rounded-full bg-sky-500/20 px-2 py-0.5 text-xs font-semibold text-sky-300"
            >
              {t}
            </span>
          ))}
        </p>
      )}
      <p className="mt-2 text-xs text-slate-500">Saved {formatDate(item.createdAt)}</p>
      {editing && (
        <div className="mt-3 border-t border-space-800 pt-3">
          <label className="mb-2 block">
            <span className="mb-1 block text-sm font-semibold text-slate-200">Note</span>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={2}
              placeholder="Why did you save this mission?"
              className="w-full rounded-md border border-space-700 bg-space-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            />
          </label>
          <label className="mb-3 block">
            <span className="mb-1 block text-sm font-semibold text-slate-200">
              Tags (comma-separated)
            </span>
            <input
              type="text"
              value={tags}
              onChange={(e) => setTags(e.target.value)}
              placeholder="e.g. lunar, lander"
              className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
            />
          </label>
          {error && <p className="mb-2 text-sm text-red-400">{error}</p>}
          <button
            type="button"
            onClick={() => void handleSave()}
            disabled={busy}
            className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
          >
            {busy ? 'Saving…' : 'Save note'}
          </button>
        </div>
      )}
    </li>
  );
}

export default function MyLibraryPage() {
  const { user } = useAuth();
  const location = useLocation();
  const [items, setItems] = useState<SavedItem[]>([]);
  const [collections, setCollections] = useState<Collection[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [search, setSearch] = useState('');
  const [newCollectionName, setNewCollectionName] = useState('');
  const [collectionError, setCollectionError] = useState('');
  const [exporting, setExporting] = useState<ExportFormat | null>(null);
  const [exportError, setExportError] = useState('');

  const load = useCallback(async (q?: string) => {
    setLoading(true);
    setLoadError('');
    try {
      const [saved, cols] = await Promise.all([listSaved(q), listCollections()]);
      setItems(saved);
      setCollections(cols);
    } catch (err) {
      setLoadError(
        err instanceof ApiError && err.status === 401
          ? 'Your session expired — please log in again.'
          : 'Could not load your library — is the API running?',
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (user) void load();
  }, [user, load]);

  if (!user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  async function handleSearch(e: FormEvent) {
    e.preventDefault();
    await load(search);
  }

  async function handleCreateCollection(e: FormEvent) {
    e.preventDefault();
    const name = newCollectionName.trim();
    if (!name) {
      setCollectionError('Give the collection a name first.');
      return;
    }
    setCollectionError('');
    try {
      const created = await createCollection(name);
      setCollections((prev) => [created, ...prev]);
      setNewCollectionName('');
    } catch {
      setCollectionError('Could not create the collection — try again.');
    }
  }

  async function handleExport(format: ExportFormat) {
    setExporting(format);
    setExportError('');
    try {
      await exportLibrary(format);
    } catch {
      setExportError(`Could not export as ${format.toUpperCase()} — try again.`);
    } finally {
      setExporting(null);
    }
  }

  return (
    <section className="pb-10">
      <h1 className="mb-1 text-2xl font-bold">My Library</h1>
      <p className="mb-5 text-sm text-slate-400">
        Missions you saved, with your own notes and tags. Only you can see
        this — export it to your own drive any time.
      </p>

      {loading ? (
        <Loading />
      ) : loadError ? (
        <EmptyState title="Could not load your library" body={loadError} />
      ) : (
        <>
          {/* Search + export */}
          <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center">
            <form onSubmit={handleSearch} className="flex flex-1 gap-2">
              <label className="flex-1">
                <span className="sr-only">Search my library</span>
                <input
                  type="text"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Search by mission name, note or tag…"
                  className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
                />
              </label>
              <button
                type="submit"
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200"
              >
                Search
              </button>
            </form>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => void handleExport('json')}
                disabled={exporting !== null}
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200 disabled:opacity-50"
              >
                {exporting === 'json' ? 'Exporting…' : 'Export JSON'}
              </button>
              <button
                type="button"
                onClick={() => void handleExport('csv')}
                disabled={exporting !== null}
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200 disabled:opacity-50"
              >
                {exporting === 'csv' ? 'Exporting…' : 'Export CSV'}
              </button>
            </div>
          </div>
          {exportError && <p className="mb-4 text-sm text-red-400">{exportError}</p>}

          {/* Saved missions */}
          <h2 className="mb-3 text-base font-semibold text-slate-100">
            Saved missions ({items.length})
          </h2>
          {items.length === 0 ? (
            <EmptyState
              title="Nothing saved yet"
              body="Open a mission and press “Save to my library” — it will show up here with your notes and tags."
              action={
                <Link
                  to="/missions"
                  className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
                >
                  Browse missions
                </Link>
              }
            />
          ) : (
            <ul className="mb-8 space-y-3">
              {items.map((item) => (
                <SavedItemRow
                  key={item.id}
                  item={item}
                  onChanged={() => void load(search)}
                />
              ))}
            </ul>
          )}

          {/* Collections */}
          <h2 className="mb-3 text-base font-semibold text-slate-100">
            My collections ({collections.length})
          </h2>
          <form
            onSubmit={handleCreateCollection}
            className="mb-4 flex flex-col gap-2 sm:flex-row"
          >
            <label className="flex-1">
              <span className="sr-only">New collection name</span>
              <input
                type="text"
                value={newCollectionName}
                onChange={(e) => setNewCollectionName(e.target.value)}
                placeholder="New collection name, e.g. “Lunar landers”"
                className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
              />
            </label>
            <button
              type="submit"
              className="inline-flex min-h-[44px] items-center justify-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
            >
              Create collection
            </button>
          </form>
          {collectionError && <p className="mb-4 text-sm text-red-400">{collectionError}</p>}
          {collections.length === 0 ? (
            <p className="text-sm text-slate-400">
              No collections yet — group saved missions into folders for a
              topic you are researching.
            </p>
          ) : (
            <ul className="grid gap-3 sm:grid-cols-2">
              {collections.map((c) => (
                <li
                  key={c.id}
                  className="rounded-xl border border-space-800 bg-space-900 p-4"
                >
                  <Link
                    to={`/collections/${c.id}`}
                    className="text-base font-semibold text-slate-100 underline decoration-slate-600 underline-offset-2 hover:text-white"
                  >
                    {c.name}
                  </Link>
                  {c.description && (
                    <p className="mt-1 text-sm text-slate-400">{c.description}</p>
                  )}
                  <p className="mt-2 text-sm text-slate-400">
                    {c.itemCount} mission{c.itemCount === 1 ? '' : 's'} ·{' '}
                    {formatDate(c.createdAt)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}

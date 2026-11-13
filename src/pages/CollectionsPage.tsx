import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../lib/authContext';
import { ApiError } from '../lib/auth';
import {
  addToCollection,
  deleteCollection,
  exportCollection,
  getCollection,
  listCollections,
  listSaved,
  removeFromCollection,
  updateCollection,
} from '../lib/library';
import type { Collection, CollectionDetail, ExportFormat, SavedItem } from '../lib/library';
import EmptyState from '../components/EmptyState';
import Loading from '../components/Loading';

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString();
}

export default function CollectionsPage() {
  const { user } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const [collections, setCollections] = useState<Collection[]>([]);
  const [detail, setDetail] = useState<CollectionDetail | null>(null);
  const [saved, setSaved] = useState<SavedItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [addMissionId, setAddMissionId] = useState('');
  const [addError, setAddError] = useState('');
  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editing, setEditing] = useState(false);
  const [exporting, setExporting] = useState<ExportFormat | null>(null);

  const loadList = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setCollections(await listCollections());
    } catch {
      setError('Could not load your collections — is the API running?');
    } finally {
      setLoading(false);
    }
  }, []);

  const loadDetail = useCallback(async (collectionId: string) => {
    setLoading(true);
    setError('');
    try {
      const [d, s] = await Promise.all([getCollection(collectionId), listSaved()]);
      setDetail(d);
      setSaved(s);
      setEditName(d.name);
      setEditDescription(d.description ?? '');
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 404
          ? 'This collection does not exist (or is not yours).'
          : 'Could not load the collection — is the API running?',
      );
      setDetail(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!user) return;
    if (id) void loadDetail(id);
    else void loadList();
  }, [user, id, loadList, loadDetail]);

  if (!user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  async function handleAdd(e: FormEvent) {
    e.preventDefault();
    if (!detail || !addMissionId) return;
    setAddError('');
    try {
      await addToCollection(detail.id, addMissionId);
      setAddMissionId('');
      await loadDetail(detail.id);
    } catch {
      setAddError('Could not add that mission — is it already saved?');
    }
  }

  async function handleRemove(missionId: string) {
    if (!detail) return;
    try {
      await removeFromCollection(detail.id, missionId);
      await loadDetail(detail.id);
    } catch {
      setAddError('Could not remove that mission — try again.');
    }
  }

  async function handleRename(e: FormEvent) {
    e.preventDefault();
    if (!detail) return;
    try {
      const updated = await updateCollection(
        detail.id,
        editName.trim(),
        editDescription.trim() === '' ? null : editDescription.trim(),
      );
      setDetail({ ...detail, name: updated.name, description: updated.description });
      setEditing(false);
    } catch {
      setAddError('Could not rename the collection — try again.');
    }
  }

  async function handleDelete() {
    if (!detail) return;
    if (!window.confirm(`Delete the collection “${detail.name}”? The missions stay in your library.`)) {
      return;
    }
    try {
      await deleteCollection(detail.id);
      navigate('/collections', { replace: true });
    } catch {
      setAddError('Could not delete the collection — try again.');
    }
  }

  async function handleExport(format: ExportFormat) {
    if (!detail) return;
    setExporting(format);
    try {
      await exportCollection(detail.id, format);
    } catch {
      setAddError(`Could not export as ${format.toUpperCase()} — try again.`);
    } finally {
      setExporting(null);
    }
  }

  // --- Detail view: one collection -----------------------------------------
  if (id) {
    if (loading) return <Loading />;
    if (error || !detail) {
      return (
        <section>
          <h1 className="mb-1 text-2xl font-bold">Collection</h1>
          <EmptyState
            title="Collection not found"
            body={error || 'This collection does not exist.'}
            action={
              <Link
                to="/collections"
                className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
              >
                Back to collections
              </Link>
            }
          />
        </section>
      );
    }

    const inCollection = new Set(detail.items.map((i) => i.missionId));
    const addable = saved.filter((s) => !inCollection.has(s.missionId));

    return (
      <section className="pb-10">
        <Link to="/collections" className="text-sm text-accent underline">
          ← All collections
        </Link>
        {editing ? (
          <form onSubmit={handleRename} className="mt-3 rounded-xl border border-space-800 bg-space-900 p-4">
            <label className="mb-2 block">
              <span className="mb-1 block text-sm font-semibold text-slate-200">Name</span>
              <input
                type="text"
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
              />
            </label>
            <label className="mb-3 block">
              <span className="mb-1 block text-sm font-semibold text-slate-200">Description</span>
              <input
                type="text"
                value={editDescription}
                onChange={(e) => setEditDescription(e.target.value)}
                className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
              />
            </label>
            <div className="flex gap-2">
              <button
                type="submit"
                className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
              >
                Save
              </button>
              <button
                type="button"
                onClick={() => setEditing(false)}
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200"
              >
                Cancel
              </button>
            </div>
          </form>
        ) : (
          <>
            <h1 className="mt-3 mb-1 text-2xl font-bold">{detail.name}</h1>
            {detail.description && (
              <p className="mb-2 text-sm text-slate-400">{detail.description}</p>
            )}
            <p className="mb-5 text-sm text-slate-400">
              {detail.itemCount} mission{detail.itemCount === 1 ? '' : 's'} ·{' '}
              Created {formatDate(detail.createdAt)}
            </p>
            <div className="mb-6 flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setEditing(true)}
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200"
              >
                Rename
              </button>
              <button
                type="button"
                onClick={() => void handleExport('json')}
                disabled={exporting !== null}
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200 disabled:opacity-50"
              >
                Export JSON
              </button>
              <button
                type="button"
                onClick={() => void handleExport('csv')}
                disabled={exporting !== null}
                className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200 disabled:opacity-50"
              >
                Export CSV
              </button>
              <button
                type="button"
                onClick={() => void handleDelete()}
                className="inline-flex min-h-[44px] items-center rounded-md border border-red-900 px-4 text-sm text-red-400"
              >
                Delete collection
              </button>
            </div>
          </>
        )}
        {addError && <p className="mb-4 text-sm text-red-400">{addError}</p>}

        <h2 className="mb-3 text-base font-semibold text-slate-100">Missions in this collection</h2>
        {detail.items.length === 0 ? (
          <EmptyState
            title="No missions here yet"
            body="Add a mission from your library below — or save one first from its mission page."
          />
        ) : (
          <ul className="mb-8 space-y-3">
            {detail.items.map((item) => (
              <li
                key={item.id}
                className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-xl border border-space-800 bg-space-900 p-4"
              >
                <Link
                  to={`/mission/${item.missionId}`}
                  className="text-base font-semibold text-slate-100 underline decoration-slate-600 underline-offset-2 hover:text-white"
                >
                  {item.missionName}
                </Link>
                {item.note && <span className="text-sm text-slate-400">{item.note}</span>}
                <button
                  type="button"
                  onClick={() => void handleRemove(item.missionId)}
                  className="ml-auto inline-flex min-h-[44px] items-center rounded-md border border-red-900 px-3 text-sm text-red-400"
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
        )}

        <h2 className="mb-3 text-base font-semibold text-slate-100">Add from my library</h2>
        {addable.length === 0 ? (
          <p className="text-sm text-slate-400">
            Every saved mission is already in this collection — save more
            missions first.
          </p>
        ) : (
          <form onSubmit={handleAdd} className="flex flex-col gap-2 sm:flex-row">
            <label className="flex-1">
              <span className="sr-only">Mission to add</span>
              <select
                value={addMissionId}
                onChange={(e) => setAddMissionId(e.target.value)}
                className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
              >
                <option value="">Choose a saved mission…</option>
                {addable.map((s) => (
                  <option key={s.id} value={s.missionId}>
                    {s.missionName}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="submit"
              disabled={!addMissionId}
              className="inline-flex min-h-[44px] items-center justify-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
            >
              Add to collection
            </button>
          </form>
        )}
      </section>
    );
  }

  // --- List view: all collections ------------------------------------------
  return (
    <section className="pb-10">
      <h1 className="mb-1 text-2xl font-bold">My collections</h1>
      <p className="mb-5 text-sm text-slate-400">
        Group saved missions into folders for the topics you are researching.
        Collections are private — only you can see them.
      </p>
      {loading ? (
        <Loading />
      ) : error ? (
        <EmptyState title="Could not load collections" body={error} />
      ) : collections.length === 0 ? (
        <EmptyState
          title="No collections yet"
          body="Create one from My Library — give it a name, then add the missions that belong to it."
          action={
            <Link
              to="/my-library"
              className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
            >
              Go to My Library
            </Link>
          }
        />
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2">
          {collections.map((c) => (
            <li key={c.id} className="rounded-xl border border-space-800 bg-space-900 p-4">
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
    </section>
  );
}

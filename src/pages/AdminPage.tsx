import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../lib/authContext';
import { ApiError } from '../lib/auth';
import {
  createMission,
  deleteMission,
  listAllMissions,
  listAuditLogs,
  listUsers,
  updateMission,
} from '../lib/library';
import type { AuditLog } from '../lib/library';
import type { Mission } from '../lib/types';
import EmptyState from '../components/EmptyState';
import Loading from '../components/Loading';

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString();
}

const MISSION_STATUSES = ['upcoming', 'in_flight', 'completed', 'partial', 'failed', 'cancelled'];

export default function AdminPage() {
  const { user } = useAuth();
  const location = useLocation();
  const [missions, setMissions] = useState<Mission[]>([]);
  const [users, setUsers] = useState<
    Array<{ id: string; email: string; displayName: string; role: string; createdAt: string }>
  >([]);
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Mission form (create + edit share one form).
  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formId, setFormId] = useState('');
  const [formName, setFormName] = useState('');
  const [formTaskOrder, setFormTaskOrder] = useState('');
  const [formProvider, setFormProvider] = useState('');
  const [formLander, setFormLander] = useState('');
  const [formStatus, setFormStatus] = useState('upcoming');
  const [formError, setFormError] = useState('');
  const [formBusy, setFormBusy] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [ms, us, ls] = await Promise.all([
        listAllMissions(),
        listUsers(),
        listAuditLogs(100),
      ]);
      setMissions(ms);
      setUsers(us);
      setLogs(ls);
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 403
          ? 'Admins only — your account does not have the Admin role.'
          : 'Could not load the admin data — is the API running?',
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (user?.role === 'Admin') void load();
  }, [user, load]);

  if (!user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }
  if (user.role !== 'Admin') {
    return (
      <section>
        <h1 className="mb-1 text-2xl font-bold">Admin</h1>
        <EmptyState
          title="Admins only"
          body="Your account is a Researcher account. Only the Admin can manage missions, view accounts and read the audit log."
          action={
            <Link
              to="/my-library"
              className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
            >
              Go to My Library
            </Link>
          }
        />
      </section>
    );
  }

  function openCreateForm() {
    setEditingId(null);
    setFormId('');
    setFormName('');
    setFormTaskOrder('');
    setFormProvider('');
    setFormLander('');
    setFormStatus('upcoming');
    setFormError('');
    setFormOpen(true);
  }

  function openEditForm(m: Mission) {
    setEditingId(m.id);
    setFormId(m.id);
    setFormName(m.name);
    setFormTaskOrder(m.taskOrder ?? '');
    setFormProvider(m.provider ?? '');
    setFormLander(m.lander ?? '');
    setFormStatus(m.status);
    setFormError('');
    setFormOpen(true);
  }

  async function handleSubmitMission(e: FormEvent) {
    e.preventDefault();
    if (!formName.trim()) {
      setFormError('Mission name is required.');
      return;
    }
    setFormError('');
    setFormBusy(true);
    try {
      if (editingId) {
        await updateMission(editingId, {
          name: formName.trim(),
          taskOrder: formTaskOrder.trim() || null,
          provider: formProvider.trim() || null,
          lander: formLander.trim() || null,
          status: formStatus,
        });
      } else {
        if (!formId.trim()) {
          setFormError('Mission ID is required (e.g. to2-ab).');
          setFormBusy(false);
          return;
        }
        await createMission({
          id: formId.trim(),
          name: formName.trim(),
          taskOrder: formTaskOrder.trim() || null,
          provider: formProvider.trim() || null,
          lander: formLander.trim() || null,
          status: formStatus,
          launchDate: null,
          launchDatePrecision: null,
          landingDate: null,
          landingDatePrecision: null,
          landingSite: null,
          payloadCountReported: null,
          objective: null,
          outcomeSummary: null,
          notes: null,
          lastVerified: null,
          payloads: [],
          sources: [],
        });
      }
      setFormOpen(false);
      await load();
    } catch (err) {
      setFormError(
        err instanceof ApiError && err.status === 409
          ? 'A mission with this ID already exists.'
          : 'Could not save the mission — try again.',
      );
    } finally {
      setFormBusy(false);
    }
  }

  async function handleDeleteMission(id: string, name: string) {
    if (!window.confirm(`Delete the mission “${name}”? This cannot be undone.`)) return;
    setDeletingId(id);
    try {
      await deleteMission(id);
      await load();
    } catch {
      setError('Could not delete the mission — try again.');
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <section className="pb-10">
      <h1 className="mb-1 text-2xl font-bold">Admin</h1>
      <p className="mb-5 text-sm text-slate-400">
        Manage the mission dataset, see researcher accounts, and read the audit
        log of who changed what.
      </p>

      {loading ? (
        <Loading />
      ) : error ? (
        <EmptyState title="Could not load admin data" body={error} />
      ) : (
        <>
          <h2 className="mb-3 text-base font-semibold text-slate-100">
            Missions ({missions.length})
          </h2>
          <button
            type="button"
            onClick={openCreateForm}
            className="mb-4 inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950"
          >
            + Add mission
          </button>
          {formOpen && (
            <form
              onSubmit={handleSubmitMission}
              className="mb-6 rounded-xl border border-space-800 bg-space-900 p-4"
            >
              <h3 className="mb-3 text-sm font-semibold text-slate-100">
                {editingId ? 'Edit mission' : 'New mission'}
              </h3>
              <div className="grid gap-3 sm:grid-cols-2">
                <label>
                  <span className="mb-1 block text-sm text-slate-200">Mission ID</span>
                  <input
                    type="text"
                    value={formId}
                    disabled={editingId !== null}
                    onChange={(e) => setFormId(e.target.value)}
                    placeholder="e.g. to2-ab"
                    className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500 disabled:opacity-50"
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm text-slate-200">Name</span>
                  <input
                    type="text"
                    value={formName}
                    onChange={(e) => setFormName(e.target.value)}
                    placeholder="e.g. Peregrine Mission 1"
                    className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm text-slate-200">Task order</span>
                  <input
                    type="text"
                    value={formTaskOrder}
                    onChange={(e) => setFormTaskOrder(e.target.value)}
                    placeholder="e.g. TO2-AB"
                    className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm text-slate-200">Provider</span>
                  <input
                    type="text"
                    value={formProvider}
                    onChange={(e) => setFormProvider(e.target.value)}
                    className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm text-slate-200">Lander</span>
                  <input
                    type="text"
                    value={formLander}
                    onChange={(e) => setFormLander(e.target.value)}
                    className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100 placeholder:text-slate-500"
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm text-slate-200">Status</span>
                  <select
                    value={formStatus}
                    onChange={(e) => setFormStatus(e.target.value)}
                    className="min-h-[44px] w-full rounded-md border border-space-700 bg-space-950 px-3 text-sm text-slate-100"
                  >
                    {MISSION_STATUSES.map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
              {formError && <p className="mt-3 text-sm text-red-400">{formError}</p>}
              <div className="mt-4 flex gap-2">
                <button
                  type="submit"
                  disabled={formBusy}
                  className="inline-flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
                >
                  {formBusy ? 'Saving…' : editingId ? 'Save changes' : 'Create mission'}
                </button>
                <button
                  type="button"
                  onClick={() => setFormOpen(false)}
                  className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm text-slate-200"
                >
                  Cancel
                </button>
              </div>
            </form>
          )}
          <ul className="mb-8 space-y-2">
            {missions.map((m) => (
              <li
                key={m.id}
                className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-xl border border-space-800 bg-space-900 px-4 py-3"
              >
                <Link
                  to={`/mission/${m.id}`}
                  className="font-semibold text-slate-100 underline decoration-slate-600 underline-offset-2 hover:text-white"
                >
                  {m.name}
                </Link>
                <span className="text-sm text-slate-400">{m.id}</span>
                <span className="text-sm text-slate-400">{m.status}</span>
                <span className="ml-auto flex gap-2">
                  <button
                    type="button"
                    onClick={() => openEditForm(m)}
                    className="inline-flex min-h-[44px] items-center rounded-md border border-space-700 px-3 text-sm text-slate-200"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => void handleDeleteMission(m.id, m.name)}
                    disabled={deletingId === m.id}
                    className="inline-flex min-h-[44px] items-center rounded-md border border-red-900 px-3 text-sm text-red-400 disabled:opacity-50"
                  >
                    Delete
                  </button>
                </span>
              </li>
            ))}
          </ul>

          <h2 className="mb-3 text-base font-semibold text-slate-100">
            Researcher accounts ({users.length})
          </h2>
          <ul className="mb-8 space-y-2">
            {users.map((u) => (
              <li
                key={u.id}
                className="flex flex-wrap items-center gap-x-3 gap-y-1 rounded-xl border border-space-800 bg-space-900 px-4 py-3"
              >
                <span className="font-semibold text-slate-100">{u.displayName}</span>
                <span className="text-sm text-slate-400">{u.email}</span>
                <span
                  className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                    u.role === 'Admin'
                      ? 'bg-amber-500/20 text-amber-300'
                      : 'bg-sky-500/20 text-sky-300'
                  }`}
                >
                  {u.role}
                </span>
                <span className="ml-auto text-sm text-slate-400">
                  since {formatDate(u.createdAt)}
                </span>
              </li>
            ))}
          </ul>

          <h2 className="mb-3 text-base font-semibold text-slate-100">
            Audit log (newest first)
          </h2>
          {logs.length === 0 ? (
            <p className="text-sm text-slate-400">No changes recorded yet.</p>
          ) : (
            <ul className="space-y-2">
              {logs.map((l) => (
                <li
                  key={l.id}
                  className="rounded-xl border border-space-800 bg-space-900 px-4 py-3 text-sm"
                >
                  <span className="font-semibold text-slate-100">{l.action}</span>{' '}
                  <span className="text-slate-400">
                    {l.entityType} {l.entityId ?? ''}
                  </span>
                  <span className="float-right text-slate-500">
                    {l.userEmail ?? 'anonymous'} · {formatDate(l.createdAt)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}

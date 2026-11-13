import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useCompare } from '../components/CompareContext';
import EmptyState from '../components/EmptyState';
import HazardBriefing from '../components/HazardBriefing';
import SunEarthPanel from '../components/navigator/SunEarthPanel';
import PayloadList from '../components/PayloadList';
import SourceList from '../components/SourceList';
import StatusBadge from '../components/StatusBadge';
import { useAuth } from '../lib/authContext';
import { ApiError } from '../lib/auth';
import { listSaved, saveMission, unsaveMission } from '../lib/library';
import { getMissionById } from '../lib/missions';

export default function MissionDetailPage() {
  const { id } = useParams();
  const mission = id ? getMissionById(id) : undefined;
  const { ids, toggle } = useCompare();
  const { user } = useAuth();
  const [savedId, setSavedId] = useState<string | null>(null);
  const [saveBusy, setSaveBusy] = useState(false);
  const [saveNote, setSaveNote] = useState('');

  // Check once whether this mission is already in my library.
  useEffect(() => {
    if (!user || !mission) return;
    let cancelled = false;
    void listSaved()
      .then((items) => {
        if (cancelled) return;
        const found = items.find((i) => i.missionId === mission.id);
        if (found) setSavedId(found.id);
      })
      .catch(() => {
        // Library check is best-effort: the save button still works.
      });
    return () => {
      cancelled = true;
    };
  }, [user, mission?.id]);

  async function handleSave() {
    if (!mission) return;
    setSaveBusy(true);
    setSaveNote('');
    try {
      const saved = await saveMission(mission.id);
      setSavedId(saved.id);
    } catch (err) {
      setSaveNote(
        err instanceof ApiError && err.status === 401
          ? 'Your session expired — please log in again.'
          : 'Could not save — is the API running?',
      );
    } finally {
      setSaveBusy(false);
    }
  }

  async function handleUnsave() {
    if (!savedId) return;
    setSaveBusy(true);
    try {
      await unsaveMission(savedId);
      setSavedId(null);
    } catch {
      setSaveNote('Could not remove — try again.');
    } finally {
      setSaveBusy(false);
    }
  }

  if (!mission) {
    return (
      <EmptyState
        title="No mission with this id"
        body="The link may be old or mistyped."
        action={
          <Link to="/missions" className="inline-flex min-h-[44px] items-center text-accent underline">
            Back to missions
          </Link>
        }
      />
    );
  }

  const site = mission.landingSite;
  const siteLabel = site?.name
    ? `${site.name}${site.coordinatesApproximate ? ' (approx)' : ''}`
    : '-';

  return (
    <article>
      <Link to="/missions" className="text-sm inline-flex min-h-[44px] items-center text-accent underline">
        &lt;- All missions
      </Link>
      <header className="mb-4 mt-2 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">{mission.name}</h1>
          <p className="text-slate-300">
            {mission.provider ?? '-'} · Lander {mission.lander ?? '-'}
            {mission.taskOrder ? ` · Task order ${mission.taskOrder}` : ''}
          </p>
        </div>
        <StatusBadge status={mission.status} />
      </header>

      <dl className="mb-4 grid grid-cols-2 gap-x-4 gap-y-1 rounded-lg border border-space-800 bg-space-900 p-4 text-sm">
        <dt className="text-slate-400">Launch</dt>
        <dd>{mission.launchDate ?? '-'}</dd>
        <dt className="text-slate-400">Landing</dt>
        <dd>{mission.landingDate ?? '-'}</dd>
        <dt className="text-slate-400">Landing site</dt>
        <dd>{siteLabel}</dd>
        <dt className="text-slate-400">Region</dt>
        <dd>{site?.region ?? '-'}</dd>
      </dl>

      <div className="mb-6 flex flex-wrap gap-3">
        <Link
          to={`/map?mission=${mission.id}`}
          className="flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm hover:bg-space-900"
        >
          View on Moon Map
        </Link>
        <label className="flex min-h-[44px] cursor-pointer items-center gap-2 rounded-md border border-space-700 px-4 text-sm hover:bg-space-900">
          <input
            type="checkbox"
            checked={ids.includes(mission.id)}
            onChange={() => toggle(mission.id)}
            className="h-4 w-4"
          />
          Compare Mission
        </label>
        {user ? (
          savedId ? (
            <button
              type="button"
              onClick={() => void handleUnsave()}
              disabled={saveBusy}
              className="flex min-h-[44px] items-center rounded-md border border-emerald-800 px-4 text-sm text-emerald-300 disabled:opacity-50"
            >
              ✓ Saved in My Library — Remove
            </button>
          ) : (
            <button
              type="button"
              onClick={() => void handleSave()}
              disabled={saveBusy}
              className="flex min-h-[44px] items-center rounded-md bg-accent px-4 text-sm font-semibold text-space-950 disabled:opacity-50"
            >
              {saveBusy ? 'Saving…' : 'Save to My Library'}
            </button>
          )
        ) : (
          <Link
            to="/login"
            state={{ from: `/mission/${mission.id}` }}
            className="flex min-h-[44px] items-center rounded-md border border-space-700 px-4 text-sm hover:bg-space-900"
          >
            Log in to save this mission
          </Link>
        )}
      </div>
      {saveNote && <p className="mb-4 text-sm text-red-400">{saveNote}</p>}

      <div className="mb-6">
        <PayloadList mission={mission} />
      </div>

      {site?.latitude != null && site.longitude != null && (
        <SunEarthPanel
          site={{ latitude: site.latitude, longitude: site.longitude }}
          siteName={site.name ?? mission.name}
          missionId={mission.id}
        />
      )}

      <div className="mb-6">
        <HazardBriefing
          site={
            site?.latitude != null && site.longitude != null
              ? { latitude: site.latitude, longitude: site.longitude }
              : null
          }
          siteName={site?.name ?? mission.name}
        />
      </div>

      {mission.objective && (
        <section className="mb-4">
          <h2 className="mb-1 text-lg font-semibold">Objective</h2>
          <p className="text-slate-300">{mission.objective}</p>
        </section>
      )}
      {mission.outcomeSummary && (
        <section className="mb-4">
          <h2 className="mb-1 text-lg font-semibold">Outcome summary</h2>
          <p className="text-slate-300">{mission.outcomeSummary}</p>
        </section>
      )}
      {mission.notes && (
        <section className="mb-4">
          <h2 className="mb-1 text-lg font-semibold">Notes</h2>
          <p className="rounded-lg border border-signal-warn/30 bg-signal-warn/10 p-3 text-sm text-signal-warn">
            {mission.notes}
          </p>
        </section>
      )}

      <SourceList mission={mission} />
    </article>
  );
}

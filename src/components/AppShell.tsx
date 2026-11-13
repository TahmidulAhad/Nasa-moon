import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useMotionPrefs } from '../lib/motionPrefs';
import { useAuth } from '../lib/authContext';
import { hasWebGL } from '../lib/webgl';
import CompareTray from './CompareTray';
import Loading from './Loading';
import PageTransition from './PageTransition';
import ReduceMotionToggle from './ReduceMotionToggle';
import StarfieldBackground from './StarfieldBackground';
import { setNavRequest, useSceneFlags, useSceneQuality } from '../three/sceneStore';
import type { RouteKey } from '../three/SceneDirector';

// Lazy: three/R3F/drei ship in one shared chunk that never blocks first paint.
const SpaceCanvas = lazy(() => import('../three/SpaceCanvas'));

const NAV = [
  { to: '/', label: 'Home' },
  { to: '/missions', label: 'Missions' },
  { to: '/map', label: 'Map' },
  { to: '/features', label: 'Features' },
  { to: '/navigator', label: 'Navigator' },
  { to: '/south-pole', label: 'South Pole' },
  { to: '/why-south-pole', label: 'Why South Pole' },
  { to: '/moon-lab', label: 'Moon Lab' },
  { to: '/moon-interior', label: 'Moon Interior' },
  { to: '/atlas', label: 'Atlas' },
  { to: '/composition', label: 'Composition' },
  { to: '/library', label: 'Library' },
  { to: '/compare', label: 'Compare' },
  { to: '/payloads', label: 'Payloads' },
  { to: '/research', label: 'Research' },
];

export default function AppShell() {
  // The compare set is app-global state (FR-9): keep it across nav clicks.
  const [params] = useSearchParams();
  const compare = params.get('compare');
  const search = compare ? `?compare=${compare}` : '';
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { reduced } = useMotionPrefs();
  const quality = useSceneQuality();
  const { navRequest, mapMode } = useSceneFlags();
  const use3D = hasWebGL() && !reduced && quality > 0;

  // Mobile: the nav folds into a menu button so the header stays one row.
  const [menuOpen, setMenuOpen] = useState(false);
  useEffect(() => setMenuOpen(false), [pathname]);
  useEffect(() => {
    if (!menuOpen) return;
    const onDown = (e: PointerEvent) => {
      if (!(e.target as HTMLElement).closest('[data-mobile-menu]')) setMenuOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setMenuOpen(false);
    };
    document.addEventListener('pointerdown', onDown);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('pointerdown', onDown);
      document.removeEventListener('keydown', onKey);
    };
  }, [menuOpen]);

  // Desktop nav is one scrollable row (14 links never fit beside the brand):
  // a normal mouse wheel over the nav scrolls it sideways instead of the
  // page, and the ‹ › buttons do the same for mice without a horizontal
  // wheel. Arrows show only while the nav actually overflows; at either
  // end the wheel falls through to normal page scrolling.
  const navRef = useRef<HTMLElement | null>(null);
  const [navArrows, setNavArrows] = useState({ left: false, right: false });
  useEffect(() => {
    const el = navRef.current;
    if (!el) return;
    const update = () => {
      const max = el.scrollWidth - el.clientWidth;
      setNavArrows({ left: el.scrollLeft > 1, right: el.scrollLeft < max - 1 });
    };
    const onWheel = (e: WheelEvent) => {
      if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;
      const max = el.scrollWidth - el.clientWidth;
      if (max <= 1) return;
      if ((e.deltaY < 0 && el.scrollLeft <= 0) || (e.deltaY > 0 && el.scrollLeft >= max)) return;
      e.preventDefault();
      el.scrollLeft += e.deltaY;
    };
    update();
    el.addEventListener('scroll', update, { passive: true });
    el.addEventListener('wheel', onWheel, { passive: false });
    window.addEventListener('resize', update);
    return () => {
      el.removeEventListener('scroll', update);
      el.removeEventListener('wheel', onWheel);
      window.removeEventListener('resize', update);
    };
  }, []);
  const scrollNav = (dir: 1 | -1) =>
    navRef.current?.scrollBy({ left: dir * 240, behavior: reduced ? 'auto' : 'smooth' });

  const routeKey: RouteKey =
    pathname === '/'
      ? 'home'
      : pathname.startsWith('/missions')
        ? 'missions'
        : pathname.startsWith('/map')
          ? 'map'
          : pathname.startsWith('/compare')
            ? 'compare'
            : pathname.startsWith('/payloads')
              ? 'payloads'
              : 'other';

  // Interactive 3D routes: page boxes must let pointer events fall through to
  // the canvas; interactive DOM clusters re-enable themselves with pe-auto.
  // (Payloads used to be one; its graph now lives in its own panel canvas.)
  const interactiveRoute = routeKey === 'map' && mapMode === 'globe';

  // 3D → router bridge (mission nodes in scenes ask to navigate).
  useEffect(() => {
    if (navRequest) {
      setNavRequest(null);
      navigate(navRequest);
    }
  }, [navRequest, navigate]);

  // The #root wrapper box also swallows pointer events unless told otherwise.
  useEffect(() => {
    const root = document.getElementById('root');
    if (root) root.style.pointerEvents = interactiveRoute ? 'none' : '';
    return () => {
      if (root) root.style.pointerEvents = '';
    };
  }, [interactiveRoute]);

  // Defer the 240 kB 3D chunk until the browser is idle: it must never compete
  // with first-paint resources on any route (brief: no blocked/slow pages).
  const [canvasReady, setCanvasReady] = useState(false);
  useEffect(() => {
    const win = window as unknown as {
      requestIdleCallback?: (cb: () => void, opts?: { timeout: number }) => void;
    };
    if (win.requestIdleCallback) win.requestIdleCallback(() => setCanvasReady(true), { timeout: 2500 });
    else setTimeout(() => setCanvasReady(true), 1500);
  }, []);

  return (
    <div
      className={`relative flex min-h-screen flex-col ${interactiveRoute ? 'pointer-events-none' : ''}`}
    >
      {use3D && canvasReady ? (
        <Suspense fallback={<StarfieldBackground />}>
          <SpaceCanvas route={routeKey} />
        </Suspense>
      ) : (
        <StarfieldBackground />
      )}
      <header className="pointer-events-auto sticky top-0 z-40 border-b border-space-800 bg-space-950/70 backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center gap-x-6 px-4 py-3">
          <NavLink
            to="/"
            className="flex min-h-[44px] shrink-0 items-center text-lg font-semibold tracking-tight text-slate-100"
          >
            CLPS Lunar Mission Browser
          </NavLink>
          <button
            type="button"
            aria-label="Scroll navigation left"
            onClick={() => scrollNav(-1)}
            disabled={!navArrows.left}
            className="ml-auto hidden min-h-[44px] min-w-[44px] shrink-0 items-center justify-center rounded-md border border-space-700 text-lg text-slate-200 disabled:opacity-30 md:flex"
          >
            ‹
          </button>
          <nav
            ref={navRef}
            className="ml-auto hidden min-w-0 flex-1 items-center gap-1 overflow-x-auto whitespace-nowrap [mask-image:linear-gradient(to_right,black_92%,transparent)] [scrollbar-width:none] md:flex [&::-webkit-scrollbar]:hidden"
          >
            {NAV.map(({ to, label }) => (
              <NavLink
                key={to}
                to={{ pathname: to, search }}
                end={to === '/' || to === '/missions'}
                className={({ isActive }) =>
                  `flex min-h-[44px] shrink-0 items-center rounded-md px-3 text-sm transition-colors duration-200 ${
                    isActive
                      ? 'bg-space-800 text-white shadow-glow'
                      : 'text-slate-300 hover:bg-space-900'
                  }`
                }
              >
                {label}
              </NavLink>
            ))}
          </nav>
          {/* Researcher account: log in / register when logged out; My
          Library + profile + log out (and Admin for admins) when logged in. */}
          <div className="hidden shrink-0 items-center gap-1 md:flex">
            {user ? (
              <>
                <NavLink
                  to="/my-library"
                  className={({ isActive }) =>
                    `flex min-h-[44px] shrink-0 items-center rounded-md px-3 text-sm transition-colors duration-200 ${
                      isActive
                        ? 'bg-space-800 text-white shadow-glow'
                        : 'text-slate-300 hover:bg-space-900'
                    }`
                  }
                >
                  My Library
                </NavLink>
                {user.role === 'Admin' && (
                  <NavLink
                    to="/admin"
                    className={({ isActive }) =>
                      `flex min-h-[44px] shrink-0 items-center rounded-md px-3 text-sm transition-colors duration-200 ${
                        isActive
                          ? 'bg-space-800 text-white shadow-glow'
                          : 'text-amber-300 hover:bg-space-900'
                      }`
                    }
                  >
                    Admin
                  </NavLink>
                )}
                <NavLink
                  to="/profile"
                  className={({ isActive }) =>
                    `flex min-h-[44px] shrink-0 items-center rounded-md px-3 text-sm transition-colors duration-200 ${
                      isActive
                        ? 'bg-space-800 text-white shadow-glow'
                        : 'text-slate-300 hover:bg-space-900'
                    }`
                  }
                >
                  {user.displayName}
                </NavLink>
                <button
                  type="button"
                  onClick={() => {
                    void logout().then(() => navigate('/', { replace: true }));
                  }}
                  className="flex min-h-[44px] shrink-0 items-center rounded-md px-3 text-sm text-slate-300 hover:bg-space-900"
                >
                  Log out
                </button>
              </>
            ) : (
              <>
                <NavLink
                  to="/login"
                  className={({ isActive }) =>
                    `flex min-h-[44px] shrink-0 items-center rounded-md px-3 text-sm transition-colors duration-200 ${
                      isActive
                        ? 'bg-space-800 text-white shadow-glow'
                        : 'text-slate-300 hover:bg-space-900'
                    }`
                  }
                >
                  Log in
                </NavLink>
                <NavLink
                  to="/register"
                  className="flex min-h-[44px] shrink-0 items-center rounded-md bg-accent px-3 text-sm font-semibold text-space-950"
                >
                  Register
                </NavLink>
              </>
            )}
          </div>
          <button
            type="button"
            aria-label="Scroll navigation right"
            onClick={() => scrollNav(1)}
            disabled={!navArrows.right}
            className="hidden min-h-[44px] min-w-[44px] shrink-0 items-center justify-center rounded-md border border-space-700 text-lg text-slate-200 disabled:opacity-30 md:flex"
          >
            ›
          </button>
          <button
            type="button"
            data-mobile-menu
            className="ml-auto flex min-h-[44px] min-w-[44px] shrink-0 items-center justify-center rounded-md border border-space-700 px-2 text-slate-200 md:hidden"
            aria-expanded={menuOpen}
            aria-label={menuOpen ? 'Close menu' : 'Open menu'}
            onClick={() => setMenuOpen((o) => !o)}
          >
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
              {menuOpen ? (
                <path
                  d="M5 5l10 10M15 5L5 15"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                />
              ) : (
                <path
                  d="M3 5h14M3 10h14M3 15h14"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                />
              )}
            </svg>
          </button>
        </div>
        {menuOpen && (
          <div data-mobile-menu className="mx-auto max-w-6xl px-4 pb-3 md:hidden">
            <nav className="flex flex-col gap-1">
              {NAV.map(({ to, label }) => (
                <NavLink
                  key={to}
                  to={{ pathname: to, search }}
                  end={to === '/' || to === '/missions'}
                  className={({ isActive }) =>
                    `flex min-h-[44px] items-center rounded-md px-3 text-sm transition-colors duration-200 ${
                      isActive
                        ? 'bg-space-800 text-white shadow-glow'
                        : 'text-slate-300 hover:bg-space-900'
                    }`
                  }
              >
                {label}
              </NavLink>
            ))}
              {user ? (
                <>
                  <NavLink
                    to="/my-library"
                    className="flex min-h-[44px] items-center rounded-md px-3 text-sm text-slate-300 hover:bg-space-900"
                  >
                    My Library
                  </NavLink>
                  {user.role === 'Admin' && (
                    <NavLink
                      to="/admin"
                      className="flex min-h-[44px] items-center rounded-md px-3 text-sm text-amber-300 hover:bg-space-900"
                    >
                      Admin
                    </NavLink>
                  )}
                  <NavLink
                    to="/profile"
                    className="flex min-h-[44px] items-center rounded-md px-3 text-sm text-slate-300 hover:bg-space-900"
                  >
                    {user.displayName} (Profile)
                  </NavLink>
                  <button
                    type="button"
                    onClick={() => {
                      void logout().then(() => navigate('/', { replace: true }));
                    }}
                    className="flex min-h-[44px] items-center rounded-md px-3 text-left text-sm text-slate-300 hover:bg-space-900"
                  >
                    Log out
                  </button>
                </>
              ) : (
                <>
                  <NavLink
                    to="/login"
                    className="flex min-h-[44px] items-center rounded-md px-3 text-sm text-slate-300 hover:bg-space-900"
                  >
                    Log in
                  </NavLink>
                  <NavLink
                    to="/register"
                    className="flex min-h-[44px] items-center rounded-md px-3 text-sm font-semibold text-accent hover:bg-space-900"
                  >
                    Register
                  </NavLink>
                </>
              )}
            </nav>
          </div>
        )}
      </header>

      <main className="relative z-10 mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <PageTransition>
          {/* The shell (header/nav/footer) stays up while a lazy page chunk
          loads — previously the outer Suspense blanked the whole page to a
          bare "Loading…". The fallback below keeps the page shape + a label
          naming what is loading. */}
          <Suspense fallback={<Loading />}>
            <Outlet />
          </Suspense>
        </PageTransition>
      </main>

      <footer
        className={`pointer-events-auto relative z-10 border-t border-space-800 text-xs leading-relaxed text-slate-400 ${
          interactiveRoute ? 'bg-space-950' : ''
        }`}
      >
        <div className="mx-auto max-w-6xl px-4 py-4">
          <p>
            Basemap: NASA/LRO/LROC Team (Arizona State University) WAC global morphology mosaic,
            published by USGS Astrogeology Science Center — public domain, please cite authors.
          </p>
          <p>
            Moon 3D model: NASA Solar System Exploration, &quot;Earth&apos;s Moon&quot; — public
            domain.
          </p>
          <p>Mission data: NASA Science, &quot;CLPS Deliveries&quot; (science.nasa.gov).</p>
          <p>Lander details: mission provider pages (Astrobotic, Intuitive Machines, Firefly, …).</p>
          <p className="mt-2 text-slate-400">
            Educational geometric model. Independent project, not affiliated with or endorsed
            by NASA.
          </p>
          {/* Moved here from the header (boss: declutter) — still one click away. */}
          <div className="mt-3">
            <ReduceMotionToggle />
          </div>
        </div>
      </footer>
      <CompareTray />
    </div>
  );
}

import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { initMissions } from './lib/missions';
import './index.css';

// Load missions from the API first (falls back to the bundled static data
// when VITE_API_URL is unset or the API is unreachable), and only then
// import the app — pages read `missions` at module scope, so the data must
// be final before any page module is evaluated.
initMissions().finally(async () => {
  const { default: App } = await import('./App');
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </React.StrictMode>,
  );
});

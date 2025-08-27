import { useEffect, useState } from 'react';
import { login, logout, refresh } from './lib/auth';
import { apiFetch } from './lib/api';
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

function App() {
  const [count, setCount] = useState(0)
  const [u, setU] = useState('');
  const [p, setP] = useState('');
  const [out, setOut] = useState('');

  // NEW: keep structured lookup state so we can update per-match
  const [lookupState, setLookupState] = useState(null);

  // NEW: inputs for Riot lookup
  const [platform, setPlatform] = useState('americas');
  const [riotName, setRiotName] = useState('');
  const [riotTag, setRiotTag] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // Try to get a fresh access token from the refresh cookie on load
    refresh().then(tok => {
      if (tok) setOut('Session restored.');
    }).catch(() => {});
  }, []);

  async function onLogin(e) {
    e.preventDefault();
    try {
      await login(u, p);
      setOut('Logged in.');
    } catch (e) {
      setOut(`Login failed: ${e.message}`);
    }
  }

  async function me() {
    const r = await apiFetch('/api/auth/me');
    setOut(`${r.status}: ${await r.text()}`);
  }

  // NEW: helper to update one match detail entry
  function updateDetail(id, patch) {
    setLookupState(prev => {
      if (!prev) return prev;
      const curr = prev.details?.[id] || { status: 'pending' };
      const next = { ...curr, ...patch };
      return { ...prev, details: { ...(prev.details || {}), [id]: next } };
    });
  }

  // NEW: poll for a match detail until it’s ready (or timeout)
  async function fetchMatchDetail(platform, id, attempt = 0) {
    const res = await apiFetch(`/api/matches/${platform}/by-id/${id}`);
    if (res.status === 202) {
      updateDetail(id, { status: 'fetching' });
      if (attempt < 12) {
        const ra = res.headers.get('Retry-After');
        const delay = ra ? Number(ra) * 1000 : 1000 * Math.min(5, attempt + 1);
        setTimeout(() => fetchMatchDetail(platform, id, attempt + 1), delay);
      } else {
        updateDetail(id, { status: 'timeout' });
      }
      return;
    }
    if (!res.ok) {
      updateDetail(id, { status: 'error', error: await res.text() });
      return;
    }
    const data = await res.json();
    updateDetail(id, { status: 'done', data });
  }

  // NEW: lookup flow -> get puuid -> get matches -> start detail polling
  async function lookup() {
    setLoading(true);
    setOut('');
    setLookupState(null);
    try {
      if (!riotName || !riotTag) {
        setOut('Enter Riot username and tag.');
        return;
      }

      // 1) Get PUUID
      const puuidRes = await apiFetch(`/api/summoners/${platform}/${encodeURIComponent(riotName)}/${encodeURIComponent(riotTag)}`);
      if (!puuidRes.ok) {
        const txt = await puuidRes.text();
        setOut(`PUUID lookup failed (${puuidRes.status}): ${txt}`);
        return;
      }
      const account = await puuidRes.json();
      const puuid = account.puuid || account.Puuid || account.PUUID;
      if (!puuid) {
        setOut('PUUID not found in response.');
        return;
      }

      // 2) Fetch match ids for the PUUID
      const idsRes = await apiFetch(`/api/matches/${platform}/by-puuid/${encodeURIComponent(puuid)}`);
      if (!idsRes.ok) {
        const txt = await idsRes.text();
        setOut(`Match list failed (${idsRes.status}): ${txt}`);
        return;
      }
      const idsJson = await idsRes.json();
      const matchIds = Array.isArray(idsJson) ? idsJson : (idsJson.matches || idsJson.Matches || []);
      if (!matchIds.length) {
        setOut(`No matches found for ${riotName}#${riotTag} (${puuid}).`);
        return;
      }

      // 3) Initialize UI state and start polling a few details
      const firstFew = matchIds.slice(0, 5);
      setLookupState({ puuid, matches: firstFew, details: {} });
      firstFew.forEach(id => {
        updateDetail(id, { status: 'pending' });
        fetchMatchDetail(platform, id, 0);
      });
    } catch (err) {
      setOut(`Lookup error: ${err?.message || String(err)}`);
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/App.jsx</code> and save to test HMR
        </p>
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>

      <div style={{ padding: 16 }}>
        <h1>LoLSmith Demo</h1>
        {/* Auth */}
        <form onSubmit={onLogin}>
          <input placeholder="username" value={u} onChange={e => setU(e.target.value)} />
          <input placeholder="password" type="password" value={p} onChange={e => setP(e.target.value)} />
          <button type="submit">Login</button>
          <button type="button" onClick={async () => { await logout(); setOut('Logged out.'); }}>Logout</button>
          <button type="button" onClick={me}>Auth: Me</button>
        </form>

        {/* NEW: Riot lookup */}
        <div style={{ marginTop: 16, display: 'grid', gap: 8, maxWidth: 520 }}>
          <div style={{ display: 'flex', gap: 8 }}>
            <select value={platform} onChange={e => setPlatform(e.target.value)}>
              <option value="americas">americas</option>
              <option value="europe">europe</option>
              <option value="asia">asia</option>
            </select>
            <input placeholder="Riot username (e.g., Faker)" value={riotName} onChange={e => setRiotName(e.target.value)} />
            <input placeholder="Tag (e.g., T1)" value={riotTag} onChange={e => setRiotTag(e.target.value)} />
          </div>
          <div>
            <button disabled={loading} onClick={lookup}>{loading ? 'Looking up...' : 'Lookup PUUID + Matches'}</button>
          </div>
        </div>

        {/* Results */}
        <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap' }}>{out}</pre>

        {/* NEW: show live-updating structured result */}
        {lookupState && (
          <div style={{ marginTop: 12 }}>
            <div><b>PUUID:</b> {lookupState.puuid}</div>
            <div style={{ marginTop: 8 }}>
              <b>Matches:</b>
              <ul>
                {lookupState.matches.map(id => {
                  const d = lookupState.details?.[id];
                  return (
                    <li key={id}>
                      {id} — {d?.status || 'pending'}
                      {d?.status === 'error' && <span> — {d.error}</span>}
                      {d?.status === 'done' && <details style={{ marginTop: 4 }}>
                        <summary>details</summary>
                        <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(d.data, null, 2)}</pre>
                      </details>}
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        )}
      </div>
    </>
  )
}

export default App

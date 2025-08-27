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

  // NEW: lookup flow -> get puuid -> get matches -> fetch a few details
  async function lookup() {
    setLoading(true);
    setOut('');
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

      // 3) Pull a few match details (limit to first 5)
      const firstFew = matchIds.slice(0, 5);
      const details = await Promise.all(firstFew.map(async (id) => {
        const dRes = await apiFetch(`/api/matches/${platform}/by-id/${id}`);
        if (!dRes.ok) {
          return { id, error: await dRes.text(), status: dRes.status };
        }
        return { id, data: await dRes.json() };
      }));

      setOut(JSON.stringify({ puuid, matches: firstFew, details }, null, 2));
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

        <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap' }}>{out}</pre>
      </div>
    </>
  )
}

export default App

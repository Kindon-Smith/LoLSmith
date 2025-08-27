let accessToken = null;

export function getToken() {
  return accessToken;
}

export async function login(username, password) {
  const res = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include', // sets httpOnly refresh cookie via proxy
    body: JSON.stringify({ username, password })
  });
  if (!res.ok) throw new Error(await safeText(res));
  const json = await res.json();
  accessToken = json.access_token;
  return accessToken;
}

export async function refresh() {
  const res = await fetch('/api/auth/refresh', {
    method: 'POST',
    credentials: 'include' // sends refresh cookie
  });
  if (!res.ok) return null;
  const json = await res.json();
  accessToken = json.access_token;
  return accessToken;
}

export async function logout() {
  await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
  accessToken = null;
}

async function safeText(res) {
  try { return await res.text(); } catch { return ''; }
}
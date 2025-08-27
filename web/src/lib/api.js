import { getToken, refresh } from './auth';

export async function apiFetch(path, options = {}) {
  const opts = { ...options, headers: { ...(options.headers || {}) } };
  const token = getToken();
  if (token) opts.headers.Authorization = `Bearer ${token}`;

  let res = await fetch(path, opts);
  if (res.status !== 401) return res;

  // try one refresh then retry
  const newToken = await refresh();
  if (!newToken) return res;

  const retry = { ...opts, headers: { ...(opts.headers || {}), Authorization: `Bearer ${newToken}` } };
  return await fetch(path, retry);
}
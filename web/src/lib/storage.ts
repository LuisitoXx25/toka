import { useEffect, useState } from 'react'

// Browser storage can be unavailable (private mode, blocked site data); every access is guarded
// and the app keeps working in memory.

export function readJson<T>(storage: 'session' | 'local', key: string, fallback: T): T {
  try {
    const raw = (storage === 'session' ? sessionStorage : localStorage).getItem(key)
    return raw ? (JSON.parse(raw) as T) : fallback
  } catch {
    return fallback
  }
}

export function writeJson(storage: 'session' | 'local', key: string, value: unknown) {
  try {
    const target = storage === 'session' ? sessionStorage : localStorage
    if (value === null || value === undefined) target.removeItem(key)
    else target.setItem(key, JSON.stringify(value))
  } catch {
    // Storage full or blocked: state stays in memory only.
  }
}

/**
 * useState that survives navigation and reloads within the tab (sessionStorage).
 * Never use it for card data.
 */
export function useSessionState<T>(key: string, initial: T) {
  const [value, setValue] = useState<T>(() => readJson('session', key, initial))
  useEffect(() => writeJson('session', key, value), [key, value])
  return [value, setValue] as const
}

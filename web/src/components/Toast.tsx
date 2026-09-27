import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react'
import { ToastContext, type Tone, type ToastApi } from './toastContext'

interface Toast {
  id: number
  tone: Tone
  title: string
  message?: string
}

const AUTO_DISMISS_MS = 5000

const toastIcons: Record<Tone, ReactNode> = {
  success: <path d="m8 12.5 2.5 2.5L16 9.5M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z" />,
  error: <path d="m9 9 6 6m0-6-6 6M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z" />,
  warning: <path d="M12 8v5m0 3h.01M10.3 3.9 2.4 18a2 2 0 0 0 1.7 3h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />,
  info: <path d="M12 11v6m0-9h.01M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z" />,
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextId = useRef(1)

  const dismiss = useCallback((id: number) => setToasts((current) => current.filter((t) => t.id !== id)), [])

  const notify = useCallback<ToastApi['notify']>((tone, title, message) => {
    const id = nextId.current++
    // Newest on top; keep at most three so they never cover the form.
    setToasts((current) => [{ id, tone, title, message }, ...current].slice(0, 3))
    if (tone === 'success' || tone === 'info') setTimeout(() => dismiss(id), AUTO_DISMISS_MS)
  }, [dismiss])

  const api = useMemo(() => ({ notify }), [notify])

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toasts" aria-live="polite" aria-relevant="additions">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast--${toast.tone}`} role={toast.tone === 'error' ? 'alert' : 'status'}>
            <svg className="toast__icon" viewBox="0 0 24 24" aria-hidden="true">{toastIcons[toast.tone]}</svg>
            <div>
              <p className="toast__title">{toast.title}</p>
              {toast.message && <p className="toast__message">{toast.message}</p>}
            </div>
            <button type="button" className="toast__close" aria-label="Cerrar aviso" onClick={() => dismiss(toast.id)}>
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18" /></svg>
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

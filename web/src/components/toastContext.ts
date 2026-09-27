import { createContext, useContext } from 'react'

export type Tone = 'success' | 'error' | 'warning' | 'info'

export interface ToastApi {
  /** Errors and warnings stay until dismissed; success and info close after a few seconds. */
  notify: (tone: Tone, title: string, message?: string) => void
}

export const ToastContext = createContext<ToastApi | null>(null)

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) throw new Error('useToast must be used inside ToastProvider')
  return context
}

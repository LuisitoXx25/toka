import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type ButtonHTMLAttributes } from 'react'

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
  hint?: string
}

export function Field({ label, error, hint, className, ...input }: FieldProps) {
  const id = useId()
  const describedBy = [error && `${id}-error`, hint && `${id}-hint`].filter(Boolean).join(' ') || undefined
  return (
    <div className={`field ${error ? 'field--invalid' : ''} ${className ?? ''}`}>
      <label htmlFor={id}>{label}</label>
      <input id={id} aria-invalid={error ? true : undefined} aria-describedby={describedBy} {...input} />
      {hint && !error && <p id={`${id}-hint`} className="field__hint">{hint}</p>}
      {error && <p id={`${id}-error`} className="field__error">{error}</p>}
    </div>
  )
}

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string
  children: ReactNode
}

export function SelectField({ label, children, className, ...select }: SelectFieldProps) {
  const id = useId()
  return (
    <div className={`field ${className ?? ''}`}>
      <label htmlFor={id}>{label}</label>
      <select id={id} {...select}>{children}</select>
    </div>
  )
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary'
  loading?: boolean
}

export function Button({ variant = 'primary', loading = false, children, disabled, ...button }: ButtonProps) {
  return (
    <button className={`button button--${variant}`} disabled={disabled || loading} aria-busy={loading || undefined} {...button}>
      {loading && <span className="spinner" aria-hidden="true" />}
      <span>{children}</span>
    </button>
  )
}

type Tone = 'error' | 'warning' | 'success' | 'info'

const icons: Record<Tone, ReactNode> = {
  error: <path d="m9 9 6 6m0-6-6 6M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z" />,
  warning: <path d="M12 8v5m0 3h.01M10.3 3.9 2.4 18a2 2 0 0 0 1.7 3h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />,
  success: <path d="m8 12.5 2.5 2.5L16 9.5M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z" />,
  info: <path d="M12 11v6m0-9h.01M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z" />,
}

export function Alert({ tone, title, children }: { tone: Tone; title: string; children?: ReactNode }) {
  return (
    <div className={`alert alert--${tone}`} role={tone === 'error' ? 'alert' : 'status'}>
      <svg className="alert__icon" viewBox="0 0 24 24" aria-hidden="true">{icons[tone]}</svg>
      <div>
        <p className="alert__title">{title}</p>
        {children && <div className="alert__body">{children}</div>}
      </div>
    </div>
  )
}

/** Support reference shown with server errors, so the user can report the exact request. */
export function Reference({ id }: { id: string | null }) {
  if (!id) return null
  return <p className="reference">Referencia para soporte: <code>{id}</code></p>
}

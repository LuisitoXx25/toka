import type { ReactNode } from 'react'

interface StepProps {
  number: number
  title: string
  state: 'active' | 'done' | 'locked'
  /** One-line recap shown when the step is completed. */
  summary?: ReactNode
  onEdit?: () => void
  editDisabled?: boolean
  children: ReactNode
}

/** Checkout section that unlocks in order and collapses to a recap once completed. */
export function Step({ number, title, state, summary, onEdit, editDisabled, children }: StepProps) {
  const titleId = `step-${number}-title`
  return (
    <section className={`step step--${state}`} aria-labelledby={titleId}>
      <header className="step__header">
        <span className="step__number" aria-hidden="true">
          {state === 'done' ? <svg viewBox="0 0 24 24"><path d="m6 12.5 4 4 8-9" /></svg> : number}
        </span>
        <h2 id={titleId} className="step__title">{title}</h2>
        {state === 'done' && onEdit && (
          <button type="button" className="link-button" onClick={onEdit} disabled={editDisabled}>Editar</button>
        )}
      </header>
      {state === 'done' && summary && <div className="step__summary">{summary}</div>}
      <div className={`collapse ${state === 'active' ? 'collapse--open' : ''}`} inert={state !== 'active'}>
        <div className="collapse__inner">
          <div className="step__body">{children}</div>
        </div>
      </div>
    </section>
  )
}

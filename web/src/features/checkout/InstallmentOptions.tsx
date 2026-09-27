import type { InstallmentPlans } from '../../api/types'

interface Props {
  options: InstallmentPlans | null
  selected: number
  onSelect: (months: number) => void
  disabled: boolean
}

/** Single payment or interest-free installments (MSI) offered by the API for the amount and card. */
export function InstallmentOptions({ options, selected, onSelect, disabled }: Props) {
  if (!options) return <p className="muted">Consultando formas de pago…</p>
  return (
    <fieldset className="options" disabled={disabled}>
      <legend className="options__legend">Forma de pago</legend>
      {options.plans.map((plan) => (
        <label key={plan.months} className={`option ${selected === plan.months ? 'option--selected' : ''}`}>
          <input type="radio" name="installments" value={plan.months} checked={selected === plan.months}
            onChange={() => onSelect(plan.months)} />
          <span>{plan.label}</span>
        </label>
      ))}
      {options.cardType === 'Debit' && (
        <p className="muted">Los meses sin intereses solo aplican con tarjeta de crédito.</p>
      )}
      {options.cardType !== 'Debit' && options.plans.length === 1 && (
        <p className="muted">Los meses sin intereses están disponibles a partir de $1,500.00.</p>
      )}
      {options.cardType === 'Unknown' && options.plans.length > 1 && (
        <p className="muted">Los meses sin intereses se confirman al capturar una tarjeta de crédito.</p>
      )}
    </fieldset>
  )
}

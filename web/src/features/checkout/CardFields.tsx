import { Field } from '../../components/ui'
import { detectBrand, formatCardNumber, formatExpiry, onlyDigits } from '../../lib/card'
import type { CardForm, FieldErrors } from '../../lib/validation'

interface Props {
  /** "Crédito" / "Débito" once the API resolved the BIN. */
  cardType?: string | null
  value: CardForm
  onChange: (value: CardForm) => void
  errors: FieldErrors
  disabled: boolean
}

export function CardFields({ cardType, value, onChange, errors, disabled }: Props) {
  const brand = detectBrand(onlyDigits(value.number))
  const hint = [brand, cardType].filter(Boolean).join(' · ') || undefined
  const set = (patch: Partial<CardForm>) => onChange({ ...value, ...patch })

  return (
    <div className="grid">
      <Field
        className="span-2"
        label="Número de tarjeta"
        inputMode="numeric"
        autoComplete="cc-number"
        placeholder="0000 0000 0000 0000"
        value={value.number}
        onChange={(e) => set({ number: formatCardNumber(e.target.value) })}
        error={errors['card.number']}
        hint={hint}
        disabled={disabled}
      />
      <Field
        label="Vencimiento"
        inputMode="numeric"
        autoComplete="cc-exp"
        placeholder="MM/AA"
        value={value.expiry}
        onChange={(e) => set({ expiry: formatExpiry(e.target.value) })}
        error={errors['card.expiry']}
        disabled={disabled}
      />
      <Field
        label="CVV"
        inputMode="numeric"
        autoComplete="cc-csc"
        placeholder="123"
        maxLength={4}
        value={value.cvv}
        onChange={(e) => set({ cvv: onlyDigits(e.target.value).slice(0, 4) })}
        error={errors['card.cvv']}
        disabled={disabled}
      />
      <Field
        className="span-2"
        label="Nombre del titular"
        autoComplete="cc-name"
        value={value.holderName}
        onChange={(e) => set({ holderName: e.target.value })}
        error={errors['card.holderName']}
        disabled={disabled}
      />
    </div>
  )
}

export function TestCards() {
  return (
    <details className="test-cards">
      <summary>Tarjetas de prueba del simulador</summary>
      <table>
        <tbody>
          <tr><td><code>4111 1111 1111 1111</code></td><td>Aprobada</td></tr>
          <tr><td><code>4000 0000 0000 0002</code></td><td>Rechazada por fondos insuficientes</td></tr>
          <tr><td><code>4000 0000 0000 0259</code></td><td>Falla el primer intento y se aprueba al reintentar</td></tr>
          <tr><td><code>4000 0000 0000 0119</code></td><td>El procesador no responde (pago fallido)</td></tr>
          <tr><td><code>4000 0000 0000 0341</code></td><td>Tiempo de espera agotado (pago fallido)</td></tr>
          <tr><td><code>4000 0566 5566 5556</code></td><td>Débito Visa: solo pago de contado</td></tr>
          <tr><td><code>5200 8282 8282 8210</code></td><td>Débito Mastercard: solo pago de contado</td></tr>
        </tbody>
      </table>
      <p className="muted">Cualquier fecha futura y CVV de 3 dígitos.</p>
    </details>
  )
}

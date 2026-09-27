import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../../api/client'
import type { Order } from '../../api/types'
import { useToast } from '../../components/toastContext'
import { Button } from '../../components/ui'
import { formatMoney } from '../../lib/format'
import { fromApiErrors, validateCard, type CardForm, type FieldErrors } from '../../lib/validation'
import { CardFields, TestCards } from './CardFields'
import { emptyCard, toCardInput } from './cardPayload'
import { notifyOutcome, notifyError } from './notifications'

interface Props {
  order: Order
  email: string
  onUpdated: (order: Order) => void
}

/** Pays a declined or failed order again, usually with another card. */
export function RetryPaymentForm({ order, email, onUpdated }: Props) {
  const toast = useToast()
  const [card, setCard] = useState<CardForm>(emptyCard)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [retrying, setRetrying] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const found = validateCard(card)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setRetrying(true)
    try {
      const updated = await api.retryPayment(order.id, email, toCardInput(card))
      notifyOutcome(toast, updated)
      onUpdated(updated)
    } catch (err) {
      const apiError = err instanceof ApiError ? err : new ApiError(0, 'unknown', 'Ocurrió un error inesperado.')
      setErrors(fromApiErrors(apiError.fieldErrors))
      notifyError(toast, apiError, 'No se pudo reintentar el pago')
    } finally {
      // Card data is never kept after an attempt.
      setCard(emptyCard)
      setRetrying(false)
    }
  }

  return (
    <form className="retry" onSubmit={submit} noValidate>
      <CardFields value={card} onChange={setCard} errors={errors} disabled={retrying} />
      <TestCards />
      {retrying && <div className="progress" role="progressbar" aria-label="Reintentando pago" />}
      <div className="actions">
        <Button type="submit" loading={retrying}>
          {retrying ? 'Reintentando pago…' : `Pagar ${formatMoney(order.total, order.currency)}`}
        </Button>
      </div>
      {retrying && <p className="muted" role="status">Confirmando con el banco. No cierres esta ventana.</p>}
    </form>
  )
}

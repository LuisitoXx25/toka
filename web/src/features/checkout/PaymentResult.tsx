import type { Order } from '../../api/types'
import { Alert, Button } from '../../components/ui'
import { formatMoney } from '../../lib/format'
import { navigate } from '../../lib/router'
import { RetryPaymentForm } from './RetryPaymentForm'

interface Props {
  order: Order
  email: string
  onUpdated: (order: Order) => void
  onNewPurchase: () => void
}

export function PaymentResult({ order, email, onUpdated, onNewPurchase }: Props) {
  if (order.status === 'Paid') {
    return (
      <section className="result reveal" aria-labelledby="result-title">
        <svg className="result__check" viewBox="0 0 52 52" aria-hidden="true">
          <circle cx="26" cy="26" r="24" />
          <path d="m15 27 7 7 15-16" />
        </svg>
        <h2 id="result-title" className="result__title">Pago aprobado</h2>
        <p className="muted">
          {formatMoney(order.total, order.currency)} en {order.paymentPlan}.
        </p>
        <dl className="details">
          <div><dt>Número de orden</dt><dd><code>{order.id}</code></dd></div>
          <div><dt>Autorización</dt><dd><code>{order.authorizationCode}</code></dd></div>
          <div><dt>Tarjeta</dt><dd>{lastCard(order)}</dd></div>
        </dl>
        <div className="actions">
          <Button onClick={() => navigate(`/compras/${order.id}`)}>Ver detalle</Button>
          <Button variant="secondary" onClick={onNewPurchase}>Nueva compra</Button>
        </div>
      </section>
    )
  }

  const declined = order.status === 'PaymentDeclined'
  return (
    <section className="result reveal" aria-labelledby="result-title">
      {declined ? (
        <Alert tone="error" title="Tarjeta rechazada">
          <p>{order.failureReason} El banco no autorizó el cargo a {lastCard(order)}. No se realizó ningún cobro.</p>
        </Alert>
      ) : (
        <Alert tone="warning" title="No pudimos procesar el pago">
          <p>{order.failureReason} No se realizó ningún cobro.</p>
        </Alert>
      )}
      <h2 id="result-title" className="section__title">{declined ? 'Pagar con otra tarjeta' : 'Intentar de nuevo'}</h2>
      <p className="muted">Orden <code>{order.id}</code>. El producto se aparta de nuevo al reintentar.</p>
      <RetryPaymentForm order={order} email={email} onUpdated={onUpdated} />
    </section>
  )
}

function lastCard(order: Order) {
  const attempt = order.attempts.at(-1)
  return attempt ? `${attempt.cardBrand} ${attempt.cardTypeDisplay.toLowerCase()} terminación ${attempt.cardLast4}` : 'la tarjeta'
}

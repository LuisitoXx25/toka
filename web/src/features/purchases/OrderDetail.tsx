import type { Order } from '../../api/types'
import { StatusLine } from '../../components/StatusLine'
import { formatDateTime, formatMoney, formatPercent } from '../../lib/format'
import { RetryPaymentForm } from '../checkout/RetryPaymentForm'

interface Props {
  order: Order
  email: string
  onUpdated: (order: Order) => void
}

export function OrderDetail({ order, email, onUpdated }: Props) {
  return (
    <div className="order-detail reveal">
      <section className="section">
        <div className="order-detail__header">
          <h2 className="section__title">Orden <code>{order.id}</code></h2>
          <StatusLine status={order.status} label={order.statusDisplay} at={order.updatedAt} />
        </div>
        {order.failureReason && <p className="muted">{order.failureReason}</p>}
        <dl className="details">
          <div><dt>Creada</dt><dd>{formatDateTime(order.createdAt)}</dd></div>
          <div><dt>Última actualización</dt><dd>{formatDateTime(order.updatedAt)}</dd></div>
          <div><dt>Cantidad</dt><dd>{order.quantity} × {formatMoney(order.unitPrice, order.currency)}</dd></div>
          <div><dt>Forma de pago</dt><dd>{order.paymentPlan}</dd></div>
          {order.authorizationCode && <div><dt>Autorización</dt><dd><code>{order.authorizationCode}</code></dd></div>}
        </dl>
        <dl className="summary__totals">
          <div><dt>Subtotal</dt><dd className="amount">{formatMoney(order.subtotal, order.currency)}</dd></div>
          <div><dt>IVA ({formatPercent(order.taxRate)})</dt><dd className="amount">{formatMoney(order.taxAmount, order.currency)}</dd></div>
          <div className="summary__total"><dt>Total</dt><dd className="amount">{formatMoney(order.total, order.currency)}</dd></div>
        </dl>
      </section>

      {order.canRetryPayment && (
        <section className="section">
          <h2 className="section__title">Pagar con otra tarjeta</h2>
          <RetryPaymentForm order={order} email={email} onUpdated={onUpdated} />
        </section>
      )}

      <section className="section">
        <h2 className="section__title">Intentos de pago</h2>
        {order.attempts.length === 0 ? <p className="muted">Sin intentos registrados.</p> : (
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>#</th><th>Fecha</th><th>Tarjeta</th><th>Resultado</th><th>Código</th></tr></thead>
              <tbody>
                {order.attempts.map((a) => (
                  <tr key={a.attemptNumber}>
                    <td className="amount">{a.attemptNumber}</td>
                    <td>{formatDateTime(a.occurredAt)}</td>
                    <td>{a.cardBrand} {a.cardTypeDisplay.toLowerCase()} ···· {a.cardLast4}</td>
                    <td>{a.message}</td>
                    <td><code>{a.responseCode}</code></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="section">
        <h2 className="section__title">Bitácora</h2>
        <ol className="timeline">
          {order.events.map((event, i) => (
            <li key={i}>
              <time dateTime={event.occurredAt}>{formatDateTime(event.occurredAt)}</time>
              <p>{event.description}</p>
            </li>
          ))}
        </ol>
      </section>
    </div>
  )
}

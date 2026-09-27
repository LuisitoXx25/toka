import type { Order, Product } from '../../api/types'
import { formatMoney, formatPercent } from '../../lib/format'

const IVA_RATE = 0.16

/** Same split the API uses: tax-included total, subtotal rounded to cents, IVA is the remainder. */
function preview(product: Product, quantity: number) {
  const total = Math.round(product.price * quantity * 100) / 100
  const subtotal = Math.round((total / (1 + IVA_RATE)) * 100) / 100
  return { subtotal, taxAmount: Math.round((total - subtotal) * 100) / 100, taxRate: IVA_RATE, total, currency: product.currency }
}

/** Re-mounts on change so the new value fades in. */
function Amount({ value, currency }: { value: number; currency: string }) {
  return <dd className="amount"><span key={value} className="tick">{formatMoney(value, currency)}</span></dd>
}

interface Props {
  product: Product | null
  quantity: number
  /** Label of the selected payment plan, e.g. "3 pagos de $1,166.33 sin intereses; el primero de $1,166.34". */
  planLabel: string | null
  order: Order | null
}

export function OrderSummary({ product, quantity, planLabel, order }: Props) {
  if (!product) {
    return (
      <aside className="summary" aria-label="Resumen del pedido">
        <h2 className="summary__title">Resumen del pedido</h2>
        <p className="muted">Selecciona un producto.</p>
      </aside>
    )
  }

  const amounts = order ?? preview(product, quantity)
  const units = order?.quantity ?? quantity

  return (
    <aside className="summary" aria-label="Resumen del pedido">
      <h2 className="summary__title">Resumen del pedido</h2>
      <div className="summary__item">
        <div>
          <p className="summary__name">{product.name}</p>
          <p className="muted">Cantidad: {units}</p>
        </div>
        <p className="amount"><span key={units} className="tick">{formatMoney(product.price * units, amounts.currency)}</span></p>
      </div>
      <dl className="summary__totals">
        <div><dt>Subtotal</dt><Amount value={amounts.subtotal} currency={amounts.currency} /></div>
        <div><dt>IVA ({formatPercent(amounts.taxRate)})</dt><Amount value={amounts.taxAmount} currency={amounts.currency} /></div>
        <div className="summary__total"><dt>Total</dt><Amount value={amounts.total} currency={amounts.currency} /></div>
      </dl>
      {(order?.paymentPlan ?? planLabel) && (
        <p className="summary__plan"><span key={order?.paymentPlan ?? planLabel} className="tick">{order?.paymentPlan ?? planLabel}</span></p>
      )}
      <p className="summary__note">Precios con IVA incluido.</p>
    </aside>
  )
}

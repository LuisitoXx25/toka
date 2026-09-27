import type { OrderStatus } from '../api/types'
import { formatDateTime } from '../lib/format'

const tone: Record<OrderStatus, string> = {
  Paid: 'success',
  PaymentDeclined: 'error',
  PaymentFailed: 'warning',
  PendingPayment: 'info',
}

const icons: Record<OrderStatus, string> = {
  Paid: 'm8 12.5 2.5 2.5L16 9.5',
  PaymentDeclined: 'm9 9 6 6m0-6-6 6',
  PaymentFailed: 'M12 7.5v5m0 3.5h.01',
  PendingPayment: 'M12 7v5l3 2',
}

interface Props {
  status: OrderStatus
  /** Formal label from the API, e.g. "Pago aprobado". */
  label: string
  /** When the status was last updated. */
  at?: string
}

/** Order status as a receipt line: icon, formal label and, optionally, the date of the last change. */
export function StatusLine({ status, label, at }: Props) {
  return (
    <span className={`status status--${tone[status]}`}>
      <svg className="status__icon" viewBox="0 0 24 24" aria-hidden="true">
        <circle cx="12" cy="12" r="10" />
        <path d={icons[status]} />
      </svg>
      <span className="status__label">{label}</span>
      {at && <span className="status__at">· {formatDateTime(at)}</span>}
    </span>
  )
}

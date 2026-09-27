import type { ApiError } from '../../api/client'
import type { Order } from '../../api/types'
import type { useToast } from '../../components/toastContext'
import { formatMoney } from '../../lib/format'

type Toast = ReturnType<typeof useToast>

/** Toast for the final state of a payment. */
export function notifyOutcome(toast: Toast, order: Order) {
  if (order.status === 'Paid') toast.notify('success', 'Pago aprobado', `Se cobraron ${formatMoney(order.total, order.currency)}.`)
  else if (order.status === 'PaymentDeclined') toast.notify('error', 'Tarjeta rechazada', order.failureReason ?? undefined)
  else if (order.status === 'PaymentFailed') toast.notify('warning', 'No pudimos procesar el pago', order.failureReason ?? undefined)
}

/** Toast for request errors. Field errors are shown next to each field instead. */
export function notifyError(toast: Toast, error: ApiError, title: string) {
  if (Object.keys(error.fieldErrors).length > 0) {
    toast.notify('error', 'Revisa los datos marcados', error.message)
    return
  }
  const reference = error.correlationId ? ` Referencia: ${error.correlationId}` : ''
  toast.notify('error', title, `${error.message}${reference}`)
}

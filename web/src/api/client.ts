import type { CardInput, CustomerInput, InstallmentPlans, Order, Product } from './types'

/** Error returned by the API (RFC 9457 problem) or produced locally for network failures. Messages are user-facing. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly fieldErrors: Record<string, string[]>
  readonly correlationId: string | null

  constructor(status: number, code: string, message: string, fieldErrors: Record<string, string[]> = {}, correlationId: string | null = null) {
    super(message)
    this.status = status
    this.code = code
    this.fieldErrors = fieldErrors
    this.correlationId = correlationId
  }

  /** True when the request may have reached the server but the outcome is unknown (retry with the same idempotency key). */
  get isOutcomeUnknown() {
    return this.status === 0 || this.status >= 500
  }
}

// Longest backend path: 3 gateway attempts with timeout and backoff. Leave margin on top.
const TIMEOUT_MS = 20_000

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api/v1${path}`, {
      ...init,
      // Same-origin only: the API key is added by the proxy, never by the browser.
      credentials: 'same-origin',
      headers: { Accept: 'application/json', ...(init.body ? { 'Content-Type': 'application/json' } : {}), ...init.headers },
      signal: AbortSignal.timeout(TIMEOUT_MS),
    })
  } catch (e) {
    const timedOut = e instanceof DOMException && e.name === 'TimeoutError'
    throw new ApiError(0, timedOut ? 'timeout' : 'network',
      timedOut
        ? 'El servidor tardó demasiado en responder. Revisa el estado de tu orden antes de intentar de nuevo.'
        : 'No pudimos conectar con el servidor. Revisa tu conexión e intenta de nuevo.')
  }

  const correlationId = response.headers.get('X-Correlation-Id')
  const body: unknown = await response.json().catch(() => null)

  if (response.ok) return body as T

  const problem = (body ?? {}) as { detail?: string; title?: string; code?: string; errors?: Record<string, string[]> }
  // Only the problem's user-facing text is shown; anything unexpected gets a generic message.
  throw new ApiError(
    response.status,
    problem.code ?? 'unknown',
    problem.detail ?? problem.title ?? 'Ocurrió un error inesperado. Intenta de nuevo más tarde.',
    problem.errors ?? {},
    correlationId,
  )
}

export const api = {
  listProducts: () => request<Product[]>('/products'),

  /** Payment options for an amount. With the card BIN (first 6 digits) the API resolves credit/debit; debit only allows a single payment. */
  installmentPlans: (amount: number, bin: string | null) =>
    request<InstallmentPlans>(`/installment-plans?amount=${encodeURIComponent(amount.toFixed(2))}${bin ? `&bin=${encodeURIComponent(bin)}` : ''}`),

  /** Guest lookup: needs the buyer's email; a mismatch is reported as "not found". */
  lookupOrder: (orderId: string, email: string) =>
    request<Order>('/orders/lookup', { method: 'POST', body: JSON.stringify({ orderId, email }) }),

  placeOrder: (
    input: { customer: CustomerInput; productId: string; quantity: number; installments: number; card: CardInput },
    idempotencyKey: string,
  ) =>
    request<Order>('/orders', {
      method: 'POST',
      body: JSON.stringify(input),
      headers: { 'Idempotency-Key': idempotencyKey },
    }),

  retryPayment: (orderId: string, email: string, card: CardInput) =>
    request<Order>(`/orders/${encodeURIComponent(orderId)}/payment-retries`, {
      method: 'POST',
      body: JSON.stringify({ email, card }),
    }),
}

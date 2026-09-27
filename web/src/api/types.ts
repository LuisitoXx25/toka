export interface Product {
  id: string
  sku: string
  name: string
  description: string
  price: number
  currency: string
  stock: number
}

export type CardType = 'Credit' | 'Debit' | 'Unknown'

export type OrderStatus = 'PendingPayment' | 'Paid' | 'PaymentDeclined' | 'PaymentFailed'

export interface PaymentAttempt {
  attemptNumber: number
  outcome: 'Approved' | 'Declined' | 'TransientError'
  responseCode: string
  message: string | null
  cardBrand: string
  cardLast4: string
  cardType: CardType
  cardTypeDisplay: string
  occurredAt: string
}

export interface AuditEntry {
  occurredAt: string
  eventType: string
  description: string
  correlationId: string | null
}

export interface Order {
  id: string
  productId: string
  quantity: number
  unitPrice: number
  subtotal: number
  taxRate: number
  taxAmount: number
  total: number
  currency: string
  installments: number
  firstPayment: number
  monthlyPayment: number
  /** Ready-to-show plan, e.g. "3 pagos de $1,166.33 sin intereses; el primero de $1,166.34". */
  paymentPlan: string
  status: OrderStatus
  statusDisplay: string
  authorizationCode: string | null
  failureReason: string | null
  canRetryPayment: boolean
  createdAt: string
  updatedAt: string
  attempts: PaymentAttempt[]
  events: AuditEntry[]
}

export interface CustomerInput {
  firstName: string
  lastName: string
  email: string
  phone: string
}

export interface CardInput {
  holderName: string
  number: string
  expiryMonth: number
  expiryYear: number
  cvv: string
}

export interface InstallmentPlan {
  months: number
  /** Ready-to-show text, e.g. "3 pagos de $1,166.33 sin intereses; el primero de $1,166.34". */
  label: string
  /** Absorbs the rounding difference; equals monthlyPayment when the split is exact. */
  firstPayment: number
  monthlyPayment: number
  minimumAmount: number
}

export interface InstallmentPlans {
  cardType: CardType
  /** "Crédito" / "Débito"; null until a BIN is sent. */
  cardTypeDisplay: string | null
  plans: InstallmentPlan[]
}

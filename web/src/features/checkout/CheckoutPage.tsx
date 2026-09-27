import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api, ApiError } from '../../api/client'
import type { InstallmentPlans, Order, Product } from '../../api/types'
import { Step } from '../../components/Step'
import { useToast } from '../../components/toastContext'
import { Alert, Button, Field, Reference, SelectField } from '../../components/ui'
import { describeInstallments, formatMoney } from '../../lib/format'
import { onlyDigits } from '../../lib/card'
import { purchases } from '../../lib/purchases'
import { useSessionState } from '../../lib/storage'
import { fromApiErrors, validateCard, validateContact, type CardForm, type ContactForm, type FieldErrors } from '../../lib/validation'
import { CardFields, TestCards } from './CardFields'
import { emptyCard, toCardInput } from './cardPayload'
import { InstallmentOptions } from './InstallmentOptions'
import { notifyError, notifyOutcome } from './notifications'
import { OrderSummary } from './OrderSummary'
import { PaymentResult } from './PaymentResult'
import { ProductList, ProductListSkeleton } from './ProductList'

type Catalog = { state: 'loading' } | { state: 'error'; error: ApiError } | { state: 'ready'; products: Product[] }

const MAX_QUANTITY = 10
const emptyContact: ContactForm = { firstName: '', lastName: '', email: '', phone: '' }

export function CheckoutPage() {
  const toast = useToast()
  const [catalog, setCatalog] = useState<Catalog>({ state: 'loading' })

  // Everything except card data survives navigation and reloads within the tab.
  const [step, setStep] = useSessionState<1 | 2 | 3>('checkout.step', 1)
  const [productId, setProductId] = useSessionState<string | null>('checkout.productId', null)
  const [quantity, setQuantity] = useSessionState('checkout.quantity', 1)
  const [installments, setInstallments] = useSessionState('checkout.installments', 1)
  const [contact, setContact] = useSessionState<ContactForm>('checkout.contact', emptyContact)
  const [order, setOrder] = useSessionState<Order | null>('checkout.order', null)
  // One key per purchase, reused when the outcome of a submit is unknown (timeout, 5xx)
  // so paying again returns the same order instead of charging twice.
  const [idempotencyKey, setIdempotencyKey] = useSessionState<string | null>('checkout.idempotencyKey', null)

  const [card, setCard] = useState<CardForm>(emptyCard)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [submitting, setSubmitting] = useState(false)
  const [slow, setSlow] = useState(false)
  const [plansFor, setPlansFor] = useState<{ key: string; options: InstallmentPlans } | null>(null)

  const fetchCatalog = useCallback(
    () =>
      api.listProducts().then(
        (products) => {
          setCatalog({ state: 'ready', products })
          setProductId((current) => (current && products.some((p) => p.id === current) ? current : products.find((p) => p.stock > 0)?.id ?? null))
        },
        (err: unknown) =>
          setCatalog({ state: 'error', error: err instanceof ApiError ? err : new ApiError(0, 'unknown', 'No se pudo cargar el catálogo.') }),
      ),
    [setProductId],
  )

  useEffect(() => {
    void fetchCatalog()
  }, [fetchCatalog])

  function reloadCatalog() {
    setCatalog({ state: 'loading' })
    void fetchCatalog()
  }

  const products = catalog.state === 'ready' ? catalog.products : []
  const product = products.find((p) => p.id === productId) ?? null
  const maxQuantity = Math.min(MAX_QUANTITY, product?.stock ?? 1)
  const amount = product ? Math.round(product.price * quantity * 100) / 100 : 0

  // The BIN (first 6 digits) is enough to know credit vs debit; the rest of the number never leaves the form until payment.
  const digits = onlyDigits(card.number)
  const bin = digits.length >= 6 ? digits.slice(0, 6) : null
  const plansKey = `${amount}:${bin ?? ''}`

  useEffect(() => {
    if (!amount) return
    let cancelled = false
    api.installmentPlans(amount, bin).then(
      (options) => !cancelled && setPlansFor({ key: plansKey, options }),
      // Without plans the purchase can still be paid in a single payment.
      () => !cancelled && setPlansFor({
        key: plansKey,
        options: { cardType: 'Unknown', cardTypeDisplay: null, plans: [{ months: 1, label: describeInstallments(1, amount), firstPayment: amount, monthlyPayment: amount, minimumAmount: 0 }] },
      }),
    )
    return () => { cancelled = true }
  }, [amount, bin, plansKey])

  const planOptions = plansFor?.key === plansKey ? plansFor.options : null
  // A plan chosen earlier may no longer apply (different amount, or a debit card).
  const selectedPlan = planOptions?.plans.find((p) => p.months === installments) ?? planOptions?.plans[0] ?? null
  const effectiveInstallments = selectedPlan?.months ?? 1

  useEffect(() => {
    if (!submitting) return
    const timer = setTimeout(() => setSlow(true), 2500)
    return () => {
      clearTimeout(timer)
      setSlow(false)
    }
  }, [submitting])

  function continueFromContact() {
    const found = validateContact(contact)
    setErrors(found)
    if (Object.keys(found).length === 0) setStep(3)
  }

  async function pay(e: FormEvent) {
    e.preventDefault()
    if (!product) return
    const found = { ...validateContact(contact), ...validateCard(card) }
    setErrors(found)
    if (Object.keys(found).length > 0) {
      if (Object.keys(found).some((k) => k.startsWith('customer.'))) setStep(2)
      return
    }

    const key = idempotencyKey ?? crypto.randomUUID()
    setIdempotencyKey(key)
    setSubmitting(true)
    try {
      const email = contact.email.trim()
      const result = await api.placeOrder(
        { customer: { ...contact, email }, productId: product.id, quantity, installments: effectiveInstallments, card: toCardInput(card) },
        key,
      )
      setIdempotencyKey(null)
      purchases.upsert(result, email, product.name)
      notifyOutcome(toast, result)
      setOrder(result)
    } catch (err) {
      const apiError = err instanceof ApiError ? err : new ApiError(0, 'unknown', 'Ocurrió un error inesperado.')
      if (!apiError.isOutcomeUnknown) setIdempotencyKey(null)
      setErrors(fromApiErrors(apiError.fieldErrors))
      notifyError(toast, apiError, failureTitle(apiError))
      if (apiError.code === 'insufficient_stock' || apiError.code === 'concurrency_conflict') {
        setStep(1)
        reloadCatalog()
      }
    } finally {
      setCard(emptyCard)
      setSubmitting(false)
    }
  }

  function updateOrder(updated: Order) {
    purchases.upsert(updated, contact.email.trim(), product?.name ?? '')
    setOrder(updated)
  }

  function newPurchase() {
    setOrder(null)
    setErrors({})
    setQuantity(1)
    setInstallments(1)
    setStep(1)
    reloadCatalog()
  }

  const setContactField = (field: keyof ContactForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setContact({ ...contact, [field]: e.target.value })

  const stepState = (n: 1 | 2 | 3) => (step === n ? 'active' : step > n ? 'done' : 'locked')

  return (
    <div className="checkout">
      <div className="checkout__main">
        <h1 className="page-title">Finalizar compra</h1>

        {order ? (
          <PaymentResult order={order} email={contact.email.trim()} onUpdated={updateOrder} onNewPurchase={newPurchase} />
        ) : (
          <form onSubmit={pay} noValidate>
            <Step number={1} title="Producto" state={stepState(1)} onEdit={() => setStep(1)} editDisabled={submitting}
              summary={product && <>{product.name} · Cantidad {quantity}</>}>
              {catalog.state === 'loading' && <ProductListSkeleton />}
              {catalog.state === 'error' && (
                <Alert tone="error" title="No se pudo cargar el catálogo">
                  <p>{catalog.error.message}</p>
                  <Reference id={catalog.error.correlationId} />
                  <Button variant="secondary" type="button" onClick={reloadCatalog}>Reintentar</Button>
                </Alert>
              )}
              {catalog.state === 'ready' && (
                <>
                  <ProductList products={products} selectedId={productId} disabled={submitting}
                    onSelect={(id) => { setProductId(id); setQuantity(1) }} />
                  <SelectField label="Cantidad" className="quantity" value={Math.min(quantity, maxQuantity)} disabled={!product}
                    onChange={(e) => setQuantity(Number(e.target.value))}>
                    {Array.from({ length: maxQuantity }, (_, i) => i + 1).map((n) => <option key={n} value={n}>{n}</option>)}
                  </SelectField>
                  <div className="actions">
                    <Button type="button" disabled={!product} onClick={() => setStep(2)}>Continuar</Button>
                  </div>
                </>
              )}
            </Step>

            <Step number={2} title="Datos de contacto" state={stepState(2)} onEdit={() => setStep(2)} editDisabled={submitting}
              summary={<>{contact.firstName} {contact.lastName} · {contact.email}</>}>
              <div className="grid">
                <Field label="Nombre" autoComplete="given-name" value={contact.firstName} onChange={setContactField('firstName')}
                  error={errors['customer.firstName']} />
                <Field label="Apellido" autoComplete="family-name" value={contact.lastName} onChange={setContactField('lastName')}
                  error={errors['customer.lastName']} />
                <Field label="Correo electrónico" type="email" autoComplete="email" value={contact.email} onChange={setContactField('email')}
                  error={errors['customer.email']} hint="Lo necesitarás para consultar tu compra." />
                <Field label="Teléfono (opcional)" type="tel" autoComplete="tel" value={contact.phone} onChange={setContactField('phone')}
                  error={errors['customer.phone']} />
              </div>
              <div className="actions">
                <Button type="button" onClick={continueFromContact}>Continuar</Button>
              </div>
            </Step>

            <Step number={3} title="Pago con tarjeta" state={stepState(3)}>
              <CardFields cardType={planOptions?.cardTypeDisplay} value={card} onChange={setCard} errors={errors} disabled={submitting} />
              <InstallmentOptions options={planOptions} selected={effectiveInstallments} onSelect={setInstallments} disabled={submitting} />
              <TestCards />
              {submitting && <div className="progress" role="progressbar" aria-label="Procesando pago" />}
              <div className="actions">
                <Button type="submit" loading={submitting} disabled={!product || !planOptions}>
                  {submitting ? 'Procesando pago…' : `Pagar ${formatMoney(amount, product?.currency ?? 'MXN')}`}
                </Button>
              </div>
              {submitting && slow && (
                <p className="muted reveal" role="status">Confirmando con el banco. No cierres esta ventana ni vuelvas a enviar el formulario.</p>
              )}
            </Step>
          </form>
        )}
      </div>

      <OrderSummary product={product} quantity={quantity} planLabel={selectedPlan?.label ?? null} order={order} />
    </div>
  )
}

function failureTitle(error: ApiError) {
  if (error.code === 'insufficient_stock') return 'No hay suficiente inventario'
  if (error.code === 'installments_not_available') return 'Meses sin intereses no disponibles'
  if (error.status === 429) return 'Demasiados intentos'
  if (error.isOutcomeUnknown) return 'No pudimos confirmar tu pago'
  return 'No se pudo completar la compra'
}

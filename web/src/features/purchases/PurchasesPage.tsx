import { useEffect, useState, type FormEvent } from 'react'
import { api, ApiError } from '../../api/client'
import type { Order } from '../../api/types'
import { StatusLine } from '../../components/StatusLine'
import { useToast } from '../../components/toastContext'
import { Alert, Button, Field, Reference } from '../../components/ui'
import { formatDateTime, formatMoney } from '../../lib/format'
import { purchases, usePurchases } from '../../lib/purchases'
import { navigate } from '../../lib/router'
import { OrderDetail } from './OrderDetail'

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

type Lookup = { state: 'loading' } | { state: 'error'; error: ApiError } | { state: 'ready'; order: Order; email: string }

/** Remounted per order id (see App), so every lookup starts clean. */
export function PurchasesPage({ orderId }: { orderId: string | null }) {
  const list = usePurchases()
  const stored = orderId ? purchases.find(orderId) : undefined
  const storedEmail = stored?.email
  const [lookup, setLookup] = useState<Lookup | null>(null)

  // Orders from this device already carry the email, so they load directly.
  useEffect(() => {
    if (!orderId || !storedEmail) return
    let cancelled = false
    api.lookupOrder(orderId, storedEmail).then(
      (order) => {
        if (cancelled) return
        purchases.refresh(order)
        setLookup({ state: 'ready', order, email: storedEmail })
      },
      (err: unknown) => !cancelled && setLookup({ state: 'error', error: toApiError(err) }),
    )
    return () => { cancelled = true }
  }, [orderId, storedEmail])

  const current: Lookup | null = lookup ?? (orderId && storedEmail ? { state: 'loading' } : null)

  return (
    <div className="order-page">
      <h1 className="page-title">Mis compras</h1>

      {current ? (
        <>
          <button type="button" className="link-button back" onClick={() => navigate('/compras')}>← Todas mis compras</button>
          {current.state === 'loading' && <OrderDetailSkeleton />}
          {current.state === 'error' && (
            <Alert tone="error" title={current.error.status === 404 ? 'Orden no encontrada' : 'No se pudo consultar la orden'}>
              <p>{current.error.message}</p>
              <Reference id={current.error.correlationId} />
            </Alert>
          )}
          {current.state === 'ready' && (
            <OrderDetail order={current.order} email={current.email}
              onUpdated={(order) => { purchases.refresh(order); setLookup({ ...current, order }) }} />
          )}
        </>
      ) : (
        <>
          <PurchaseList />
          <LookupForm initialOrderId={orderId ?? ''} onFound={(order, email) => setLookup({ state: 'ready', order, email })} />
          {list.length > 0 && (
            <p className="muted privacy-note">
              Esta lista se guarda solo en este navegador: número de orden, correo y total. Nunca datos de tarjeta.{' '}
              <button type="button" className="link-button" onClick={() => purchases.clear()}>Borrar historial</button>
            </p>
          )}
        </>
      )}
    </div>
  )
}

function PurchaseList() {
  const list = usePurchases()
  if (list.length === 0) {
    return <p className="muted empty">No hay compras hechas desde este navegador.</p>
  }
  return (
    <ul className="purchase-list">
      {list.map((p) => (
        <li key={p.orderId} className="reveal">
          <a href={`/compras/${p.orderId}`} onClick={(e) => { e.preventDefault(); navigate(`/compras/${p.orderId}`) }}>
            <span className="purchase__main">
              <span className="purchase__name">{p.productName}</span>
              <span className="muted">{formatDateTime(p.createdAt)} · <code>{p.orderId.slice(0, 8)}</code></span>
            </span>
            <span className="purchase__side">
              <span className="amount">{formatMoney(p.total, p.currency)}</span>
              <StatusLine status={p.status} label={p.statusDisplay} />
            </span>
          </a>
          <button type="button" className="link-button purchase__remove" aria-label={`Quitar ${p.productName} de este dispositivo`}
            onClick={() => purchases.remove(p.orderId)}>Quitar</button>
        </li>
      ))}
    </ul>
  )
}

function LookupForm({ initialOrderId, onFound }: { initialOrderId: string; onFound: (order: Order, email: string) => void }) {
  const toast = useToast()
  const [orderId, setOrderId] = useState(initialOrderId)
  const [email, setEmail] = useState('')
  const [errors, setErrors] = useState<{ orderId?: string; email?: string }>({})
  const [loading, setLoading] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const id = orderId.trim().toLowerCase()
    const found = {
      orderId: UUID.test(id) ? undefined : 'Copia el número completo de la orden, por ejemplo 01a0e07b-6274-728c-a1ff-53db31beaa5e.',
      email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim()) ? undefined : 'Ingresa el correo con el que compraste.',
    }
    setErrors(found)
    if (found.orderId || found.email) return

    setLoading(true)
    try {
      const order = await api.lookupOrder(id, email.trim())
      onFound(order, email.trim())
    } catch (err) {
      const apiError = toApiError(err)
      toast.notify('error', apiError.status === 404 ? 'Orden no encontrada' : 'No se pudo consultar la orden',
        apiError.status === 404 ? 'Verifica el número de orden y el correo con el que compraste.' : apiError.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <form className="lookup-card" onSubmit={submit} noValidate>
      <h2 className="section__title">Consultar otra compra</h2>
      <p className="muted">Para compras hechas en otro dispositivo, usa el número de orden y el correo de la compra.</p>
      <div className="grid">
        <Field label="Número de orden" name="order-id" value={orderId} onChange={(e) => setOrderId(e.target.value)} error={errors.orderId}
          autoComplete="off" spellCheck={false} />
        <Field label="Correo electrónico" name="email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)}
          error={errors.email} />
      </div>
      <div className="actions">
        <Button type="submit" variant="secondary" loading={loading}>Consultar</Button>
      </div>
    </form>
  )
}

function OrderDetailSkeleton() {
  return (
    <div className="section" aria-busy="true" aria-label="Cargando orden">
      <span className="skeleton skeleton--line" />
      <span className="skeleton skeleton--line skeleton--short" />
      <span className="skeleton skeleton--line" />
    </div>
  )
}

const toApiError = (err: unknown) => (err instanceof ApiError ? err : new ApiError(0, 'unknown', 'Ocurrió un error inesperado.'))

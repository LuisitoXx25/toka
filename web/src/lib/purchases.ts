import { useSyncExternalStore } from 'react'
import type { Order, OrderStatus } from '../api/types'
import { readJson, writeJson } from './storage'

/**
 * Orders placed from this browser. Stores what is needed to list them and look them up
 * (order id + buyer email); never card data. The user can remove entries at any time.
 */
export interface Purchase {
  orderId: string
  email: string
  productName: string
  total: number
  currency: string
  installments: number
  status: OrderStatus
  statusDisplay: string
  createdAt: string
}

const KEY = 'checkout.purchases'
const listeners = new Set<() => void>()
let cache: Purchase[] = readJson<Purchase[]>('local', KEY, [])

function save(next: Purchase[]) {
  cache = next
  writeJson('local', KEY, next.length ? next : null)
  listeners.forEach((listener) => listener())
}

export const purchases = {
  list: () => cache,

  find: (orderId: string) => cache.find((p) => p.orderId === orderId),

  /** Adds or refreshes an order, newest first. */
  upsert(order: Order, email: string, productName: string) {
    const entry: Purchase = {
      orderId: order.id,
      email,
      productName,
      total: order.total,
      currency: order.currency,
      installments: order.installments,
      status: order.status,
      statusDisplay: order.statusDisplay,
      createdAt: order.createdAt,
    }
    save([entry, ...cache.filter((p) => p.orderId !== order.id)].sort((a, b) => b.createdAt.localeCompare(a.createdAt)))
  },

  /** Updates the stored status after a lookup, if the order is in the list. */
  refresh(order: Order) {
    if (!cache.some((p) => p.orderId === order.id)) return
    save(cache.map((p) => (p.orderId === order.id ? { ...p, status: order.status, statusDisplay: order.statusDisplay } : p)))
  },

  remove: (orderId: string) => save(cache.filter((p) => p.orderId !== orderId)),

  clear: () => save([]),
}

export function usePurchases() {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    purchases.list,
  )
}

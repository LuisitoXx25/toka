const moneyFormatters = new Map<string, Intl.NumberFormat>()

/** Amount with currency symbol and code, e.g. "$3,499.00 MXN". */
export function formatMoney(amount: number, currency: string) {
  let formatter = moneyFormatters.get(currency)
  if (!formatter) {
    formatter = new Intl.NumberFormat('es-MX', { style: 'currency', currency, currencyDisplay: 'narrowSymbol' })
    moneyFormatters.set(currency, formatter)
  }
  return `${formatter.format(amount)} ${currency}`
}

/** Amount with symbol only, e.g. "$1,166.33" (used inside sentences that already imply the currency). */
export function formatAmount(amount: number) {
  return amountFormatter.format(amount)
}

const amountFormatter = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', currencyDisplay: 'narrowSymbol' })

const dateTimeFormatter = new Intl.DateTimeFormat('es-MX', { dateStyle: 'medium', timeStyle: 'medium' })

export const formatDateTime = (iso: string) => dateTimeFormatter.format(new Date(iso))

export const formatPercent = (rate: number) => `${Math.round(rate * 100)}%`

/**
 * Same split as the API: regular payments truncated to the cent, the first one absorbs the difference.
 * Used only as a fallback when the API plans are not available.
 */
export function describeInstallments(months: number, total: number) {
  const totalCents = Math.round(total * 100)
  const regular = Math.floor(totalCents / months)
  const first = totalCents - regular * (months - 1)
  if (months === 1) return `1 pago de ${formatAmount(total)}`
  const text = `${months} pagos de ${formatAmount(regular / 100)} sin intereses`
  return first === regular ? text : `${text}; el primero de ${formatAmount(first / 100)}`
}

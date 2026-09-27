export const onlyDigits = (value: string) => value.replace(/\D/g, '')

/** Groups digits in blocks of 4 (Amex: 4-6-5) for display while typing. */
export function formatCardNumber(value: string) {
  const digits = onlyDigits(value).slice(0, 19)
  if (/^3[47]/.test(digits)) {
    return [digits.slice(0, 4), digits.slice(4, 10), digits.slice(10, 15)].filter(Boolean).join(' ')
  }
  return digits.replace(/(\d{4})(?=\d)/g, '$1 ')
}

/** Formats expiry as MM/AA while typing. */
export function formatExpiry(value: string) {
  const digits = onlyDigits(value).slice(0, 4)
  return digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits
}

export function passesLuhn(number: string) {
  if (!/^\d+$/.test(number)) return false
  let sum = 0
  let double = false
  for (let i = number.length - 1; i >= 0; i--) {
    let digit = Number(number[i])
    if (double) {
      digit *= 2
      if (digit > 9) digit -= 9
    }
    sum += digit
    double = !double
  }
  return sum % 10 === 0
}

export function detectBrand(number: string): string | null {
  if (/^4/.test(number)) return 'Visa'
  if (/^3[47]/.test(number)) return 'American Express'
  if (/^5[1-5]/.test(number)) return 'Mastercard'
  if (/^2\d{3}/.test(number)) {
    const prefix = Number(number.slice(0, 4))
    if (prefix >= 2221 && prefix <= 2720) return 'Mastercard'
  }
  return null
}

/** Parses "MM/AA". Returns null when the format is invalid. */
export function parseExpiry(value: string): { month: number; year: number } | null {
  const match = /^(\d{2})\/(\d{2})$/.exec(value)
  if (!match) return null
  const month = Number(match[1])
  if (month < 1 || month > 12) return null
  return { month, year: 2000 + Number(match[2]) }
}

/** A card is valid through the last day of its expiry month. */
export function isExpired(month: number, year: number, now = new Date()) {
  return year < now.getFullYear() || (year === now.getFullYear() && month < now.getMonth() + 1)
}

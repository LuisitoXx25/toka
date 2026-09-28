import { onlyDigits } from './card'

/** Mexican numbers: 10 digits, optionally preceded by the +52 country code. */
const NATIONAL_DIGITS = 10
const COUNTRY_CODE = '52'

/**
 * Keeps only what a phone number can contain (digits and a leading "+") and groups it while typing:
 * "5512345678" → "55 1234 5678", "+525512345678" → "+52 55 1234 5678". Anything else is dropped as it is typed.
 */
export function formatPhone(value: string) {
  const international = value.trimStart().startsWith('+')
  const digits = onlyDigits(value).slice(0, international ? COUNTRY_CODE.length + NATIONAL_DIGITS : NATIONAL_DIGITS)
  if (!international) return groupNational(digits)
  if (digits.length <= COUNTRY_CODE.length) return `+${digits}`
  return `+${digits.slice(0, COUNTRY_CODE.length)} ${groupNational(digits.slice(COUNTRY_CODE.length))}`
}

function groupNational(digits: string) {
  return [digits.slice(0, 2), digits.slice(2, 6), digits.slice(6, 10)].filter(Boolean).join(' ')
}

/** Empty is valid (the field is optional); otherwise 10 digits, or +52 followed by 10 digits. */
export function isValidPhone(value: string) {
  const trimmed = value.trim()
  if (!trimmed) return true
  const digits = onlyDigits(trimmed)
  return trimmed.startsWith('+')
    ? digits.length === COUNTRY_CODE.length + NATIONAL_DIGITS && digits.startsWith(COUNTRY_CODE)
    : digits.length === NATIONAL_DIGITS
}

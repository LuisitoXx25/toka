import type { CardInput } from '../../api/types'
import { onlyDigits, parseExpiry } from '../../lib/card'
import type { CardForm } from '../../lib/validation'

export const emptyCard: CardForm = { number: '', expiry: '', cvv: '', holderName: '' }

/** Converts the validated form into the API payload. */
export function toCardInput(card: CardForm): CardInput {
  const expiry = parseExpiry(card.expiry) ?? { month: 0, year: 0 }
  return {
    holderName: card.holderName.trim(),
    number: onlyDigits(card.number),
    expiryMonth: expiry.month,
    expiryYear: expiry.year,
    cvv: card.cvv,
  }
}

import { describe, expect, it } from 'vitest'
import { detectBrand, formatCardNumber, formatExpiry, isExpired, parseExpiry, passesLuhn } from './card'
import { fromApiErrors, validateCard } from './validation'

describe('card helpers', () => {
  it('validates Luhn', () => {
    expect(passesLuhn('4111111111111111')).toBe(true)
    expect(passesLuhn('4111111111111112')).toBe(false)
  })

  it('formats numbers while typing', () => {
    expect(formatCardNumber('4111a1111 11111111')).toBe('4111 1111 1111 1111')
    expect(formatCardNumber('378282246310005')).toBe('3782 822463 10005')
    expect(formatExpiry('1230')).toBe('12/30')
  })

  it('detects brand', () => {
    expect(detectBrand('5555555555554444')).toBe('Mastercard')
    expect(detectBrand('6011111111111117')).toBeNull()
  })

  it('treats a card as valid through its expiry month', () => {
    const now = new Date(2026, 8, 26)
    expect(isExpired(9, 2026, now)).toBe(false)
    expect(isExpired(8, 2026, now)).toBe(true)
    expect(parseExpiry('13/30')).toBeNull()
  })
})

describe('validation', () => {
  it('reports every invalid card field in Spanish', () => {
    const errors = validateCard({ number: '4111 1111 1111 1112', expiry: '01/20', cvv: '1', holderName: '' }, new Date(2026, 8, 26))
    expect(errors).toEqual({
      'card.number': 'El número de tarjeta no es válido.',
      'card.expiry': 'La tarjeta está vencida.',
      'card.cvv': 'El CVV debe tener 3 o 4 dígitos.',
      'card.holderName': 'Ingresa el nombre como aparece en la tarjeta.',
    })
  })

  it('maps API expiry errors to the expiry field', () => {
    expect(fromApiErrors({ 'card.expiryMonth': ['Mes inválido.'] })).toEqual({ 'card.expiry': 'Mes inválido.' })
  })
})

describe('installment labels', () => {
  it('lets the first payment absorb the rounding, like the API', async () => {
    const { describeInstallments } = await import('./format')
    expect(describeInstallments(3, 100)).toBe('3 pagos de $33.33 sin intereses; el primero de $33.34')
    expect(describeInstallments(6, 3000)).toBe('6 pagos de $500.00 sin intereses')
  })
})

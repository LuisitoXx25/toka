import { isExpired, onlyDigits, parseExpiry, passesLuhn } from './card'

export interface ContactForm {
  firstName: string
  lastName: string
  email: string
  phone: string
}

export interface CardForm {
  number: string
  expiry: string
  cvv: string
  holderName: string
}

export type FieldErrors = Partial<Record<string, string>>

// Client-side checks only improve the experience; the API validates everything again.

export function validateContact(form: ContactForm): FieldErrors {
  const errors: FieldErrors = {}
  if (!form.firstName.trim()) errors['customer.firstName'] = 'Ingresa tu nombre.'
  if (!form.lastName.trim()) errors['customer.lastName'] = 'Ingresa tu apellido.'
  if (!form.email.trim()) errors['customer.email'] = 'Ingresa tu correo electrónico.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) errors['customer.email'] = 'El correo electrónico no es válido.'
  if (form.phone.trim() && !/^\+?[0-9 ()-]{7,20}$/.test(form.phone.trim())) errors['customer.phone'] = 'El teléfono no tiene un formato válido.'
  return errors
}

export function validateCard(form: CardForm, now = new Date()): FieldErrors {
  const errors: FieldErrors = {}
  const number = onlyDigits(form.number)
  if (!number) errors['card.number'] = 'Ingresa el número de tarjeta.'
  else if (number.length < 13 || !passesLuhn(number)) errors['card.number'] = 'El número de tarjeta no es válido.'

  const expiry = parseExpiry(form.expiry)
  if (!form.expiry) errors['card.expiry'] = 'Ingresa la fecha de vencimiento.'
  else if (!expiry) errors['card.expiry'] = 'Usa el formato MM/AA.'
  else if (isExpired(expiry.month, expiry.year, now)) errors['card.expiry'] = 'La tarjeta está vencida.'

  if (!/^\d{3,4}$/.test(form.cvv)) errors['card.cvv'] = 'El CVV debe tener 3 o 4 dígitos.'
  if (!form.holderName.trim()) errors['card.holderName'] = 'Ingresa el nombre como aparece en la tarjeta.'
  return errors
}

/** Maps API field errors (e.g. "card.expiryMonth") to the form field that shows them. */
export function fromApiErrors(errors: Record<string, string[]>): FieldErrors {
  const alias: Record<string, string> = { 'card.expiryMonth': 'card.expiry', 'card.expiryYear': 'card.expiry' }
  const result: FieldErrors = {}
  for (const [key, messages] of Object.entries(errors)) {
    const field = alias[key] ?? key
    result[field] ??= messages[0]
  }
  return result
}

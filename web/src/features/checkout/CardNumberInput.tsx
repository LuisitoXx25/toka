import { useEffect, useLayoutEffect, useRef } from 'react'
import { Field } from '../../components/ui'
import { MAX_CARD_DIGITS, maskCardNumber, onlyDigits } from '../../lib/card'

interface Props {
  /** Digits only. */
  digits: string
  onDigits: (digits: string) => void
  error?: string
  hint?: string
  disabled: boolean
}

/**
 * Card number field that only ever shows the last 4 digits ("•••• •••• •••• 1111").
 * The masked text cannot be edited like normal text, so typing, deleting and pasting are intercepted
 * with `beforeinput` and applied to the digits; the caret always stays at the end.
 */
export function CardNumberInput({ digits, onDigits, error, hint, disabled }: Props) {
  const ref = useRef<HTMLInputElement>(null)
  const latest = useRef({ digits, onDigits })
  useLayoutEffect(() => {
    latest.current = { digits, onDigits }
  })

  useEffect(() => {
    const input = ref.current
    if (!input) return

    function onBeforeInput(e: InputEvent) {
      const { digits: current, onDigits: update } = latest.current
      const el = e.target as HTMLInputElement
      const replacesAll = el.value.length > 0 && el.selectionStart === 0 && el.selectionEnd === el.value.length
      const base = replacesAll ? '' : current

      if (e.inputType.startsWith('insert')) {
        const text = e.data ?? e.dataTransfer?.getData('text') ?? ''
        e.preventDefault()
        update((base + onlyDigits(text)).slice(0, MAX_CARD_DIGITS))
      } else if (e.inputType.startsWith('delete')) {
        e.preventDefault()
        update(replacesAll ? '' : current.slice(0, -1))
      }
    }

    input.addEventListener('beforeinput', onBeforeInput)
    return () => input.removeEventListener('beforeinput', onBeforeInput)
  }, [])

  // Keep the caret at the end after every change, since digits are only added or removed there.
  useLayoutEffect(() => {
    const input = ref.current
    if (input && document.activeElement === input) input.setSelectionRange(input.value.length, input.value.length)
  }, [digits])

  return (
    <Field
      className="span-2"
      inputRef={ref}
      label="Número de tarjeta"
      name="cc-number"
      inputMode="numeric"
      autoComplete="cc-number"
      placeholder="0000 0000 0000 0000"
      value={maskCardNumber(digits)}
      // Browser autofill writes the full number without a beforeinput event.
      onChange={(e) => {
        if (!e.target.value.includes('•')) onDigits(onlyDigits(e.target.value).slice(0, MAX_CARD_DIGITS))
      }}
      error={error}
      hint={hint}
      disabled={disabled}
    />
  )
}

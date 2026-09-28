# Evidencia de pruebas

Generado el 27/09/2026 a partir de las ejecuciones reales (`dotnet test` con TRX y cobertura, Vitest y `scripts/smoke.sh`).

## Toka.UnitTests

**82 de 82 pasaron** · fallidas: 0

<details><summary><b>CardRulesTests</b> (17)</summary>

| Prueba | Resultado |
|---|---|
| `Brand(number: "2", expected: "UNKNOWN")` | ✅ |
| `Brand(number: "2223003122003222", expected: "MASTERCARD")` | ✅ |
| `Brand(number: "378282246310005", expected: "AMEX")` | ✅ |
| `Brand(number: "4111111111111111", expected: "VISA")` | ✅ |
| `Brand(number: "5555555555554444", expected: "MASTERCARD")` | ✅ |
| `Brand(number: "6011111111111117", expected: "UNKNOWN")` | ✅ |
| `Card_ToString_never_exposes_pan_or_cvv` | ✅ |
| `Expiry(month: 1, year: 2027, expired: False)` | ✅ |
| `Expiry(month: 12, year: 2025, expired: True)` | ✅ |
| `Expiry(month: 8, year: 2026, expired: True)` | ✅ |
| `Expiry(month: 9, year: 2026, expired: False)` | ✅ |
| `Luhn(number: "", expected: False)` | ✅ |
| `Luhn(number: "378282246310005", expected: True)` | ✅ |
| `Luhn(number: "4111-1111-1111-1111", expected: False)` | ✅ |
| `Luhn(number: "4111111111111111", expected: True)` | ✅ |
| `Luhn(number: "4111111111111112", expected: False)` | ✅ |
| `Luhn(number: "5555555555554444", expected: True)` | ✅ |

</details>

<details><summary><b>InstallmentPolicyTests</b> (11)</summary>

| Prueba | Resultado |
|---|---|
| `Debit_cards_only_get_a_single_payment` | ✅ |
| `Exact_split_has_a_single_amount_in_the_label` | ✅ |
| `Labels_state_the_first_payment_when_the_split_is_not_exact` | ✅ |
| `Offers_plans_whose_minimum_is_met(amount: 1000, months: [1])` | ✅ |
| `Offers_plans_whose_minimum_is_met(amount: 1500, months: [1, 3])` | ✅ |
| `Offers_plans_whose_minimum_is_met(amount: 3499, months: [1, 3, 6])` | ✅ |
| `Validates_requested_plan(months: 1, amount: 100, cardType: Debit, allowed: True)` | ✅ |
| `Validates_requested_plan(months: 12, amount: 50000, cardType: Credit, allowed: False)` | ✅ |
| `Validates_requested_plan(months: 3, amount: 1500, cardType: Credit, allowed: True)` | ✅ |
| `Validates_requested_plan(months: 3, amount: 1500, cardType: Debit, allowed: False)` | ✅ |
| `Validates_requested_plan(months: 6, amount: 2999.99, cardType: Credit, allowed: False)` | ✅ |

</details>

<details><summary><b>OrderServiceTests</b> (13)</summary>

| Prueba | Resultado |
|---|---|
| `Debit_card_cannot_use_installments` | ✅ |
| `Debit_card_pays_single_payment_and_is_recorded_as_debit` | ✅ |
| `Installment_plan_below_its_minimum_is_rejected_before_reserving_stock` | ✅ |
| `Installment_plan_is_stored_with_the_monthly_payment` | ✅ |
| `Insufficient_stock_is_a_conflict_and_nothing_is_saved` | ✅ |
| `Invalid_command_returns_validation_error_without_side_effects` | ✅ |
| `Lookup_requires_the_buyer_email` | ✅ |
| `Place_registers_customer_creates_order_and_charges` | ✅ |
| `Retry_payment_of_unknown_order_is_not_found` | ✅ |
| `Retry_payment_with_another_email_is_not_found` | ✅ |
| `Returning_customer_is_reused` | ✅ |
| `Same_idempotency_key_returns_original_order_without_charging_again` | ✅ |
| `Unknown_product_is_not_found` | ✅ |

</details>

<details><summary><b>PaymentProcessorTests</b> (6)</summary>

| Prueba | Resultado |
|---|---|
| `Approved_marks_order_paid_and_keeps_stock_reserved` | ✅ |
| `Declined_is_not_retried_and_releases_stock` | ✅ |
| `Every_attempt_is_audited_with_masked_card` | ✅ |
| `Exhausted_retries_end_as_payment_failed` | ✅ |
| `Gateway_exception_is_treated_as_transient` | ✅ |
| `Transient_error_is_retried_until_approved` | ✅ |

</details>

<details><summary><b>ValidatorTests</b> (2)</summary>

| Prueba | Resultado |
|---|---|
| `Reports_every_invalid_field_in_spanish` | ✅ |
| `Valid_command_passes` | ✅ |

</details>

<details><summary><b>InstallmentScheduleTests</b> (6)</summary>

| Prueba | Resultado |
|---|---|
| `First_payment_absorbs_the_rounding(total: 100, months: 3, first: 33.34, regular: 33.33)` | ✅ |
| `First_payment_absorbs_the_rounding(total: 3000, months: 6, first: 500, regular: 500)` | ✅ |
| `First_payment_absorbs_the_rounding(total: 3499, months: 1, first: 3499, regular: 3499)` | ✅ |
| `First_payment_absorbs_the_rounding(total: 3499, months: 3, first: 1166.34, regular: 1166.33)` | ✅ |
| `First_payment_absorbs_the_rounding(total: 3499, months: 6, first: 583.2, regular: 583.16)` | ✅ |
| `Payments_always_add_up_to_the_total` | ✅ |

</details>

<details><summary><b>OrderTests</b> (9)</summary>

| Prueba | Resultado |
|---|---|
| `Attempts_are_numbered_sequentially` | ✅ |
| `Cannot_record_attempt_after_order_is_closed` | ✅ |
| `Installments_must_be_at_least_one` | ✅ |
| `MarkDeclined_releases_stock_and_allows_retry` | ✅ |
| `MarkPaid_sets_authorization` | ✅ |
| `Monthly_payment_splits_the_total` | ✅ |
| `Paid_order_cannot_be_reopened` | ✅ |
| `Place_reserves_stock_and_stores_iva_breakdown` | ✅ |
| `ReopenForPayment_reserves_stock_again` | ✅ |

</details>

<details><summary><b>ProductTests</b> (5)</summary>

| Prueba | Resultado |
|---|---|
| `Release_returns_stock` | ✅ |
| `Reserve_decrements_stock_and_bumps_version` | ✅ |
| `Reserve_more_than_available_fails_and_keeps_stock` | ✅ |
| `Reserve_requires_positive_quantity(quantity: -1)` | ✅ |
| `Reserve_requires_positive_quantity(quantity: 0)` | ✅ |

</details>

<details><summary><b>TaxBreakdownTests</b> (5)</summary>

| Prueba | Resultado |
|---|---|
| `Splits_tax_included_total(total: 0.01, subtotal: 0.01, tax: 0)` | ✅ |
| `Splits_tax_included_total(total: 116, subtotal: 100, tax: 16)` | ✅ |
| `Splits_tax_included_total(total: 28999, subtotal: 24999.14, tax: 3999.86)` | ✅ |
| `Splits_tax_included_total(total: 3499, subtotal: 3016.38, tax: 482.62)` | ✅ |
| `Subtotal_plus_tax_always_equals_total` | ✅ |

</details>

<details><summary><b>SimulatedPaymentGatewayTests</b> (8)</summary>

| Prueba | Resultado |
|---|---|
| `Amount_over_card_limit_is_declined` | ✅ |
| `Approved_payments_get_an_authorization_code` | ✅ |
| `Card_number_drives_the_outcome(card: "4000000000000002", attempt: 1, expected: Declined)` | ✅ |
| `Card_number_drives_the_outcome(card: "4000000000000119", attempt: 3, expected: TransientError)` | ✅ |
| `Card_number_drives_the_outcome(card: "4000000000000259", attempt: 1, expected: TransientError)` | ✅ |
| `Card_number_drives_the_outcome(card: "4000000000000259", attempt: 2, expected: Approved)` | ✅ |
| `Card_number_drives_the_outcome(card: "4111111111111111", attempt: 1, expected: Approved)` | ✅ |
| `Unresponsive_acquirer_is_cut_by_the_timeout` | ✅ |

</details>

## Toka.IntegrationTests

**29 de 29 pasaron** · fallidas: 0

<details><summary><b>CheckoutApiTests</b> (19)</summary>

| Prueba | Resultado |
|---|---|
| `Approved_payment_creates_paid_order_and_decrements_stock` | ✅ |
| `Correlation_id_is_echoed_and_stored_in_the_audit_trail` | ✅ |
| `Debit_card_is_accepted_only_for_a_single_payment` | ✅ |
| `Declined_payment_releases_stock_and_can_be_retried_with_another_card` | ✅ |
| `Gateway_unavailable_ends_as_payment_failed_after_all_attempts(card: "4000000000000119")` | ✅ |
| `Gateway_unavailable_ends_as_payment_failed_after_all_attempts(card: "4000000000000341")` | ✅ |
| `Guest_lookup_needs_order_id_and_matching_email` | ✅ |
| `Installment_plan_below_minimum_is_rejected` | ✅ |
| `Installment_plans_depend_on_the_amount` | ✅ |
| `Invalid_request_returns_field_errors_in_spanish` | ✅ |
| `Malformed_json_is_rejected_without_internal_details` | ✅ |
| `Order_can_be_paid_in_interest_free_installments` | ✅ |
| `Order_status_includes_attempts_and_audit_trail` | ✅ |
| `Paid_order_cannot_be_retried` | ✅ |
| `Quantity_above_stock_is_a_conflict` | ✅ |
| `Retry_payment_requires_the_buyer_email` | ✅ |
| `Same_idempotency_key_returns_the_original_order` | ✅ |
| `Transient_error_is_retried_automatically` | ✅ |
| `Unknown_product_and_order_return_404` | ✅ |

</details>

<details><summary><b>ConcurrencyTests</b> (1)</summary>

| Prueba | Resultado |
|---|---|
| `Concurrent_checkouts_never_oversell_stock` | ✅ |

</details>

<details><summary><b>CustomersAndSecurityApiTests</b> (5)</summary>

| Prueba | Resultado |
|---|---|
| `Catalog_lists_seeded_products_with_security_headers` | ✅ |
| `Customer_can_be_registered_once_per_email` | ✅ |
| `Health_checks_are_public` | ✅ |
| `Requests_without_api_key_are_rejected` | ✅ |
| `Wrong_api_key_is_rejected` | ✅ |

</details>

<details><summary><b>DataProtectionTests</b> (4)</summary>

| Prueba | Resultado |
|---|---|
| `App_role_cannot_change_order_amounts` | ✅ |
| `App_role_cannot_change_the_installment_plan` | ✅ |
| `App_role_cannot_tamper_with_the_audit_trail` | ✅ |
| `Correction_role_needs_ticket_and_reason_and_every_fix_is_logged` | ✅ |

</details>

## Cobertura de código (.NET)

| Suite | Domain | Application | Infrastructure | Api |
|---|---|---|---|---|
| Unitarias | 87.8% | 81.5% | 1.0% | — |
| Integración | 95.0% | 97.3% | 96.5% | 78.7% |

Porcentaje de líneas cubiertas. Las pruebas unitarias no tocan Infrastructure por diseño (usan fakes); la de integración ejercita la API real contra PostgreSQL.

## Frontend (Vitest)

```
Test Files  1 passed (1)
      Tests  10 passed (10)
```

## Prueba de humo de punta a punta (`./scripts/smoke.sh` contra `docker compose`)

```
Catálogo y formas de pago
  ok    GET productos
  ok    3 y 6 MSI con crédito
  ok    débito solo contado
Checkout
  ok    pago aprobado a 3 MSI
  ok    el primer pago absorbe el redondeo
  ok    IVA desglosado
  ok    tarjeta rechazada
  ok    reintento automático tras error temporal
  ok    débito con MSI rechazado
Consulta y reintento
  ok    consulta con el correo del comprador
  ok    consulta con otro correo
  ok    reintento con otra tarjeta
Superficie expuesta por el proxy
  ok    clientes (datos personales) no expuestos
  ok    orden por id sin correo no expuesta
  ok    método no permitido
  ok    CSP presente
  ok    la API key no está en el bundle

Todas las verificaciones pasaron.
```

## Colección Postman (Newman contra la API en Docker)

```
✓  200 con 4 productos
  ✓  Crédito: contado, 3 y 6 MSI
  ✓  El primer pago absorbe el redondeo
  ✓  Débito: solo contado
  ✓  201 y pago aprobado
  ✓  IVA desglosado que cuadra
  ✓  Pagos MSI suman el total
  ✓  Solo últimos 4 dígitos
  ✓  200 con la orden original, sin segundo cobro
  ✓  201 con estado rechazado y reintento permitido
  ✓  La orden queda pagada con dos intentos
↳ Error temporal con reintento automático
  ✓  Se aprueba en el segundo intento
  ✓  422 con mensaje en español
  ✓  400 con errores por campo
  ✓  200 con estado y bitácora
  ✓  404: no revela que la orden existe
  ✓  200
  ✓  201 con Location
  ✓  409 cliente duplicado
  ✓  401 en español
  ✓  CSP, nosniff y sin caché
  ✓  Healthy
│                requests │                18 │                 0 │
│              assertions │                22 │                 0 │
```

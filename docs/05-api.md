# 5. API

REST versionada en `/api/v1`, JSON, errores en formato **RFC 9457** (`application/problem+json`).

- **Swagger:** http://localhost:8080/swagger (solo desde la máquina local; botón *Authorize* con la `API_KEY` de `.env`).
- **Postman:** [`postman/toka-checkout.postman_collection.json`](../postman/toka-checkout.postman_collection.json) + [`postman/local.postman_environment.json`](../postman/local.postman_environment.json). Incluye el flujo completo con pruebas automáticas: aprobado, rechazado, reintento, MSI, débito, consulta e idempotencia.

## Autenticación

Toda ruta de `/api/v1` requiere el encabezado `X-Api-Key`. Desde el navegador, nginx lo agrega del lado del servidor; la llave nunca llega al cliente.

## Endpoints

| Método | Ruta | Uso | Expuesto en la web |
|---|---|---|---|
| GET | `/products` | Catálogo con stock (precios con IVA) | Sí |
| GET | `/products/{id}` | Un producto | No |
| GET | `/installment-plans?amount=&bin=` | Formas de pago para un monto; con BIN resuelve crédito o débito | Sí |
| POST | `/orders` | Checkout: cliente + orden + stock + pago. Encabezado opcional `Idempotency-Key` | Sí |
| POST | `/orders/lookup` | Consulta con `{orderId, email}` | Sí |
| POST | `/orders/{id}/payment-retries` | Reintento de pago con `{email, card}` | Sí |
| GET | `/orders/{id}` | Detalle para uso interno (back office) | No |
| POST | `/customers` | Registro de cliente | No |
| GET | `/customers/{id}` | Cliente por id | No |
| GET | `/health/live`, `/health/ready` | Salud del proceso y de la base de datos (sin API key) | No |

## Ejemplo: checkout a 3 MSI

```http
POST /api/v1/orders
X-Api-Key: …
Idempotency-Key: 6f1c2b9e-…
Content-Type: application/json

{
  "customer": { "firstName": "Ana", "lastName": "López", "email": "ana@correo.mx", "phone": "55 1234 5678" },
  "productId": "0199a0d4-0000-7000-8000-000000000002",
  "quantity": 1,
  "installments": 3,
  "card": { "holderName": "ANA LOPEZ", "number": "4111111111111111", "expiryMonth": 12, "expiryYear": 2030, "cvv": "123" }
}
```

```http
HTTP/1.1 201 Created
Location: /api/v1/orders/01a0e572-…

{
  "id": "01a0e572-…", "status": "Paid", "statusDisplay": "Pago aprobado",
  "subtotal": 3016.38, "taxRate": 0.16, "taxAmount": 482.62, "total": 3499.00, "currency": "MXN",
  "installments": 3, "firstPayment": 1166.34, "monthlyPayment": 1166.33,
  "paymentPlan": "3 pagos de $1,166.33 sin intereses; el primero de $1,166.34",
  "authorizationCode": "AUTH-7B6C8249", "canRetryPayment": false,
  "attempts": [{ "attemptNumber": 1, "outcome": "Approved", "cardBrand": "VISA", "cardLast4": "1111", "cardTypeDisplay": "Crédito", … }],
  "events": [{ "eventType": "order.created", "description": "Orden creada: 1 × Audífonos inalámbricos. Subtotal 3,016.38 + IVA 482.62 = 3,499.00 MXN, en 3 pagos…", "correlationId": "…" }, …]
}
```

## Códigos de respuesta

| HTTP | Cuándo | `code` |
|---|---|---|
| 201 | Orden creada, **cualquiera que sea el resultado del pago** (revisar `status`) | — |
| 200 | Consulta, reintento, o el mismo `Idempotency-Key` repetido (devuelve la orden original) | — |
| 400 | Datos inválidos: errores por campo en `errors` (`customer.email`, `card.expiry`…) | `validation`, `malformed_request` |
| 401 | Falta la API key o es inválida | `unauthorized` |
| 404 | Recurso inexistente, o una orden con correo que no coincide | `not_found` |
| 409 | Sin stock, estado de orden incompatible, conflicto de concurrencia | `insufficient_stock`, `invalid_order_state`, `concurrency_conflict` |
| 422 | Regla de negocio: MSI no disponible para el monto o para débito | `installments_not_available` |
| 429 | Límite de peticiones (encabezado `Retry-After`) | `rate_limited` |
| 500 | Error inesperado, sin detalles internos | `internal_error` |

Todas las respuestas de error tienen la misma forma:

```json
{
  "title": "La operación no se puede completar en el estado actual.",
  "status": 409,
  "detail": "Solo hay 2 unidad(es) disponibles del producto 'Monitor 27\" 4K'.",
  "code": "insufficient_stock",
  "correlationId": "6bcdf0e7d8a54b2ca8b3bf83c0072b42",
  "instance": "/api/v1/orders"
}
```

`title` y `detail` están en español y son aptos para mostrarse al usuario. `code` es estable para programar contra él. `correlationId` localiza la petición en los logs.

## Límites

| Política | Límite por IP | Rutas |
|---|---|---|
| Global | 120 peticiones por minuto | Todas |
| Checkout | 10 por minuto | `POST /orders`, `POST /orders/{id}/payment-retries` |
| Consulta | 20 por minuto | `POST /orders/lookup` |

Configurables en la sección `RateLimiting`.

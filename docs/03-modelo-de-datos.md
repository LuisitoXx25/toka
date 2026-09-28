# 3. Modelo de datos

PostgreSQL 17. Nombres en `snake_case`, montos en `numeric` (nunca `float`), fechas en `timestamptz` (UTC).

## 3.1 Diagrama entidad-relación

```mermaid
erDiagram
    customers ||--o{ orders : realiza
    products ||--o{ orders : "se vende en"
    orders ||--o{ payment_attempts : registra
    orders ||--o{ audit_events : "entity_id (lógico)"
    orders ||--o{ data_corrections : "row_id (lógico)"

    customers {
        uuid id PK
        varchar first_name
        varchar last_name
        varchar email UK "normalizado en minúsculas"
        varchar phone
        timestamptz created_at
    }
    products {
        uuid id PK
        varchar sku UK
        varchar name
        numeric price "12,2 · IVA incluido"
        char currency
        int stock "CHECK >= 0"
        int version "concurrencia optimista"
    }
    orders {
        uuid id PK
        uuid customer_id FK
        uuid product_id FK
        int quantity
        numeric unit_price
        numeric subtotal
        numeric tax_rate "5,4"
        numeric tax_amount
        numeric total "CHECK subtotal + tax_amount = total"
        char currency
        int installments "CHECK >= 1"
        varchar status
        varchar idempotency_key UK "único si no es nulo"
        varchar authorization_code
        varchar failure_reason
        timestamptz created_at
        timestamptz updated_at
    }
    payment_attempts {
        uuid id PK
        uuid order_id FK
        int attempt_number "único por orden"
        varchar outcome
        varchar response_code
        varchar authorization_code
        char card_last4 "nunca el número completo"
        varchar card_brand
        varchar card_type "Credit / Debit"
        timestamptz occurred_at
    }
    audit_events {
        uuid id PK
        timestamptz occurred_at
        varchar event_type "p. ej. order.paid"
        varchar description "en español"
        varchar entity_type
        uuid entity_id
        varchar correlation_id
        jsonb data "sin datos de tarjeta"
    }
    data_corrections {
        bigint id PK
        timestamptz occurred_at
        text db_user
        text table_name
        text operation
        uuid row_id
        text ticket
        text reason
        jsonb old_data
        jsonb new_data
    }
```

## 3.2 Qué se guarda de una tarjeta

| Dato | ¿Se guarda? | Dónde |
|---|---|---|
| Número completo (PAN) | **No** | Solo en memoria durante la petición |
| CVV | **No** | Solo en memoria durante la petición |
| Titular y vencimiento | **No** | Solo en memoria durante la petición |
| Últimos 4 dígitos, marca, crédito o débito | Sí | `payment_attempts` |

## 3.3 Restricciones de integridad

| Restricción | Protege contra |
|---|---|
| `ck_products_stock_non_negative` | Vender más de lo disponible, aunque falle la lógica de la aplicación |
| `products.version` como token de concurrencia | Dos compras simultáneas que descuentan el mismo stock |
| `ck_orders_tax_breakdown` (`subtotal + tax_amount = total`) | Montos fiscales que no cuadran |
| `ck_orders_installments_positive` | Planes de pago inválidos |
| Índice único filtrado en `orders.idempotency_key` | Dos órdenes por la misma solicitud, incluso con peticiones concurrentes |
| Único `(order_id, attempt_number)` | Intentos de pago duplicados o reordenados |
| Único `customers.email` y `products.sku` | Duplicados |

## 3.4 Protección de datos de evidencia

La migración `DataProtection` agrega triggers que aplican **aunque se acceda a la base de datos por fuera de la API**:

| Tabla | Regla |
|---|---|
| `orders` | La API puede actualizar el estado y los datos de pago. **Montos, plan de pagos, cliente, producto, fecha e idempotency key son inmutables.** No se permite borrar. |
| `payment_attempts`, `audit_events` | Solo inserción |
| `data_corrections` | Solo inserción, sin excepción |
| Todas las anteriores | `TRUNCATE` bloqueado |

La única forma de modificarlas es la corrección de emergencia: rol `toka_corrections` + ticket + motivo, con registro automático del valor anterior y el nuevo. Procedimiento en el [runbook](runbooks/correccion-de-datos.md).

## 3.5 Roles de base de datos

| Rol | Puede | No puede |
|---|---|---|
| `toka_owner` | Ser dueño del esquema y correr migraciones | — (sus credenciales no las usa la API) |
| `toka_app` (API) | Leer, insertar y actualizar clientes, productos y órdenes; insertar intentos y bitácora | Borrar, cambiar el esquema, alterar datos protegidos, leer `data_corrections` |
| `toka_corrections` | Corregir datos protegidos declarando ticket y motivo | Iniciar sesión (se otorga temporalmente a un DBA con nombre) |

Los roles se crean en [`deploy/postgres/init/01-roles.sh`](../deploy/postgres/init/01-roles.sh) al inicializar el volumen de Docker.

## 3.6 Migraciones

En `src/Toka.Infrastructure/Persistence/Migrations`; se aplican al arrancar la API con el rol dueño:

| Migración | Cambio |
|---|---|
| `InitialCreate` | Esquema base y catálogo de ejemplo (4 productos) |
| `TaxBreakdown` | Subtotal, tasa e IVA; recalcula órdenes previas; CHECK fiscal |
| `DataProtection` | Roles, permisos, `data_corrections`, triggers de protección y corrección de emergencia |
| `Installments` | Plan de pagos (MSI) y su inclusión en las columnas protegidas |
| `PaymentAttemptCardType` | Tipo de tarjeta (crédito o débito) en cada intento |

Todas incluyen `Down` para revertir. Para generar el script SQL completo:

```bash
dotnet ef migrations script -p src/Toka.Infrastructure -s src/Toka.Infrastructure -o schema.sql
```

# Runbook: corrección de datos de emergencia

## Qué está protegido

| Tabla | Protección |
|---|---|
| `orders` | No se pueden borrar. Los montos (subtotal, IVA, total, precio, cantidad) y los datos de origen (cliente, producto, fecha, idempotency key) no se pueden cambiar. El estado y los datos de pago sí los actualiza la API. |
| `payment_attempts` | Solo inserción. |
| `audit_events` | Solo inserción (bitácora). |
| `data_corrections` | Solo inserción, sin excepción. Es el registro de las correcciones. |

`TRUNCATE` está bloqueado en todas.

## Roles

| Rol | Uso |
|---|---|
| `toka_owner` | Dueño del esquema. Solo corre migraciones; sus credenciales no las usa la API. |
| `toka_app` | Usuario de la API. Lee, inserta y actualiza; no puede borrar, cambiar el esquema ni hacer correcciones. |
| `toka_corrections` | Sin login. Se otorga temporalmente a un DBA con nombre durante un incidente. |

## Procedimiento

1. Abrir un ticket del incidente con la evidencia del dato incorrecto y la aprobación del responsable.
2. Un administrador otorga el rol al DBA asignado:
   ```sql
   GRANT toka_corrections TO dba_nombre;
   ```
3. El DBA aplica la corrección en una transacción, declarando ticket y motivo (mínimo 10 caracteres):
   ```sql
   BEGIN;
   SET LOCAL toka.correction_ticket = 'INC-1234';
   SET LOCAL toka.correction_reason = 'Cantidad mal capturada por bug en v1.2';
   UPDATE orders SET quantity = 2, unit_price = 1749.50 WHERE id = '...';
   COMMIT;
   ```
   Si falta el rol, el ticket o el motivo, PostgreSQL rechaza el cambio.
4. Verificar el registro de la corrección:
   ```sql
   SELECT occurred_at, db_user, table_name, operation, row_id, ticket, reason, old_data, new_data
   FROM data_corrections WHERE ticket = 'INC-1234';
   ```
5. Retirar el rol al terminar:
   ```sql
   REVOKE toka_corrections FROM dba_nombre;
   ```

## Consideraciones

- Los montos de una orden deben seguir cumpliendo `subtotal + tax_amount = total` (CHECK en la base de datos).
- Corregir una orden no ajusta el inventario. `toka_corrections` solo tiene permisos sobre las tablas protegidas; un ajuste de `products` se hace por el flujo normal de la aplicación.
- Un superusuario de PostgreSQL o `toka_owner` pueden desactivar triggers. Por eso esas credenciales deben estar restringidas y fuera de la operación diaria.

# 8. Limitaciones y evolución

## 8.1 Limitaciones de esta demo

Decisiones conscientes de alcance, no descuidos. Cada una indica su riesgo y cómo se resuelve.

| Limitación | Consecuencia | Resolución |
|---|---|---|
| **Adquirente simulado** con escenarios deterministas por número de tarjeta | No hay autorizaciones reales, 3-D Secure ni conciliación | Implementar `IPaymentGateway` y `ICardBinLookup` contra el adquirente real, sin tocar los casos de uso |
| **El backend recibe el número de tarjeta** (necesario para el simulador) | Alcance PCI mayor al necesario | Tokenización en el navegador con el SDK del adquirente; el backend recibe solo un token |
| **Sin cuentas de cliente**: consulta con orden + correo y listado por dispositivo | "Mis compras" no se sincroniza entre dispositivos; orden + correo no es autenticación fuerte | Login sin contraseña (código por correo) e historial en el servidor |
| **Un producto por orden** | Sin carrito | Entidad `OrderLine`; el IVA y los MSI ya se calculan sobre el total |
| **Solo español, solo MXN, solo IVA general de 16%** | No sirve para otros mercados ni para la zona fronteriza (8%) | Ver 8.3 |
| **Rate limiting en memoria** | Con varias instancias, cada una cuenta por separado | Redis o el rate limiting del API gateway |
| **Pago síncrono** | Una autorización lenta ocupa la petición hasta 20 s en el peor caso | Pago asíncrono con outbox y webhooks del adquirente (8.2) |
| **`toka_corrections` solo cubre órdenes, intentos y bitácora** | Correcciones de stock o de clientes no tienen vía de emergencia auditada | Extender el trigger de registro a `products` y `customers` |
| **Teléfono:** el frontend exige formato mexicano; la API acepta formato internacional | Otra integración podría registrar números no mexicanos | Unificar la regla cuando se defina el alcance internacional |
| **Pruebas de integración requieren Docker** | Sin Docker solo corren las unitarias | Intencional: probar contra PostgreSQL real valida triggers, roles y concurrencia, que un proveedor en memoria no reproduce |
| **Pipeline sin ejecutar en GitHub** | El repositorio aún no tiene remoto; los workflows se validaron localmente con las mismas herramientas | Publicar el repositorio y activar las protecciones de rama que exigen el pipeline en verde |

## 8.2 Hoja de ruta priorizada por riesgo

**Prioridad 1 — seguridad y cumplimiento (antes de manejar dinero real)**
1. Tokenización de tarjetas en el navegador y adquirente real con 3-D Secure.
2. TLS en el borde, HSTS y gestor de secretos con rotación.
3. Despliegue continuo por ambientes con aprobación manual a producción, y firma de imágenes (el pipeline de CI ya existe).
4. Antifraude básico: CAPTCHA adaptativo en checkout, límites por tarjeta y por correo además de por IP.

**Prioridad 2 — confiabilidad y operación**
1. Pagos asíncronos: la orden se confirma al recibir el webhook del adquirente, con patrón outbox para no perder eventos.
2. Métricas y alertas con OpenTelemetry (aprobación, rechazo, latencia del adquirente, 429).
3. Rate limiting distribuido.
4. Respaldos con PITR y migraciones como paso separado del arranque.
5. Vía de emergencia auditada también para `products` y `customers`.

**Prioridad 3 — producto**
1. Cuentas de cliente con historial de compras.
2. Carrito con varios productos.
3. Facturación electrónica (CFDI 4.0): RFC, régimen fiscal y uso de CFDI; timbrado con un PAC.
4. Multi-idioma y multi-moneda (8.3).
5. Back office para soporte: búsqueda de órdenes, bitácora y reembolsos.

## 8.3 Deseables: multi-idioma y multi-moneda

La base ya separa lo que cambia por idioma o por mercado, así que ninguno de los dos requiere rediseño:

**Multi-idioma**
- Hoy los códigos técnicos (`insufficient_stock`, `order.paid`, `PaymentDeclined`) ya son estables e independientes del idioma. El cliente puede traducir por código sin depender del texto.
- Siguiente paso en backend: mover los mensajes de validación, dominio y bitácora a recursos `.resx` y elegir la cultura con `Accept-Language` (`RequestLocalizationMiddleware`). FluentValidation ya soporta cultura por petición.
- La bitácora guardaría el código del evento con sus parámetros y traduciría al consultarla, no al escribirla.
- Frontend: extraer los textos a catálogos (`react-intl` o `i18next`) y formatear con `Intl` según el idioma; montos y fechas ya usan `Intl`.

**Multi-moneda**
- `Product.Currency` y `Order.Currency` ya existen y cada orden guarda su moneda. Los montos son `decimal`, sin errores de punto flotante.
- Falta:
  - un objeto de valor `Money` que impida sumar monedas distintas;
  - el redondeo según las unidades mínimas de cada moneda (el JPY no tiene centavos);
  - reglas fiscales y de MSI por país detrás de un puerto (`ITaxPolicy`, `IInstallmentPolicy` por mercado);
  - tipos de cambio con fecha de cotización guardada en la orden, como se hace hoy con la tasa de IVA.

## 8.4 Qué mantiene al sistema en estado de mejora continua

- **Puertos y adaptadores:** cambiar el adquirente, el BIN lookup o la persistencia no toca las reglas de negocio.
- **Reglas configurables:** planes MSI, límites, reintentos y timeouts se ajustan sin recompilar.
- **Migraciones versionadas con `Down`:** cada cambio de esquema es reversible y queda en el historial.
- **Pruebas como red de seguridad:** 121 pruebas y un script E2E detectan regresiones funcionales, de seguridad y de protección de datos.
- **Evidencia de auditoría:** cualquier cambio de datos protegidos deja rastro (quién, cuándo, por qué, antes y después).

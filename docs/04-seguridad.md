# 4. Seguridad

Criterio rector: **priorizar por riesgo**. Primero se protege lo que puede causar daño económico, legal o reputacional (datos de tarjeta, cobros duplicados, alteración de montos y de la evidencia); después, lo demás.

## 4.1 Riesgos priorizados

| # | Riesgo | Impacto | Prob. | Controles implementados | Riesgo residual |
|---|---|---|---|---|---|
| R1 | Exposición de datos de tarjeta | Crítico | Media | El PAN, el CVV y el titular nunca se persisten ni se registran en logs; solo se guardan los últimos 4 dígitos. `CardDetails.ToString()` enmascarado. Número enmascarado en pantalla y CVV oculto. | En producción el PAN no debería tocar este backend: se usaría tokenización del adquirente (ver 4.5). |
| R2 | Cobro duplicado | Alto | Alta | `Idempotency-Key` con índice único; el frontend reutiliza la llave si el resultado es incierto; botón deshabilitado mientras se procesa. | Bajo |
| R3 | Alteración de montos o de la bitácora (bug, error humano o acceso indebido) | Alto | Media | Invariantes de dominio; CHECK fiscal; triggers que bloquean cambios; roles de mínimo privilegio; corrección de emergencia con ticket y registro del antes y el después. | Un superusuario puede desactivar triggers: sus credenciales deben estar restringidas. |
| R4 | Sobreventa por concurrencia | Medio | Media | Concurrencia optimista (`version`), CHECK `stock >= 0`, prueba con 8 compras simultáneas. | Bajo |
| R5 | Card testing (probar tarjetas robadas) | Alto | Media | Rate limit de 10 checkouts por minuto por IP (antes de validar la API key), validación Luhn, sin detalle del emisor al atacante. | Faltan CAPTCHA o antifraude (ver hoja de ruta). |
| R6 | Acceso a órdenes ajenas (IDOR) | Alto | Media | La consulta y el reintento exigen el correo del comprador; un correo incorrecto responde igual que una orden inexistente; límite de 20 consultas por minuto; las rutas por id no se exponen en la web. | Correo + número de orden no equivale a autenticación fuerte (ver hoja de ruta). |
| R7 | Filtración de la API key | Alto | Media | La llave solo existe en el servidor (nginx la inyecta); verificada ausente del bundle; `.env` fuera de git con permisos 600. | Rotación manual. |
| R8 | Fuga de información técnica en errores | Medio | Alta | ProblemDetails genérico para 500 (sin stack ni SQL); JSON mal formado responde sin detalles del framework; nginx sin versión. | Bajo |

## 4.2 OWASP Top 10 (2021)

| Categoría | Cómo se mitiga | Dónde verlo |
|---|---|---|
| **A01 Control de acceso roto** | Autorización por defecto: toda ruta exige API key salvo las marcadas como públicas (salud). Allowlist de 5 rutas y métodos en nginx. Consulta de órdenes ligada al correo del comprador. Roles de BD de mínimo privilegio. | `Program.cs` (fallback policy), `web/nginx/default.conf.template`, `OrderService.FindOwnedOrderAsync` |
| **A02 Fallas criptográficas** | El PAN y el CVV no se guardan. Secretos aleatorios de 192 bits. Comparación de la API key en tiempo constante. TLS en el borde en producción (ver [operación](07-operacion.md)). | `ApiKeyAuthenticationHandler`, `scripts/init-env.sh` |
| **A03 Inyección** | EF Core con consultas parametrizadas; ningún SQL concatenado. Validación estricta de entrada. CorrelationId validado con regex antes de llegar a los logs (evita inyección en logs). | Repositorios, `CorrelationIdMiddleware` |
| **A04 Diseño inseguro** | Modelo de amenazas (4.1). Idempotencia, estados explícitos, invariantes de dominio, rate limiting de checkout y consultas. | `Order`, `RateLimitPolicies` |
| **A05 Configuración incorrecta** | Contenedores sin root, con sistema de archivos de solo lectura y sin capabilities. Cabeceras de seguridad y CSP estricta. Swagger solo en localhost. BD sin puerto publicado. Sin secretos en el repositorio. | `docker-compose.yml`, `SecurityHeadersMiddleware`, `security-headers.conf` |
| **A06 Componentes vulnerables** | `TreatWarningsAsErrors` hace fallar el build ante paquetes NuGet vulnerables (así se detectó y reemplazó `Microsoft.OpenApi 2.0.0`). En el pipeline: SCA de NuGet y npm, Trivy sobre las imágenes (así se detectaron y corrigieron 38 vulnerabilidades altas en la imagen de nginx) y Dependabot semanal. | `Directory.Build.props`, `.github/workflows/ci.yml` |
| **A07 Fallas de autenticación** | API key comparada en tiempo constante; intentos limitados por IP antes de autenticarse; mensajes de error genéricos. | `ApiKeyAuthenticationHandler`, orden del pipeline en `Program.cs` |
| **A08 Integridad de datos y software** | Bitácora de solo inserción; montos inmutables; `data_corrections` sin excepción; migraciones versionadas en git; `npm ci` con lockfile. | Migración `DataProtection` |
| **A09 Fallas de registro y monitoreo** | Logs JSON estructurados con CorrelationId de punta a punta; bitácora de negocio consultable por orden; warning ante correos que no coinciden (posible enumeración); nunca se registran PAN, CVV ni correos. | `PaymentProcessor`, `OrderService` |
| **A10 SSRF** | La API no hace peticiones a URLs provistas por el usuario; el destino del adquirente viene de configuración. | — |

## 4.3 Manejo de secretos

| Secreto | Origen | Quién lo ve |
|---|---|---|
| Contraseñas de PostgreSQL (superusuario, dueño, app) | `.env` generado por `scripts/init-env.sh` | Contenedores `db` y `api` |
| API key | `.env` | nginx (se la inyecta a la API) y la API |
| Cadenas de conexión | Variables de entorno `ConnectionStrings__*` | Solo la API |

- En `appsettings.json` las cadenas de conexión y llaves están vacías; si falta una, la API no arranca.
- `docker compose` falla con *"Falta .env. Ejecuta ./scripts/init-env.sh"* antes de levantar nada sin secretos.
- En producción se reemplaza `.env` por un gestor de secretos (Vault, AWS Secrets Manager, Azure Key Vault) sin cambiar código: todo se lee de configuración.

## 4.4 Datos personales (LFPDPPP)

- Se recaba lo mínimo: nombre, correo y teléfono opcional.
- Los datos de clientes no son accesibles desde la web pública.
- No se registran correos en logs ni en la bitácora de eventos.
- El listado "Mis compras" vive solo en el navegador del usuario (número de orden, correo, total), con un aviso visible y un botón para borrarlo.

## 4.5 Alcance PCI DSS

Esta demo recibe el PAN en el backend para enviarlo al simulador. **En producción no debe hacerse así.** Lo correcto es tokenizar en el navegador con los campos alojados o el SDK del adquirente, de modo que el backend solo reciba un token. Eso reduce el alcance a SAQ A y elimina el riesgo R1 por diseño. Está en la hoja de ruta como prioridad 1.

## 4.6 Secure SDLC aplicado

1. **Diseño:** requisitos de seguridad y riesgos definidos antes de codificar (idempotencia, inmutabilidad, mínimo privilegio).
2. **Desarrollo:** warnings como errores, nullable estricto, validación en el backend, sin secretos en el código.
3. **Verificación:** pruebas de seguridad automatizadas (autorización, IDOR, inmutabilidad en BD, cabeceras), pruebas de humo contra el stack real y pipeline DevSecOps (SCA, SAST, secretos, escaneo de imágenes) en cada cambio.
4. **Revisión:** cada commit revisado y aprobado por el ingeniero responsable antes de integrarse.
5. **Operación:** runbook de corrección de emergencia, logs estructurados, health checks.

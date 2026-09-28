# 9. Registro del uso de IA

## Herramientas

| Herramienta | Uso |
|---|---|
| **Claude Code** (modelo Claude Opus 5.5), en la terminal sobre este repositorio | **Backend:** revisión de código exhaustiva sobre lo escrito por el desarrollador (optimización de procesos y mejoras de sintaxis). **Frontend:** generación de código a partir de las indicaciones, el formato y el estilo definidos por el desarrollador, con su validación posterior |

## Cómo se trabajó

El desarrollador diseñó y construyó el sistema; la IA fue una herramienta de apoyo con un rol distinto según la capa.

**Backend (.NET) e infraestructura: autoría del desarrollador.**
- La estructura de la solución, la Clean Architecture, los patrones de diseño, la lógica de dominio, los casos de uso, el modelo de datos y las decisiones de arquitectura las escribió y decidió el desarrollador.
- Docker, Docker Compose, nginx, el esquema de secretos y el pipeline de GitHub Actions también son trabajo del desarrollador.
- La IA se usó como revisor: analizó el código ya escrito para proponer optimizaciones y mejoras de sintaxis. El desarrollador evaluó cada sugerencia y decidió cuáles aplicar.

**Frontend (React + TypeScript): el desarrollador dirigió y validó.**
- El desarrollador definió el formato, el estilo, las referencias visuales y las prohibiciones de diseño, y pidió los cambios en cada ronda.
- La IA generó el código de la interfaz siguiendo esas indicaciones.
- El desarrollador revisó el resultado con base en los fundamentos del desarrollo frontend, en especial el manejo de `state` y `useEffect`, y validó el comportamiento en el navegador.

**Reglas de trabajo con la IA:**
- Plan antes de cambios, y aprobación explícita del desarrollador antes de aplicar nada.
- Un commit por paso; ningún commit entró sin su aprobación.
- Estándares de código: nombres en inglés y todo lo que ve el usuario en español; comentarios útiles para mantenimiento, sin relleno.
- Alcance del trabajo: no salir de la carpeta del proyecto.

---

## Decisiones de negocio y de arquitectura del desarrollador

| # | Decisión | Impacto |
|---|---|---|
| 1 | Stack: .NET 10 con Clean Architecture, PostgreSQL + EF Core, React + TypeScript + Vite, Docker Compose, xUnit | Base técnica del proyecto |
| 2 | Criterio de terminado: `docker compose up` levanta todo y `dotnet test` pasa | Definió la verificación final |
| 3 | Todo lo que ve el usuario en español; el código en inglés | Mensajes de dominio, validaciones, API, bitácora e interfaz |
| 4 | Códigos de error técnicos estables en inglés, con el mensaje en español | Contrato de la API integrable |
| 5 | Un pago rechazado no es un error HTTP: la orden se crea (201) con su estado | Diseño de la API de checkout |
| 6 | **Regularización fiscal:** desglose de IVA 16%, precios con IVA incluido, montos inmutables y bitácora como evidencia | Modelo fiscal de la orden |
| 7 | **Protección de datos con salida de emergencia:** en producción hay bugs que corregir, así que se diseñó una vía con permisos y auditada en lugar de un bloqueo absoluto | Roles, triggers, `data_corrections` y runbook |
| 8 | Idempotencia en el checkout | Cero cobros duplicados |
| 9 | **Meses sin intereses:** 3 y 6 MSI, sin reglas por banco, etiqueta "N pagos de $X" | Plan de pagos |
| 10 | **Redondeo de MSI:** la suma de pagos debe dar el total exacto y el primer pago absorbe la diferencia | Plan de pagos exacto |
| 11 | **Tarjetas de débito:** se aceptan solo de contado | Reglas de pago |
| 12 | **Consulta de órdenes** con número de orden + correo, y listado de compras del dispositivo | Evita acceso a órdenes ajenas |
| 13 | **Diseño del frontend:** referencias reales, paleta y tipografía de referencia sin usar la marca, tono sobrio y financiero | Identidad visual |
| 14 | **Prohibiciones de diseño:** Inter, degradados morados, emojis como íconos, tarjetas idénticas con sombra, efecto vidrio, textos de marketing inventados | Evitó una interfaz genérica |
| 15 | **Estados reales obligatorios:** carga, error, tarjeta rechazada, reintentando | Manejo de estados en la UI |
| 16 | **Consumo seguro de la API:** nada de llaves ni URLs internas en el navegador | Proxy nginx con allowlist y la llave del lado del servidor |
| 17 | Checkout por secciones progresivas con animaciones, avisos dentro de la app (no Web Push), estado persistente | Experiencia de compra |
| 18 | Estado de la orden como línea formal con ícono; avisos en barra oscura arriba a la derecha | Estados y notificaciones |
| 19 | **Protección de datos de tarjeta en pantalla:** número enmascarado mientras se escribe y CVV oculto | Menos exposición visual de la tarjeta |
| 20 | "Limpiar datos", salida tras un pago rechazado, validación estricta del teléfono, alineación de campos | Usabilidad |
| 21 | Secretos generados por script; ninguno en el repositorio | Esquema de secretos de Docker |
| 22 | **Pipeline DevSecOps completo** en GitHub Actions: pruebas, SCA, SAST, secretos, escaneo de imágenes y E2E | Seguridad continua |
| 23 | Documentación en orden lógico, con limitaciones honestas y deseables (multi-idioma, multi-moneda), centrada en seguridad y riesgos | Esta documentación |

---

## Registro por fase

### Fase 1 · Planeación y scaffold

| | |
|---|---|
| **Autoría** | Desarrollador: lectura del enunciado, plan, estructura de la solución (4 proyectos + 2 de pruebas) y configuración base del compilador |
| **Aporte de la IA** | Revisión de la configuración del proyecto |
| **Descartado** | `Microsoft.AspNetCore.OpenApi`, por arrastrar un paquete vulnerable (ver hallazgos) |
| **Verificación** | Build con warnings como errores |

### Fase 2 · Dominio

| | |
|---|---|
| **Autoría** | Desarrollador: entidades `Customer`, `Product`, `Order` (máquina de estados), `PaymentAttempt`, `AuditEvent`; invariantes y reserva y liberación de stock; mensajes de dominio en español |
| **Aporte de la IA** | Revisión de código: optimización de procesos y mejoras de sintaxis, aplicadas solo cuando el desarrollador las aprobó |
| **Descartado** | — |
| **Verificación** | Revisión del código por el desarrollador; build |

### Fase 3 · Casos de uso

| | |
|---|---|
| **Autoría** | Desarrollador: `OrderService`, `PaymentProcessor` (reintentos con backoff), `CustomerService`, validadores, puertos, `Result<T>`; decisión inmediata sobre el pago rechazado |
| **Aporte de la IA** | Revisión de código sobre lo implementado |
| **Descartado** | MediatR (indirección innecesaria para seis casos de uso). Los reintentos se resolvieron en la capa de aplicación, en lugar de Polly, para registrar cada intento |
| **Verificación** | Build; revisión del diff por el desarrollador |

### Fase 4 · Infraestructura, fiscal y protección de datos

| | |
|---|---|
| **Autoría** | Desarrollador: EF Core + PostgreSQL, migraciones, simulador del adquirente, timeout con Polly, bitácora, desglose de IVA, triggers y roles. Diseñó la corrección de emergencia con roles, ticket, motivo y registro del antes y el después |
| **Aporte de la IA** | Revisión de código y de las migraciones |
| **Descartado** | Bloqueo absoluto sin vía de corrección |
| **Verificación** | Migraciones aplicadas y revertidas en PostgreSQL real. Escenarios SQL por rol: la app no puede alterar montos; un DBA sin ticket es rechazado; con ticket, la corrección queda registrada. |

### Fase 5 · API REST

| | |
|---|---|
| **Autoría** | Desarrollador: controllers `/api/v1`, API key con comparación en tiempo constante, ProblemDetails, correlación, cabeceras de seguridad, rate limiting, Swagger, logs estructurados; errores del framework traducidos al español |
| **Aporte de la IA** | Revisión de código y sugerencias de mejora |
| **Descartado** | — |
| **Verificación** | Prueba de punta a punta con curl contra PostgreSQL y los roles reales (aprobado, rechazo, reintento, timeout, validación, 404, cabeceras); revisión de logs: cero datos de tarjeta o correos |

### Fase 6 · Pruebas

| | |
|---|---|
| **Autoría** | Desarrollador: pruebas unitarias con fakes escritos a mano; pruebas de integración con Testcontainers conectando con el rol restringido de producción |
| **Aporte de la IA** | Revisión de las pruebas |
| **Descartado** | Moq y FluentAssertions (licencias). Fallback a SQLite (no valida triggers, roles ni concurrencia). |
| **Verificación** | Tres corridas consecutivas para descartar pruebas inestables. Una prueba de integración detectó un error en el tipo de contenido de las respuestas (ver hallazgos) |

### Fase 7 · Frontend

| | |
|---|---|
| **Rol del desarrollador** | Dirigió y validó. Definió el formato, el estilo y las referencias; revisó el manejo de `state` y `useEffect`; probó la aplicación en el navegador en cada ronda |
| **Generó la IA** | SPA React + TypeScript (checkout, resultado de pago, consulta de órdenes, validación y estilos), siguiendo las indicaciones del desarrollador |
| **Rondas de revisión** | **Tres rondas** del desarrollador transformaron la interfaz: <br>1. Estado persistente, MSI, avisos, consulta segura, secciones progresivas, listado de compras.<br>2. Redondeo exacto, débito, estilo formal de estados y avisos arriba a la derecha.<br>3. Tarjeta enmascarada, CVV oculto, "Limpiar datos", salida tras rechazo, autocompletado corregido, teléfono validado, campos alineados (a partir de una captura de pantalla). |
| **Descartado** | Web Push; pasos de progreso simulados (serían inventados); nombre y logotipo de la marca de referencia; consulta de órdenes solo por id |
| **Verificación** | TypeScript estricto, linter y Vitest. **La revisión visual fue del desarrollador** |

### Fase 8 · Docker Compose

| | |
|---|---|
| **Autoría** | Desarrollador: Dockerfiles multi-stage, nginx con allowlist y CSP, compose con health checks y hardening, script de secretos (valores aleatorios en lugar de credenciales fijas) |
| **Aporte de la IA** | Revisión de las configuraciones |
| **Descartado** | Credenciales de desarrollo en el repositorio |
| **Verificación** | Allowlist, métodos bloqueados, llave ausente del bundle, cabeceras, usuarios sin root, persistencia tras reinicio |

### Fase 9 · Pipeline y documentación

| | |
|---|---|
| **Autoría** | Desarrollador: workflows de GitHub Actions, CodeQL, Dependabot, scripts de SCA y E2E, colección Postman, documentación y evidencia. Definió el enfoque de la documentación y la forma de este registro |
| **Aporte de la IA** | Revisión de los workflows y de la documentación |
| **Descartado** | Afirmar "listo para producción" sin matices; se documentó qué cumple y qué falta |
| **Verificación** | actionlint, gitleaks, SCA, Trivy y Newman (22/22) ejecutados localmente con las mismas herramientas del pipeline |

---

## Hallazgos durante el desarrollo y su corrección

| Hallazgo | Detectado por | Corrección |
|---|---|---|
| **Paquete con vulnerabilidad alta:** la plantilla de Web API traía `Microsoft.AspNetCore.OpenApi`, que dependía de `Microsoft.OpenApi 2.0.0` | La política de build (warnings como errores) | Reemplazo por Swashbuckle, sin dependencias vulnerables |
| **Errores servidos como `application/json`** en lugar de `application/problem+json` (RFC 9457) | Desarrollador | Se quitó `[Produces]`, que imponía el tipo a todas las respuestas |
| **Runbook incorrecto:** afirmaba que el rol de emergencia podía corregir inventario, cuando no tenía permisos sobre `products` | Revisión del runbook contra la migración | Texto corregido; la limitación quedó documentada |
| **38 vulnerabilidades altas en la imagen de nginx** (paquetes Alpine desactualizados en la imagen base) | Trivy, al validar la etapa de escaneo del pipeline | Actualización de paquetes en el build (0 restantes) y umbral del pipeline elevado a altas y críticas |
| nginx no arrancaba con el sistema de archivos de solo lectura | Verificación del stack | `tmpfs` asignado al usuario sin root |
| Migración con valor por defecto inválido para intentos de pago existentes | Revisión de la migración | Valor `Unknown` |
| **Suma de MSI inexacta:** $100 a 3 MSI daba $99.99 y $3,499 a 6 MSI daba $3,499.02 | Desarrollador | Plan de pagos con truncado y primer pago que absorbe la diferencia; prueba sobre ~77,000 combinaciones |
| **Débito no contemplado** en un checkout con MSI | Desarrollador | Identificación por BIN; débito solo de contado |
| **Consulta de órdenes sin verificación:** cualquiera con el número de orden veía el detalle | Desarrollador | Consulta con número de orden + correo; la ruta por id deja de exponerse a la web |
| **Autocompletado cruzado:** el navegador sugería el nombre en el campo de correo porque los campos no tenían `name` | Desarrollador | `name` estándar en cada campo |
| **Campos desalineados** por una leyenda bajo el correo | Desarrollador (captura de pantalla) | Leyenda eliminada y campos anclados arriba |

---

## Cómo se verificó el trabajo

1. **Revisión humana de cada commit:** el desarrollador aprobó, pidió cambios o rechazó cada propuesta a partir del resumen del diff.
2. **Revisión funcional y visual del desarrollador:** probó la aplicación en el navegador en cada ronda y reportó hallazgos concretos.
3. **Pruebas automatizadas:**
   - 82 unitarias, 29 de integración y 10 del frontend;
   - 18 verificaciones E2E;
   - 22 aserciones de la colección Postman.
4. **Verificación contra infraestructura real:** PostgreSQL con los roles de producción, escenarios SQL por rol y el stack completo de Docker a través de nginx.
5. **Herramientas de seguridad:** SCA, gitleaks, Trivy y actionlint ejecutados localmente; CodeQL configurado en el pipeline.
6. **Transparencia sobre límites:** lo que no se pudo verificar se reportó como no verificado (la interfaz renderizada por la IA, el enmascarado de tarjeta con eventos reales de teclado, la ejecución del pipeline en GitHub).
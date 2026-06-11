# Convenios / Marketplace / Scoring — diseño v3

> ⚠️ **BORRADOR — Fase B, EN PAUSA.** Reanudar tras el inventario de Fase A (terminar la base
> existente). Plan Mode: este documento captura decisiones y reconciliación contra código;
> **nada de aquí está implementado ni autoriza implementación.**
> Origen: sesión de diseño jun-2026 (Parte 2a completada; 2b benchmarking / 2c modelo /
> 2d decisiones quedaron pendientes de retomar).

---

## 1. Visión (insumo del dueño)

Repositorio **global** de productos, marcas y proveedores, tipo marketplace. Cualquier usuario
entra al catálogo global, ve productos y qué proveedores los venden. Para **comprar** solicita un
**Convenio de compraventa**: contrato entre proveedor y comprador/distribuidor/revendedor, con
condiciones/términos/políticas, aceptado **digital pero legalmente vinculante**. Firmado, el
comprador hace pedidos, y **cada orden cumple las condiciones aceptadas**.

## 2. Decisiones YA TOMADAS (el modelo DEBE honrarlas)

### D1 · Mecanismo de términos HÍBRIDO sobre estructura versionada efectivo-fechada

- Cada Convenio es **único entre 2 partes** (un proveedor, un comprador).
- Los términos son una **secuencia de TermVersions efectivo-fechadas**: cada versión supersede a
  la anterior; la anterior **se conserva** (histórico auditable); **cada pedido honra la versión
  vigente al momento de la orden**.
- Una nueva TermVersion nace por dos fuentes, misma estructura:
  1. **Automática por nivel de estrellas** (fuente principal): cada relación tiene un nivel
     (p. ej. 1★–5★); cada nivel trae un **paquete de beneficios**; subir mejora el paquete,
     bajar lo **recorta**. Un cambio de nivel **genera automáticamente** la nueva TermVersion.
  2. **Override manual** (excepción): el proveedor supersede el baseline del nivel **solo en esa
     relación**. Caso canónico: base nivel >20M → 15%; override → >20M = 17%, solo Cliente A↔Prov A.
- **Resolución por capas**: baseline del nivel + override manual opcional encima; gana la versión
  vigente más reciente sin importar la fuente. Cada versión lleva
  `source: level-derived | manual-override | initial` (trazabilidad).
- Los descuentos por **umbral de volumen** (≥10M → 10%, ≥20M → 15%…) viven dentro del paquete de
  términos vigente.
- **Override — decisión diferida:** mecanismo = **reglas configurables por proveedor**; la
  **granularidad de la discrecionalidad** (qué campos puede pisar, con qué límites) queda
  **pendiente de validación de mercado (MVP)**.

### D2 · Scoring bidireccional por relación + score global por party

- Cada parte **evalúa a la otra** (comprador↔proveedor): **score directo por relación**.
- **Modelo de trabajo (a validar en 2d-1):** el **nivel★ por relación** fija los términos de *ese*
  convenio; el **score global** (agregación de todos los convenios de una party) es **reputación
  de marketplace** (descubrimiento/confianza), **no** driver de descuentos. Un comprador puede ser
  4★ con A y 2★ con B.
- **Ambos lados tienen niveles con beneficios**: comprador → comerciales (descuento, crédito,
  plazo, mínimo más bajo); vendedor → paquete propio (catálogo por proponer en 2b/2d: visibilidad/
  ranking en el global, badges de confianza, comisiones…).
- Reconciliar con los **Scorecards** existentes (fase F) sin duplicar — ver §3.2.

### #6 (working model) · Convenio **tenant↔tenant** con espejo de Party local

El comprador del convenio marketplace es un **tenant** (empresa cliente del SaaS); cada extremo
mantiene un **Party espejo local** de la contraparte (para documentos, snapshot DIAN y flujos
tenant-locales existentes). *Working model a confirmar en 2d.*

---

## 3. Reconciliación con lo construido (2a — COMPLETADA, leída del código jun-2026)

### 3.1 El "Convenio" actual (fase E)

| Aspecto | As-built (evidencia) | vs visión D1 |
|---|---|---|
| Ámbito | Tenant-local (`Agreement : BaseEntity<Guid>`, filtro tenant ON) | ❌ no cruza tenants |
| Partes | Solo `SupplierId` (Party del mismo tenant, `AgreementFeatures.cs:282`); comprador implícito = el tenant | ❌ no es par 2-partes |
| Unicidad de par | No existe (N convenios por proveedor posibles) | ❌ nuevo |
| Términos | Campos planos (PriceListId, responsables, 3 políticas, ValidFrom/To) **mutables in-place** hasta `Terminado` (inmutable, `Agreement.cs:124-139`) | ❌ sin TermVersion/histórico |
| Reglas | 9 `AgreementRuleType` = **gates de elegibilidad** del distribuidor, evaluados ad-hoc (`/evaluate?distributorId=`) | ⚠️ gates, no beneficios; umbrales de volumen no existen |
| Nivel/categoría | No existe; `AgreementType` es tipología, no tier dinámico | ❌ RelationshipLevel 100% nuevo |
| Firma/aceptación | No existe (status unilateral) | ❌ nuevo |

### 3.2 Scorecards (fase F)

- `SupplierScorecard`: **unidireccional, tenant-local, por período**; KPIs ponderados (defaults
  Calidad 35 · Entrega 25 · Precio 20 · Servicio 10 · Cumplimiento 10) → `WeightedScore` 1–5 →
  `Grade` A–F; inmutable en `Cerrado`.
- De facto el campo `SupplierId` ya se usa como "EvaluatedPartyId" (el motor puntúa al
  **distribuidor**: `AgreementFeatures.cs:315`). La dualidad de dirección está insinuada, no modelada.
- **El hook E↔F ya prueba el patrón D2**: `CalificacionMinima` lee el último `WeightedScore`
  (`AgreementFeatures.cs:313-320`) — "score alimenta términos" funciona en una dirección.
- Encaje propuesto: Scorecards = **insumo de evaluación manual periódica** del score por relación
  (junto a métricas automáticas futuras de Orders); `RelationshipLevel` se **deriva** de ellos
  (no segunda vara). Reconciliación mínima: renombrar conceptualmente `SupplierId` →
  `EvaluatedPartyId` + dirección.

### 3.3 Pricing

`PriceListItem` por `VariationId` con `Price` COP, **`MinQuantity` (quantity-break ya existe)**,
`SalePrice` con ventana, histórico (`PriceListItemHistory`); `PartyPriceList` asigna listas a
terceros. El **% por umbral de volumen acumulado** no tiene ancla actual: pertenece a la
TermVersion y se aplicaría encima de la lista en el futuro Orders. Tres anclas componibles:
lista seleccionada por el convenio (a) + breaks por cantidad por ítem (b) + % por umbral en
TermVersion (c).

### 3.4 Cruce de tenants

- Patrón global existente es **solo lectura** (`IGlobalCatalogReader.RunAsync` → scope hijo fijado
  al tenant `global`, `GlobalCatalogReader.cs:10-26`); escrituras globales solo seeders/publicación.
- Convenio entre dos tenants **no tiene precedente**. Rutas candidatas (desarrollar en 2c):
  entidad `IGlobalEntity` en plano global con ambos extremos explícitos, vs registros espejo por
  tenant sincronizados por integration events (Outbox ya existe; eventos llevan `TenantId`).
- Identidad: el mismo NIT es un Party **distinto** en cada tenant (sin identidad global de party);
  los tenants sí tienen identidad única → motivó el working model #6.

### 3.5 Resumen nuevo-vs-existente

| Pieza D1/D2 | Estado |
|---|---|
| Contrato con políticas, estado, inmutabilidad al cierre | ✅ Existe (Agreement) |
| Gates de elegibilidad + motor `/evaluate` | ✅ Existe (AgreementRule) |
| Evaluación periódica ponderada de contraparte | ✅ Existe (Scorecard, unidireccional) |
| Score → condición del convenio | ✅ Existe (hook E↔F) |
| Listas de precio + breaks por cantidad + histórico | ✅ Existe |
| Par único 2-partes · TermVersion con `source` · niveles★ con paquetes · score bidireccional por relación · score global · % por umbral de volumen · firma/aceptación · convenio cross-tenant | ❌ Todo nuevo |

---

## 4. Decisiones ABIERTAS (responder antes de 2c definitivo)

1. **Escala de niveles**: cuántos niveles; qué dispara subir/bajar (volumen, recencia,
   cumplimiento de pago, evaluaciones de la contraparte, score compuesto); umbrales fijos vs
   relativos; degradación gradual vs inmediata; **paquete de beneficios por nivel para cada lado**.
2. **Precedencia override ↔ bajada de nivel**: si el proveedor dio override al Cliente A y A baja
   de nivel, ¿el override sobrevive (concesión deliberada) o lo supersede el nuevo baseline?
   Regla por defecto + ¿configurable?
3. **Capa legal**: firma electrónica con validez jurídica (proveedor externo) vs aceptación
   in-app con trazabilidad. ⚠️ El marco colombiano de firma electrónica/digital es **input para
   decisión con posible asesoría legal — no hecho jurídico cerrado**.
4. **Scope del Convenio**: todo el catálogo del proveedor vs subconjunto (marcas/categorías/
   productos). Confirmado: **uno activo por par único**; ¿se conservan los vencidos/históricos?
5. **Resolución de precio/descuento** contra `PriceListItem`: ¿el convenio selecciona lista,
   aplica % por umbral sobre una base, o define precios propios? (las 3 anclas de §3.3 son
   componibles — decidir la combinación MVP).
6. **Confirmar #6**: comprador = tenant (working model, con Party espejo local) vs Party.
   Define dónde viven score global y RelationshipLevel.

## 5. Trabajo pendiente al reanudar (Fase B)

- **2b · Benchmarking**: Faire/Ankorstore/Alibaba B2B/Amazon Business (marketplace mayorista);
  trading agreements de ERPs (scope, listas por cuenta, MOQ, crédito, términos por volumen/nivel);
  programas de tiering (escalas, triggers, degradación, paquetes); CLM/firma electrónica
  (lifecycle, versionado con vigencia, inmutabilidad post-firma) + marco legal CO (con la
  salvedad de la decisión #3).
- **2c · Modelo**: entidades (Convenio par-único, TermVersion con `source`, RelationshipLevel
  bidireccional, Score por relación, Score global), máquinas de estado (lifecycle
  solicitud→negociación→firma→activo→vencido/terminado; versionado sin borrar), contrato de
  eventos (costuras hacia el futuro Orders/OMS: resolución de descuento por umbral contra la
  TermVersion vigente al momento de la orden, MOQ, crédito, plazo), encaje tenant `global`/
  `IGlobalEntity`.
- **2d · Resolver** las decisiones abiertas de §4 con opciones/tradeoffs/recomendación.

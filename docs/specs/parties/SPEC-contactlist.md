# SPEC — ContactList + PartyRelationship (gestión de contactos M2M) · G3 · **DEFINITIVO**

> **Estado:** Versión definitiva, fundamentada en investigación profunda de referentes (Attio, Folk,
> HubSpot, Salesforce, Odoo) vía NotebookLM + doc oficial de Salesforce (`AccountContactRelation`).
> Reemplaza los borradores previos. NO commitear hasta aprobación.
> **Destino en repo (propuesto):** `docs/specs/parties/SPEC-contactlist.md`
> **Módulo:** Parties (order 550). **Milestone:** M1. **Fecha:** 2026-06-19.

> ⚠️ **Este SPEC introduce cambios de modelo** respecto al v2 actual del repo (decisión del CEO: priorizar el
> mejor modelo sobre evitar retrabajo). Cambia el vínculo persona↔empresa de jerarquía única a **M2M**, y
> financia el subsistema de **custom fields**. Las reconciliaciones con el as-built van marcadas
> **[RECONCILIAR: repo]**.
>
> 🔵 **Capa transversal (NUEVO, §8):** este SPEC se cruzó con DIAN / facturación electrónica / NIIF,
> multitenancy y RBAC. Esas restricciones son **estructurales** y obligatorias — ver §8 antes de construir.
>
> 🟣 **Capa conversacional/omnicanal (NUEVO, §9):** cruce con Mercately, Kommo y WhatsApp Business Platform.
> Define la frontera contacto ⟂ negociación y qué del canal entra en M1.

---

## 0. Decisión arquitectónica central (el hallazgo de la investigación)

El modelo v2 actual usa `Party.ParentPartyId` (único) + `ContactProfile` (1:1) para el contacto — patrón Odoo
1:N. Los mejores CRM (HubSpot, Salesforce, Attio) usan **many-to-many**, porque una persona pertenece a
varias empresas (contador externo, dueño de varias empresas, rep). **Juan confirmó: en su negocio pasa
seguido.** Por lo tanto:

> **`Party.ParentPartyId` = SOLO jerarquía organizacional** (sucursales, casa matriz de una misma empresa).
> **El vínculo persona↔empresa-contacto = entidad de relación M2M `PartyRelationship`.**
> *(Fuente: el propio SPEC MAESTRO del repo lo enuncia — "usá `ParentPartyId` para la jerarquía
> organizacional, y reservá `PartyRelationship` para los vínculos de personas con esas entidades".)*

**El rol/cargo vive en la relación, no en la persona.** Una persona = un registro `Party`; sus vínculos con
N empresas = N filas en `PartyRelationship`, cada una con su cargo, función, principal y vigencia. Esto
elimina la duplicación de personas (el defecto del modelo single-parent y de Folk, donde el cargo es global).

---

## 1. Modelo de datos

### 1.1 `PartyRelationship` (entidad nueva — el join M2M)
Sintetizada de Salesforce `AccountContactRelation` (Roles, IsActive, IsDirect, StartDate/EndDate),
HubSpot associations (labels + primary) y Attio (metadata de contexto en la relación).

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | Guid | PK de la relación. |
| `SourcePartyId` | Guid (FK Party) | La **persona** (normalmente `TipoPersona=Natural`). |
| `TargetPartyId` | Guid (FK Party) | La **empresa** (o el Party dueño del vínculo). |
| `RelationshipType` | FK Tabla Básica | Empleado · Contacto Externo · Representante Legal · Socio · (ampliable). |
| `ContactFunction` | FK Tabla Básica | Facturación electrónica · Cuentas por pagar · Financiero · Comercial · TI · Gerencia · Operativo (ampliable). **[RECONCILIAR]** hoy es enum en `ContactProfile`; se migra a Tabla Básica en la relación (decisión de Juan: ampliable). |
| `JobTitle` | FK Tabla Básica ("Cargos laborales") | Cargo en **esa** empresa (distinto por relación). **[RECONCILIAR]** hoy es string en `ContactProfile`; pasa a Lookup en la relación. |
| `IsPrimary` | bool | Contacto principal **de la empresa Target**. Invariante: máximo uno activo por `TargetPartyId` (índice parcial único). |
| `IsActive` | bool | Estado operativo de la relación. |
| `StartDate` | date | Inicio de la vinculación. |
| `EndDate` | date? | Fin (null = vigente). |
| `CustomFields` | JSONB | Campos personalizados **de esta relación** (patrón Attio list-entry). Ver §3. |

> **[RECONCILIAR: repo]** El `ContactProfile` actual (1:1, con `JobTitle`/`ContactFunction`/`IsPrimary`/
> `ResponsibleUserId`/`IsCommercialContact`) queda **parcialmente superado**: los datos *por-empresa*
> (cargo, función, principal) migran a `PartyRelationship`. Decidir con Claude Code si `ContactProfile` se
> deprecia o se conserva solo para atributos **globales** de la persona-contacto (p. ej. `ResponsibleUserId`).
> Recomendación: lo por-empresa vive en `PartyRelationship`; `ContactProfile` se deprecia o se adelgaza.

### 1.2 Identidad y canales de la persona (sin cambios de patrón)
- **Campos de persona** (nombre, y opcionales nacimiento/género/estado civil): viven en la **identidad del
  Party-persona** (uniforme; el contacto **es** un Party). **No** hay `naturalPersonMode` — el modelo M2M lo
  hace innecesario igual que el v2.
- **Canales** (teléfono/email/WhatsApp): en el **Party-persona** vía `PartyChannel` (lista N a nivel Party,
  `IsPrimary` por canal). WhatsApp con opt-in/consent (Meta). **[RECONCILIAR]** confirmar `PartyChannel`
  as-built.

### 1.3 Jerarquía organizacional (se mantiene aparte)
`Party.ParentPartyId` + `ResolveCommercialEntity()` + `PartyHierarchyService` → **solo** para sucursales/
casa matriz. La delegación de datos fiscales/cupo del grupo sigue por ahí. NO se usa para contactos.

---

## 2. ContactList (el control, en el modelo M2M)

`ContactList` administra las **`PartyRelationship`** cuyo `TargetPartyId` = la empresa que se está viendo.

- **Cada fila** = una relación a una persona-Party. Muestra: Nombre (de la identidad de la persona),
  `JobTitle`, `ContactFunction`, `RelationshipType`, `IsPrimary`, `IsActive`, canal principal (del
  `PartyChannel` de la persona). Orden: Principal primero, luego por nombre.
- **Agregar contacto** = **buscar-o-crear** la persona-Party (si ya existe, se reutiliza — el punto del M2M)
  + crear una `PartyRelationship` (empresa, rol, función, cargo). El buscador de personas evita duplicar
  (ej. el contador que ya está en otra empresa).
- **Marcar Principal** = `PartyRelationship.IsPrimary=true`; invariante "uno por empresa" + índice parcial
  único (patrón `PartyCiiuActivity`/`PartyHold` del repo: `.IsUnique().HasFilter(...)`).
- **Editar / baja** = sobre la `PartyRelationship` (baja lógica `IsActive=false` o `EndDate`), sin borrar la
  persona (puede seguir vinculada a otras empresas).
- **Acciones por fila** (patrón Folk/Attio): registrar llamada, enviar WhatsApp, crear tarea (cuando esos
  módulos existan; en G3 quedan como ganchos [DISEÑO]).

### 2.1 Completitud gobernada
Crear con lo mínimo (nombre de la persona + empresa). Acciones exigen más:

| Acción | Mínimo |
|---|---|
| Agregar contacto | Nombre persona + `RelationshipType` |
| Marcar Principal | + un canal en la persona |
| Habilitar WhatsApp proactivo | Canal WhatsApp + opt-in |
| **Empresa "Completa"** (Persona Jurídica) | ≥1 relación **Rep. Legal** + ≥1 **Comercial** + ≥1 **Facturación** (patrón B2B: Ingram Micro / el SPEC MAESTRO) |

### 2.2 Validación 3 capas + UX
Cliente (Syncfusion) · Servidor (FluentValidation, completitud por acción) · Dominio (Principal único por
empresa) + índice parcial. UI: `MakaGrid` para la lista; modal/panel para alta/edición; `SfDropDownList`
para los Lookups; buscador de persona (autocomplete sobre Parties existentes). i18n namespace `parties`,
ES/EN, ES primero.

---

## 3. Subsistema de Custom Fields (FINANCIADO — entra como prerequisito)

> Juan aprobó financiar el subsistema. Se construye **bien**, con el modelo de Attio. Es un **prerequisito**
> de G3 (sin definiciones no hay nada que renderizar) — PR propio, antes o junto a ContactList.

### 3.1 `CustomFieldDefinition` (entidad nueva)
Modelo Attio. Campos: `Id`, `EntityType` (a qué se aplica: `Party` | `PartyRelationship` | … — el scope),
`Title`, `ApiSlug`, `FieldType` (ver §3.2), `Description`, `IsRequired`, `IsUnique`,
`IsDefaultValueEnabled`, `DefaultValue`, `Options` (para Select/MultiSelect, con color), `IsMultiselect`,
`Activo`. Por tenant (Finbuckle).

### 3.2 Tipos de campo (subconjunto de los 17 de Attio — arrancamos con los útiles)
`Text`, `Number`, `Currency`, `Date`, `Timestamp`, `Checkbox`, `Select` (opciones con color), `MultiSelect`,
`EmailAddress`, `PhoneNumber`, `Rating`, `RecordReference` (FK a otro Party). *(Los especializados de Attio
—Actor, Interaction, Location, Status, Domain— se difieren hasta que se necesiten.)*

### 3.3 Almacenamiento de valores
Columna **JSONB** en la entidad scopeada (`Party.CustomFields`, `PartyRelationship.CustomFields`). La
definición vive en `CustomFieldDefinition`; los valores en el JSONB de cada registro. **[RECONCILIAR]** hoy
no existe ni la columna ni la entidad (grep=0) — se crean.

### 3.4 Scope (clave, patrón Attio/Folk)
- Definiciones scopeadas a `Party` → atributos **globales** de la persona/empresa (viajan con el registro).
- Definiciones scopeadas a `PartyRelationship` → atributos **de contexto** (ese cargo/dato vale solo en esa
  relación empresa-persona). Es el patrón list-entry de Attio y el group-scope de Folk.

### 3.5 Alcance del subsistema en este lote
- **SÍ:** entidad `CustomFieldDefinition` + columnas JSONB + render dinámico en los formularios (el contacto
  lee/renderiza/guarda).
- **Editor visual** de definiciones (pantalla admin para crear/editar definiciones): **mínimo funcional**
  (crear definición con tipo + opciones) — Juan lo financió, así que entra una versión básica usable, no solo
  el consumo.

---

## 4. Fuera de alcance / diferido

- **Pantalla de Terceros** (montaje del control, pestañas) — integración posterior.
- **Tipos de atributo especializados de Attio** (Actor, Interaction, Location atómica, Status con `active_from`)
  — se agregan cuando un módulo los pida.
- **Eventos de integración** (`PartyRelationshipCreated`, etc.) — [DISEÑO], no se cablean.
- **Smart fields** (campos calculados tipo Folk "última interacción") — requieren el módulo de actividades/CRM.
- **Importación masiva** de contactos/relaciones — con Cargas Masivas.

---

## 5. Tests (unit)

- Persona existente vinculada a 2ª empresa → 2 `PartyRelationship`, 1 sola persona (no duplica).
- Cargo/función distintos por relación de la misma persona.
- Principal: dos `IsPrimary` activos bajo la misma empresa → rechazado (índice parcial único por `TargetPartyId`).
- Empresa "Completa" exige Rep. Legal + Comercial + Facturación.
- Baja de relación (`IsActive=false`/`EndDate`) no borra la persona ni sus otras relaciones.
- Custom fields: definición `Select` scopeada a `PartyRelationship` se renderiza y persiste en JSONB; una
  scopeada a `Party` aparece en todas sus relaciones.
- `IsRequired`/`IsUnique` de una definición se respetan en validación.
- Canales en la persona vía `PartyChannel`; WhatsApp sin opt-in no habilita envío.
- i18n: sin hardcode; todo `t()` namespace `parties` ES/EN.

---

## 6. Definición de Hecho

- [ ] `PartyRelationship` (M2M) creada; `ParentPartyId` reservado a jerarquía; `ContactProfile` reconciliado.
- [ ] `CustomFieldDefinition` + JSONB + render dinámico + editor mínimo de definiciones.
- [ ] `ContactList` sobre `PartyRelationship`: buscar-o-crear persona, rol por relación, Principal único por empresa.
- [ ] Canales vía `PartyChannel`; campos de persona en identidad; sin `naturalPersonMode`.
- [ ] Completitud gobernada (incl. mínimos Rep.Legal/Comercial/Facturación para Jurídica).
- [ ] Validación 3 capas + índices parciales; i18n `parties`.
- [ ] Tests §5 en verde; sin eventos cableados.

---

## 7. Reconciliaciones pendientes con el repo (para Claude Code, ANTES de construir)

1. **[RECONCILIAR]** `ContactProfile` (1:1) vs nuevo `PartyRelationship` (M2M): ¿deprecar `ContactProfile` o
   adelgazarlo a atributos globales? Reportar el plan antes de tocar.
2. **[RECONCILIAR]** Migración de datos: ¿hay contactos vivos en `PartyContact` v1 / `ContactProfile` v2 que
   haya que migrar a `PartyRelationship`? Plan de migración.
3. **[RECONCILIAR]** `ContactFunction` enum → Tabla Básica, y `JobTitle` string → Lookup: cambio de entidad;
   confirmar impacto en el front v2 ya existente.
4. **[RECONCILIAR]** `PartyChannel`, `PartyCiiuActivity`/`PartyHold` (patrón índice parcial), `ParentPartyId`/
   `ResolveCommercialEntity` — confirmar formas as-built para reusar, no reinventar.

> El orden sugerido de construcción: (1) subsistema Custom Fields → (2) `PartyRelationship` + reconciliación
> `ContactProfile` → (3) control `ContactList`. Tres PRs encadenados, cada uno verificado contra el repo.

---

## 8. Capa transversal — DIAN/NIIF, Multitenancy y RBAC (cruce regulatorio + infraestructura)

> Esta capa NO es opcional: impone restricciones estructurales al modelo. Fuentes: Resolución DIAN
> 000165/2023, Estatuto Tributario Art. 616-1/617, marcos NIIF para Pymes, y la doctrina del repo
> (Finbuckle/FSH).

### 8.1 Fiscal / DIAN / facturación electrónica
- **`FiscalData` vive en la empresa-Party (el adquiriente).** NUNCA en el contacto ni en la
  `PartyRelationship`. El contacto-persona jamás es el sujeto fiscal de la factura.
- **Dos ejes independientes** (no confundir, confirmado as-built): **Régimen de renta**
  (Ordinario/Simple/Especial) ≠ **Responsabilidad de IVA** (Responsable/No responsable).
- **Responsabilidades tributarias = colección de códigos DIAN** (catálogo/Tabla Básica): `O-13`
  Gran contribuyente · `O-15` Autorretenedor · `O-23` Agente ReteIVA · `O-47` Régimen Simple ·
  `R-99-PN` No aplica · (catálogo completo de la Res. 000165).
- **Obligatorio para FACTURAR (no para crear):** `TipoPersona`; tipo de doc (31=NIT, 13=Cédula…);
  número + **DV** (para NIT); razón social/nombre **según RUT**; régimen + responsabilidades;
  `EmailFacturacion`; dirección con **código DANE/municipio** (no solo nombre de ciudad).
- **Consumidor final:** frase "consumidor final" + NIT `222222222222` (no soporta costos/gastos/IVA descontable).
- **Retenciones** (ReteFuente/ReteIVA/ReteICA) se **derivan del `FiscalData`** del tercero (Gran
  Contribuyente, Autorretenedor, agente). **NIIF:** cada movimiento contable anclado a **tercero (NIT)**;
  exógena (formatos 1001-1009) vinculada por NIT.
- **Representante Legal = `PartyRelationship`** (`RelationshipType=RepresentanteLegal`) + flag
  **`IsPEP`** (Persona Expuesta Políticamente — compliance). Es requisito legal para personas jurídicas.
  → el mínimo "Empresa completa" (Rep. Legal + Comercial + Facturación) tiene **peso regulatorio**, no solo UX.
- **Adquiriente fiscal en M2M/jerarquía:** el adquiriente es la **empresa-Party**; si una sucursal no
  tiene `FiscalData` propio, `ResolveCommercialEntity()` resuelve la matriz vía `ParentPartyId` (dentro del
  mismo tenant). La `PartyRelationship` (contacto) **no** carga datos fiscales.

### 8.2 Completitud gobernada en 3 capas (con dientes regulatorios)
1. **Captura mínima** — modal "listo para facturar": lo mínimo de identidad para crear-y-volver.
2. **Completitud gobernada** — validación **contextual por acción**: *facturar* exige `FiscalData` completo +
   `EmailFacturacion` + dirección DANE; *despachar* exige dirección logística; etc.
3. **Operación completa** — ficha 360°.
> La norma se aplica en la capa 2 (al ejecutar la acción), **no** bloqueando la creación. Crear es barato;
> facturar exige cumplimiento.

### 8.3 Multitenancy (Finbuckle · shared-database · default-ON)
- **Todas** las entidades del modelo (`Party`, `PartyRelationship`, `ContactProfile`, `PartyChannel`,
  `CustomFieldDefinition` y los valores JSONB) son **tenant-scoped** vía `BaseDbContext` (filtro `TenantId`
  automático). Opt-out solo si implementan `IGlobalEntity`.
- **`CustomFieldDefinition` es por-tenant** (cada empresa estructura sus objetos según su industria).
- **M2M en G3 = dentro del mismo tenant.** El contacto/proveedor **compartido cross-tenant** (marketplace)
  NO se resuelve rompiendo el aislamiento: se habilita por el **módulo Convenios** (contratos digitales,
  Fase B). → en G3, `SourcePartyId` y `TargetPartyId` están en el **mismo tenant**; el cross-tenant queda
  **diferido a Convenios** (gancho [DISEÑO]).
- **Ejes ortogonales:** `ParentPartyId` (jerarquía organizacional) ⟂ `TenantId` (aislamiento SaaS). La
  delegación `ResolveCommercialEntity()` opera **dentro** del tenant.

### 8.4 RBAC (patrón FSH)
- **`.RequirePermission()` en TODOS los endpoints** de Terceros/Contactos/Relaciones/CustomFields.
  ⚠️ NUNCA `.RequireAuthorization()` (bug crítico #14 fail-**OPEN**, CLAUDE.md §18).
- **Visibilidad/edición por rol = DTOs distintos por rol.** El repo **descarta** permisos granulares
  por-campo (patrón Frappe, considerado sobre-ingeniería). **NO** diseñar `EntityPermission` por campo.
- **Acciones sensibles → privilegio elevado / auditoría:**
  - Editar `FiscalData` (régimen/responsabilidades) — sensible tributario.
  - Definir/editar `CustomFieldDefinition` (cambio de **esquema**) ≠ llenar valores (operativo) → permiso
    de **admin** para lo primero.
  - Aprobar `CupoAsignado` en `CreditAccount` → registra `AprobadoPor` + `CreditMovement` **inmutable**.
  - Aplicar `PartyHold` → emite `HoldPlacedIntegrationEvent` (transversal a Ventas/Compras).
  - Marcar/cambiar **Rep. Legal** y **PEP** — compliance.

### 8.5 Consecuencias para los 3 PRs
- **Custom Fields:** nace **tenant-scoped**; definiciones bajo permiso **admin**; valores bajo permiso operativo.
- **`PartyRelationship`:** **within-tenant** en G3; campo `IsPEP`; gancho cross-tenant (Convenios) [DISEÑO];
  el adquiriente fiscal nunca es la persona.
- **`ContactList`:** `.RequirePermission()` + DTOs-por-rol; completitud gobernada de 3 capas con los mínimos
  fiscales para marcar un tercero como **"facturable"**; Rep. Legal + PEP como relación con peso legal.

### 8.6 Verificación transversal (para Claude Code, ANTES de construir)
- **[RECONCILIAR]** `FiscalData` as-built: confirmar que tiene los dos ejes (régimen renta ⟂ responsabilidad
  IVA), la colección de responsabilidades (códigos DIAN), DV, `EmailFacturacion`, dirección con DANE.
- **[RECONCILIAR]** que `Party`/`PartyRelationship`/`CustomFieldDefinition` heredan de `BaseDbContext`
  (tenant-scoped) y ninguna es `IGlobalEntity` por error.
- **[RECONCILIAR]** los permisos FSH existentes de Parties y definir los nuevos (definir custom fields,
  editar FiscalData) sin romper el patrón.

---

## 9. Capa conversacional / omnicanal (Mercately · Kommo · WhatsApp Business Platform)

> Última revisión cruzada. Separa lo que el modelo de Contactos/Canales debe soportar **desde ya (M1)** de lo
> que es de un módulo de Mensajería/CRM posterior (M2/M3). Evita dos errores clásicos: un `PartyChannel`
> pobre para omnicanal, y **contaminar el contacto con datos de negociación**.

### 9.1 Identidad omnicanal y `PartyChannel` (IN — M1)
- **Identidad por canal:** teléfono / `wa_id` como identificador; **`WhatsAppName`** (profile name de la app,
  distinto del nombre del contacto) — el v2 **ya lo captura** **[RECONCILIAR: confirmar as-built]**. N canales
  por persona vía `PartyChannel` (WhatsApp, IG, Messenger, email en una identidad unificada — patrón Mercately).
- **Enriquecer `PartyChannel`** si as-built es solo `Value`+`IsPrimary`: añadir `ExternalId`/`wa_id`
  (identidad de plataforma ≠ número marcado), `ProfileName`, y un **VO de consentimiento**
  (`ConsentStatus` OptIn/OptOut, `ConsentTimestamp`, `ConsentSource` web/QR/SMS/conversación, estado de
  **bloqueo**). **[RECONCILIAR]**.
- **Deduplicación:** por **identificación (NIT/Cédula)** en la captura (doctrina del repo: la validación más
  valiosa desde el inicio). El **merge** de personas que llegan por canales distintos (un WhatsApp sin nombre
  que luego resulta ser un cliente con NIT) es una operación que el modelo debe **tolerar** (FKs reasignables,
  canales que se mueven); el **tooling de merge se difiere** a Mensajería/CRM.

### 9.2 Frontera contacto ⟂ negociación (la clarificación clave)
Cruzado con los campos de lead de Kommo/Mercately, **dónde vive cada dato**:

| Dato | Vive en | Nota |
|---|---|---|
| Etapa / pipeline | **Negociación/Lead** | un cliente puede estar en varios embudos a la vez |
| Fuente / atribución | **Party** | para cálculo de CAC |
| Agente responsable | **Party** (routing) **y** Negociación (vendedor del deal) | **dual** |
| Tags / etiquetas | **transversal** (Party + Conversación/Negociación) | |
| N° seguimiento · dirección de entrega · método de pago · descuento · razón de pérdida | **Negociación/Orden** | términos de la transacción actual |
| Estado de conversación (Nuevo/Por resolver/Resuelto) | **Conversación** | ni contacto ni negociación |
| **LifecycleStage** (Prospecto/Cliente/Recurrente) | **Party** (macro) | ≠ etapa de embudo |

> Doctrina del repo confirmada: **Lifecycle ≠ Embudos** (conceptos separados). No hay selector de embudo en
> la ficha del tercero; la sección CRM lista las oportunidades activas, cada una con su embudo+etapa.
> → `ContactList` / `PartyRelationship` **NO** cargan datos de pipeline/negociación.

### 9.3 WhatsApp Business Platform — modelo vs módulo de Mensajería
- **EN el modelo (M1):** el **opt-in** del cliente es prerequisito para mensajes iniciados por el negocio, y
  el **opt-out/bloqueo** debe respetarse (sacar de envíos). Vive como **consentimiento en el canal** (§9.1).
- **EN Mensajería (M2):** la **ventana de 24h** de atención (se reinicia con cada mensaje del cliente; fuera
  de ella solo **plantillas HSM aprobadas por Meta**), categorías de conversación + pricing, y el tracking de
  la ventana. Es estado de runtime de la `Conversation`, no atributo del contacto.
  *(Fuente: WhatsApp Business Messaging Policy — Meta.)*

### 9.4 M1 (ahora) vs M2/M3 (después)
- **M1 (entra al modelo):** identidad extendida (nombre, ID, DV, `WhatsAppName`, teléfonos clasificados),
  Profiles (Customer/Supplier/Employee), controles `AddressList`(DANE)/`PhoneList`/`ContactList`,
  **`LifecycleStage` en Party**, consentimiento por canal.
- **M2/M3 (módulos posteriores):** motor de `Conversation` (hilos, estados de chat, sync Meta Cloud API),
  embudos dinámicos (`WorkflowDefinition`), in-chat commerce (órdenes / links de pago en el chat), métricas
  predictivas (LTV/CAC/pLTV).

### 9.5 Verificación (Claude Code)
- **[RECONCILIAR]** `PartyChannel` as-built: ¿tiene `WhatsAppName`/`wa_id`/`ExternalId`/consentimiento, o solo
  `Value`+`IsPrimary`? Si lo segundo, planear la extensión (es modelo de canal, no de mensajería).
- **[RECONCILIAR]** `LifecycleStage` en Party as-built (atributo macro, separado de embudos).
- **[DISEÑO]** ganchos de merge/dedup y la separación `Conversation`/`Negociación` — no implementar tooling,
  pero **no contaminar** el contacto con esos datos.

---

## 10. Decisiones de reconciliación — APROBADAS por Juan (2026-06-19)

> Estas decisiones cierran los `[RECONCILIAR]` de §1, §7, §8.6 y §9.5 contra el as-built verificado por
> Claude Code. **Tienen prioridad** sobre cualquier afirmación previa de este SPEC que las contradiga.

1. **Modelo de contacto (§1, §7.1):** **adelgazar `ContactProfile`** a atributos **globales** de la persona
   (p. ej. `ResponsibleUserId`); **deprecar `PartyContact` v1**. Todo lo *por-empresa*
   (`RelationshipType`/`ContactFunction`/`JobTitle`/`IsPrimary`/`IsCommercial`) vive **solo en
   `PartyRelationship`**. Patrón HubSpot (contact owner global + labels por asociación).
   - **Confirmación pendiente de Juan:** ¿algún tenant tiene contactos cargados, o es dev limpio? Si dev →
     migración de datos = None.

2. **`ContactFunction` / `JobTitle` / `RelationshipType` → Tabla Básica (§7.3):**
   - `ContactFunction`: Tabla Básica **con códigos de sistema protegidos** — `FacturacionElectronica` y
     `Comercial` quedan **seeded, no borrables ni renombrables** (la lógica de ruteo DIAN keya sobre el
     `code` estable); el resto del catálogo es libre/ampliable.
   - `JobTitle`: Lookup **"Cargos laborales"** — reusar el patrón de `PartyContact.PositionCode`, que **ya**
     es Tabla Básica.
   - **Acción adicional (PR-2):** actualizar `parties/SPEC.md` maestro §6.3/§13 (deja de fijar
     `ContactFunction` como enum) y el **front v2** que hoy consume el enum.

3. **`PartyChannel` omnicanal (§9.5): DIFERIDO a Mensajería (M2).** PR-3 (`ContactList`) usa `PartyChannel`
   tal cual existe (`ChannelTypeCode` + `Value` + `Reference` + `IsPrimary`). **NO** agregar
   `wa_id`/`ExternalId`/`ProfileName` ni VO de consentimiento ahora. La extensión futura se diseña
   **aditiva** (gancho **[DISEÑO]**): no debe reabrir el contrato de `ContactList` cuando llegue M2.

4. **`EmailFacturacion` (§8.1):** agregarlo a **`FiscalData`** (en PR-2). Hoy se reusa `Party.Email`; se
   **separa** en un campo fiscal dedicado.

5. **`IsPEP` (§8.1) → a nivel PERSONA**, no empresa ni relación:
   - `IsPEP` (+ `PepType`/cargo opcional) en el **Party-persona** (identidad). Se marca **una vez**; todas
     sus relaciones lo **heredan** (evita la inconsistencia de PEP-en-N-empresas).
   - La relación rep-legal (`PartyRelationship` con `RelationshipType=RepresentanteLegal`) **expone/refleja**
     el `IsPEP` de la persona para el compliance de esa empresa (no lo duplica como dato propio).
   - **Deprecar `FiscalData.FlagPEP`** (empresa): "¿esta empresa tiene rep legal PEP?" se responde vía la
     relación rep-legal → `persona.IsPEP`. **No borrar en caliente:** marcar como *deprecated* y migrar si
     hay datos.

6. **Editor de Custom Fields (§3, PR-1) — tipos de arranque:** `Text`, `Number`, `Currency`, `Date`,
   `Checkbox`, `Select`, `MultiSelect`. (`EmailAddress`/`PhoneNumber` si son baratos; `Rating`,
   `RecordReference`, `Timestamp` después.)

7. **Orden de ejecución confirmado:** PR-1 (Custom Fields) → PR-2 (`PartyRelationship` + reconciliación
   `ContactProfile`/`PartyContact` + `EmailFacturacion` + `IsPEP`) → PR-3 (`ContactList` + i18n). Cada PR
   verificado contra el repo, con Plan Mode aprobado **antes** de escribir código.

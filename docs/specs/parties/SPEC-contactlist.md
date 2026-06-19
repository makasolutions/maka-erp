# SPEC — ContactList (control de Contactos de un Tercero) · G3

> **Estado:** Borrador de diseño para revisión de Juan. NO commitear hasta aprobación.
> **Destino en repo (propuesto):** `docs/specs/parties/SPEC-contactlist.md`
> **Módulo:** Parties (order 550). **Milestone:** M1.
> **Alcance G3 (decidido):** Control **completo** — soporta caso **Empresa** y caso **Persona Natural**
> en su lógica, con unit tests para ambos. **NO** se construye todavía la pantalla de Terceros donde se
> integra; el cableado de la inserción de campos de persona en esa pantalla queda **diferido a la fase de
> integración** (documentado en §7).
> **Fecha:** 2026-06-19. **Rev:** enriquecido con barrido dirigido de referentes (NotebookLM, cuaderno
> "Maka ERP Project Roadmap"). **Decisiones de §10 cerradas:** i18n=`parties`; custom fields mínimo SÍ en G3;
> canales (email/teléfono) inline en el contacto. **§10 abiertas también cerradas:** sin tope de contactos;
> catálogo de Función operativa fijado.

> ⚠️ **Marcas de verificación.** El barrido de referentes confirmó el *patrón de diseño*. Donde queda un
> **[VERIFICAR: parties/SPEC.md]** es porque depende del **nombre exacto de entidad/campo as-built en el
> repo** (el cuaderno no es el repo). Claude Code confirma esos nombres antes de construir y reporta
> divergencias — no inventar nombres que contradigan lo construido.

---

## 0. Fundamentación (barrido de referentes — confirmado)

El diseño se apoya en el barrido dirigido a Contactos/Personas de los referentes, que **confirmó** (no
cambió) las decisiones:

- **Campos de persona opcionales en todos:** Effi (Género/Fecha nacimiento/Estado civil presentes, no
  requeridos), Folk (Birthday/Gender nativos), Attio (atributos Date/Select configurables). Ninguno los
  hace obligatorios.
- **Multi-contacto por empresa:** Siigo permite **hasta 10 contactos** por cliente; Effi usa "Tercero base
  + roles" con N personas vinculadas; Attio conecta Companies↔People; Odoo vincula vía `parent_id`.
- **Persona Natural vs Jurídica por campo único:** Siigo `person_type` (`person`/`company`); Effi enum
  "Tipo de persona" (Natural/Jurídica). → valida el enfoque `naturalPersonMode` (§4).
- **Clasificación por función operativa (B2B):** Ingram Micro organiza contactos por **Facturación
  electrónica / Cuentas por pagar / Financiero / Comercial** → base de `TipoRelacion` (§3.1).
- **Cargo/Área por tablas de mantenimiento:** Effi usa "Cargos laborales" y "Departamentos laborales"
  → van por Lookups (Tabla Básica), no texto libre.
- **Patrón del control de lista (confirmado como el del repo):** *cada fila con `Activo`, `Principal`
  (cuál es la primaria) y clasificación por Tabla Básica; N filas por owner.*
- **Relación con la matriz:** `Party.ParentPartyId` (regla de modelado R4), **NO** un campo del perfil.

---

## 1. Propósito y encuadre

`ContactList` es el control reutilizable que administra los **contactos de un Tercero** (Party): las personas
con las que se interactúa dentro de una empresa cliente/proveedor (gerente de compras, contador, almacenista),
o —en el caso de Persona Natural— la persona misma.

Es un **control genérico polimórfico**, coherente con el patrón ya confirmado del repo para los controles de
lista (Address/Phone/etc.): `OwnerType` (string) + `OwnerId` (Guid), N filas por owner, cada fila con
`Activo` + `Principal` (índice parcial único) + clasificación por Tabla Básica. **[VERIFICAR: parties/SPEC.md]**
el nombre exacto de la entidad de contacto (¿`Contact`? ¿`ContactProfile`? ¿`PartyContact`?) y si Email/Phone
ya son controles polimórficos reutilizables.

**Qué NO es:** no es el perfil CRM del Tercero (eso vive en el Party: lifecycle, lead score). No es la pantalla
de Terceros. Es solo el control de la lista de personas-contacto y su CRUD.

---

## 2. Principio rector: completitud gobernada (no completitud exigida)

Alineado con la doctrina del repo (CLAUDE.md §17, validación de formularios) y con todos los referentes
(ninguno exige los campos de persona): **el sistema persigue la completitud, no la exige al crear.** Crear un
contacto con lo mínimo está permitido; ciertas **acciones** exigen más datos.

| Acción | Datos mínimos exigidos |
|---|---|
| Crear contacto | Nombre (solo eso) |
| Marcar como **Principal** | Nombre + al menos **un canal** (email o teléfono/celular) |
| Habilitar envío **WhatsApp** al contacto | Número WhatsApp + **Opt-in/Consentimiento** registrado (Meta) |
| Usar como contacto de **facturación** (futuro) | Email válido (lo consume DIAN; `system-module-map.md` §3) |

---

## 3. Modelo de datos (campos del contacto)

> **[VERIFICAR: parties/SPEC.md]** — cotejar cada campo contra el `Contact`/`ContactProfile` as-built.
> Donde el repo ya tenga el campo, usar su nombre exacto; donde no exista, este SPEC lo propone (respaldado
> por los referentes de §0).

### 3.1 Identidad y clasificación del contacto
- `Nombres` (string, requerido) · `Apellidos` (string, opcional para PN simple).
- `Cargo` / `Puesto` (FK Tabla Básica, opcional) — taxonomía "Cargos laborales" estilo Effi.
- `Area` / `Departamento` (FK Tabla Básica, opcional) — Compras, Contabilidad, Almacén, Gerencia.
- `TipoRelacion` / **`FuncionOperativa`** (FK Tabla Básica, opcional) — catálogo inicial (patrón Ingram
  Micro): **Facturación electrónica · Cuentas por pagar · Financiero · Comercial · Gerencia · TI · Operativo**.
  Habilita **ruteo de comunicaciones** (a quién mandar la factura, a quién la cotización). Ampliable por
  Tabla Básica sin tocar código.

### 3.2 Canales de contacto (INLINE, con contrato de control de lista — decidido)
> **Decisión:** los canales van **inline en el contacto**, pero **siguiendo el mismo contrato de los
> controles de lista** (N por contacto, `Principal` por canal, clasificación por Tabla Básica) — patrón
> Siigo/Effi/repo.
> ⚠️ **[VERIFICAR: repo]** antes de construir: si en el repo Email/Phone YA son controles polimórficos
> reutilizables, **reportar la divergencia** antes de duplicar lógica inline (decisión informada, no
> accidental).
- `Emails` (N, cada uno con `Principal` + clasificación) — al menos uno recomendado, no obligatorio.
- `Telefonos` (N: fijo/celular, cada uno con `Principal` + clasificación).
- `WhatsApp`: número + `WhatsAppName` + **`Consent` (VO: opt-in, fecha, origen)**. El opt-in bloquea
  envíos proactivos sin consentimiento (regla Meta).

### 3.3 Campos de persona (OPCIONALES — confirmado en todos los referentes)
`FechaNacimiento` (date, opcional) · `Genero` (FK Tabla Básica, opcional) · `EstadoCivil` (FK Tabla Básica,
opcional). **Todos opcionales en todos los casos** (Effi, Folk, Attio, Siigo, Odoo — ninguno los hace
obligatorios). Su *ubicación* cambia según el tipo de Tercero — ver §4.

### 3.4 Estado y metadatos
- `Principal` (bool) — **invariante: máximo un contacto Principal activo por Party** (índice parcial único:
  `WHERE Activo AND Principal`). Patrón confirmado como el del repo. **[VERIFICAR]** replicar el mismo
  mecanismo de índice parcial que ya usan los otros controles de lista.
- `Activo` (bool, default true) — baja lógica, no borrado físico.
- `Notas` (text, opcional).
- `CustomFields` (JSONB, opcional) — extensibilidad tipo Attio vía `CustomFieldDefinition`. **Mínimo SÍ en
  G3** (ver §6): el contacto **lee/renderiza las `CustomFieldDefinition` existentes y guarda sus valores en
  el JSONB**. La pantalla para **crear/definir** nuevos custom fields (administración) NO entra en G3.

> **Decidido:** **sin tope** de contactos por Party (Siigo cae en 10; no hay razón técnica para limitar).

---

## 4. Regla de Persona Natural (caso Empresa vs. Persona Natural)

Esta es la lógica que hace al control "completo" (alcance G3 decidido). Confirmada por el patrón `person_type`
de Siigo y el enum "Tipo de persona" de Effi.

**Caso Empresa (Party = Persona Jurídica):** la empresa tiene **N contactos** (personas distintas). Los campos
de persona (§3.3) viven **en cada contacto**. Es el caso B2B, el más común y el más complejo — el que G3 clava.

**Caso Persona Natural (Party = Persona Natural):** el Tercero *es* una persona. Sus campos de persona
pertenecen a su **identidad** (pestaña principal de la pantalla de Terceros), no a una lista de contactos. El
`ContactList` puede seguir existiendo (un asistente, un familiar autorizado), pero los campos de persona del
*titular* no se editan dentro de la lista.

**Cómo lo resuelve el control (sin construir la pantalla de Terceros):**
- `ContactList` expone una prop/`mode`: **`naturalPersonMode: boolean`**.
  - `false` (Empresa, default): comportamiento completo, campos de persona visibles en cada contacto.
  - `true` (Persona Natural): los campos de persona del titular se **ocultan/relocalizan** (la lista no es su
    dueña); el control sigue permitiendo contactos secundarios sin campos de persona del titular.
- El control se construye y se **unit-testea en ambos modos** en G3.
- La **inserción real** en la pestaña de identidad de Terceros (cómo se muestran esos campos cuando
  `naturalPersonMode=true`) es **trabajo de integración diferido** (§7) — depende de una pantalla que aún no
  existe. Documentado acá para que no se rediseñe luego.

> Decisión registrada: G3 entrega el control completo (ambos modos + tests); la integración cablea el modo
> Persona Natural a la pantalla de Terceros. No es "rediseñar después" — es secuenciar: la regla está
> definida desde ya, G3 implementa lo que no depende de la pantalla.

---

## 5. Comportamiento del control (UI)

> Frontend: React 19 + Syncfusion vía wrappers Maka* (NO Blazor). Colores por tokens CSS. Texto vía `t()`,
> ES primero. **[VERIFICAR: parties/SPEC.md / CLAUDE.md §18]** el patrón de los controles de lista existentes
> (Address/Phone) y replicar su UX para consistencia.

- **Lista:** `MakaGrid` con columnas clave (Nombre, Cargo, Función, Canal principal, Principal, Activo).
  Orden: Principal primero, luego por nombre.
- **Alta/edición:** modal o panel lateral (seguir el patrón del control de Address/Phone — **[VERIFICAR]**).
  Inputs: `SfTextBox`, `SfDropDownList`, `SfDatePicker` para fecha de nacimiento (mapa de componentes
  obligatorio, CLAUDE.md §9).
- **Marcar Principal:** acción por fila; al marcar uno, el anterior Principal se desmarca (mantiene el
  invariante §3.4). Exige los mínimos de §2.
- **Baja:** lógica (`Activo=false`), nunca borrado físico. Confirmación.
- **Validación en 3 capas:** Cliente (máscaras Syncfusion, formato email/teléfono, tiempo real) · Servidor
  (FluentValidation, completitud gobernada por acción) · Dominio (invariante de Principal único). + índice
  único parcial en persistencia.
- **i18n:** todas las cadenas vía `t()` en el namespace **`parties`**, ES y EN, ES primero.

---

## 6. Fuera de alcance de G3 (explícito, para no expandir el PR)

- **Editor de definiciones de Custom Fields** (crear/editar `CustomFieldDefinition`, tipo Attio). Eso es
  administración y va aparte. **SÍ entra en G3** el consumo: el contacto lee las definiciones existentes,
  las renderiza y guarda valores en el JSONB (ver §3.4).
- **Pantalla de Terceros** y el cableado de Persona Natural a la identidad (§7).
- **Eventos de integración.** Cualquier `PartyContactAdded`/`ContactUpdated` es **[DISEÑO]**, no se cablea
  (`system-module-map.md` §5: solo 4 eventos [REAL]; publicar a un consumidor inexistente = bug silencioso).
  Diseñar la entidad *preparada* para emitirlos, sin emitir.
- **Importación masiva** de contactos (CSV) — va con Cargas Masivas, no acá.

---

## 7. Trabajo diferido a la fase de integración (documentado, no se pierde)

1. **Inserción en la pantalla de Terceros:** cómo se monta `ContactList` en la ficha del Tercero, y cómo, en
   `naturalPersonMode=true`, los campos de persona del titular se muestran en la pestaña de identidad.
2. **Delegación jerárquica (patrón Odoo):** la relación con la matriz es **`Party.ParentPartyId` (regla R4),
   no un campo del perfil** (confirmado en el barrido). Decidir si los contactos se heredan/visualizan desde
   la matriz vía `ResolveCommercialEntity()`. **[VERIFICAR: parties/SPEC.md]** que `ParentPartyId` y ese
   método existen as-built (`system-module-map.md` §3 los menciona).
3. **Exposición a vecinos** (cuando se construyan): DIAN consume el email del contacto de **Facturación
   electrónica**; WhatsApp consume el número + consent. Solo cablear cuando esos módulos existan. (La función
   operativa de §3.1 es justo lo que habilita ese ruteo.)

---

## 8. Tests (unit, G3)

- Crear contacto con solo nombre → OK (completitud gobernada).
- Marcar Principal sin canal → rechazado; con canal → OK.
- Dos contactos Principal activos en el mismo Party → rechazado por el invariante (índice parcial único).
- `naturalPersonMode=false`: campos de persona visibles/editables en el contacto.
- `naturalPersonMode=true`: campos de persona del titular ocultos/relocalizados; contactos secundarios OK.
- WhatsApp sin opt-in → no permite marcar el contacto como habilitado para envío proactivo.
- Baja lógica: `Activo=false` no borra fila; queda fuera de la lista activa.
- Clasificación por función operativa (Facturación/CxP/Financiero/Comercial) persiste y filtra.
- Custom fields: el contacto renderiza una `CustomFieldDefinition` existente y persiste su valor en JSONB
  (crear definiciones nuevas NO se prueba acá — está fuera de G3).
- Canales inline: alta de N emails/teléfonos en el contacto, con Principal por canal.
- i18n: no hay cadenas hardcodeadas; todas resuelven por `t()` en ES y EN.

---

## 9. Definición de Hecho (G3)

- [ ] Entidad/campos alineados con `parties/SPEC.md` (divergencias reportadas y resueltas).
- [ ] Control `ContactList` (React + Maka*/Syncfusion) con CRUD, Principal único, baja lógica.
- [ ] `naturalPersonMode` implementado y unit-tested en ambos modos.
- [ ] Clasificación por Cargo/Área/Función operativa vía Tabla Básica.
- [ ] Canales (email/teléfono) inline, con Principal por canal. (Divergencia repo reportada si Email/Phone ya eran polimórficos.)
- [ ] Custom fields (consumo): lee/renderiza definiciones existentes y persiste valores en JSONB. Editor de definiciones fuera de G3.
- [ ] Validación en 3 capas + índice parcial único en persistencia.
- [ ] i18n ES/EN completo.
- [ ] Tests de §8 en verde.
- [ ] Sin eventos cableados (solo entidad preparada).
- [ ] Trabajo de §7 documentado en el SPEC del repo como diferido.

---

## 10. Estado de decisiones — TODAS CERRADAS

1. ✅ **Namespace i18n:** `parties`.
2. ✅ **Custom fields en G3 (patrón Attio):** consumo SÍ — el contacto lee/renderiza las `CustomFieldDefinition`
   existentes y guarda valores en JSONB. El **editor de definiciones** (crear/editar definiciones) va aparte.
3. ✅ **Canales del contacto:** inline, pero siguiendo el contrato de los controles de lista (N por contacto,
   `Principal` por canal — patrón Siigo/Effi/repo). **[VERIFICAR repo]:** si Email/Phone ya son polimórficos
   reutilizables, reportar antes de duplicar.
4. ✅ **Tope de contactos por Party:** **sin tope**.
5. ✅ **Catálogo de Función operativa (patrón Ingram Micro):** Facturación electrónica · Cuentas por pagar ·
   Financiero · Comercial · Gerencia · TI · Operativo. (Ampliable por Tabla Básica sin tocar código.)

> SPEC en versión definitiva. Listo para crear en el repo (`docs/specs/parties/SPEC-contactlist.md`) y arrancar G3.

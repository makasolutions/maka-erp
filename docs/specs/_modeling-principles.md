# Principio de Modelado — Maka ERP

> **Estatus:** Principio rector permanente. Aplica a TODOS los SPECs y módulos, sin excepción.
> **Anclado:** junio 2026, por directriz explícita de Juan.
> **Ubicación destino en repo:** `docs/specs/_modeling-principles.md` (referenciado desde `_constitution.md §4`)

---

## Principio central

> **Las fuentes de referencia (Effi, Mercately, Kommo, Siigo, Ingram, Odoo, Frappe/ERPNext) son INSUMOS, no PLANOS.** Maka ERP debe ser mejor que la suma de todos ellos. Nunca se copian campos de un pantallazo o de una tabla de otro sistema directamente al modelo. Toda información externa se **normaliza, se cuestiona y se complementa** antes de entrar al diseño.

Cuando un campo aparece en un pantallazo de Effi o en una tabla de Mercately, ese campo es **evidencia de una necesidad**, no una instrucción de diseño. La pregunta correcta nunca es "¿qué campos tenía Effi?" sino "¿cuál es la entidad correcta, normalizada, que satisface esta necesidad mejor que como lo hizo Effi?".

---

## Reglas de modelado (obligatorias en cada SPEC)

### R1 — Normalización estricta antes que fidelidad al origen
Cada campo debe pertenecer a la entidad correcta según las reglas de normalización (al menos 3FN, salvo desnormalización justificada y documentada). Si un pantallazo mostraba 30 campos en un formulario, eso NO significa 30 columnas en una tabla — significa identificar las 4-6 entidades reales que ese formulario aplana.

**Pregunta de control:** "¿este campo describe a esta entidad, o describe a una relación/sub-entidad que merece su propia tabla?"

Ejemplo aplicado (Parties): `CupoCreditoCxC` no es un campo de `Party` ni de `CustomerProfile` — es una propiedad de la relación crediticia, modelada como `CreditAccount` con historial de movimientos. El pantallazo de Effi lo mostraba como un campo; el modelo correcto es una entidad con vida propia.

### R2 — Contrastar contra Odoo y Frappe, siempre
Odoo (`res.partner`, modelo unificado) y Frappe/ERPNext (DocTypes, Party pattern) son los dos referentes más maduros y completos. Aunque no tengamos pantallazos de ellos, **todo modelo de entidad central debe contrastarse contra cómo lo resuelven estos dos** antes de cerrarse. Se investiga su código/docs reales (no de memoria), se extrae el patrón, y se adapta — divergiendo conscientemente cuando el contexto colombiano o la doctrina Maka lo justifican.

**Hallazgos ya incorporados de este contraste:**
- **Odoo `commercial_fields` + delegación al padre:** campos como NIT y cupo de crédito pueden delegarse de una sucursal/contacto a su entidad comercial matriz (`ParentPartyId`).
- **Odoo: separar jerarquía (`ParentPartyId`) de multi-tenant (`TenantId`).** Son ejes ortogonales.
- **Frappe: límites de crédito como tabla hija por compañía**, no como campo escalar.
- **Frappe: Block/Hold con tipo (Facturas/Pagos/Todo) y fecha de liberación**, no un booleano `PermitirVenta`.
- **Frappe: grupos de cliente/proveedor como catálogo (Link)**, no enum.

### R3 — Cada entidad tiene un único motivo de existir
Si una "tabla" agrupa conceptos heterogéneos (clasificación + términos comerciales + asignación operativa + reglas de precio), se parte en value objects o entidades separadas. Un `CustomerProfile` que mezcla "es mayorista" con "su vendedor es Juan" con "su cupo es $5M" con "su lista de precios es X" está mal: son 4 conceptos.

### R4 — Distinguir ejes ortogonales que los sistemas legacy mezclan
Los sistemas de referencia frecuentemente colapsan ejes independientes en un solo campo. El modelo Maka los separa:
- **Régimen de renta** (Ordinario/Simple/Especial) ≠ **Responsabilidad de IVA** (Responsable/No responsable). Son ejes independientes.
- **Jerarquía organizacional** (empresa→sucursal→contacto, `ParentPartyId`) ≠ **Tenant SaaS** (`TenantId`).
- **Clasificación comercial** (Mayorista/Minorista) ≠ **Términos comerciales** (días de crédito) ≠ **Asignación** (vendedor).
- **Identidad del tercero** (NIT, nombre) ≠ **Faceta comercial** (es cliente / es proveedor).

### R5 — Nombres sin colisión conceptual
Los nombres de entidades no deben chocar con conceptos ya usados en el sistema. Ejemplo: NO usar `CustomerRole`/`SupplierRole` porque "Role" ya significa rol de seguridad (RBAC/Identity). Se usa `CustomerProfile`/`SupplierProfile` para las facetas comerciales del tercero.

### R6 — Compliance colombiano nativo, no parcheado
Los requisitos DIAN/NIIF/legales colombianos se modelan desde el día 1 como parte de la estructura, no como campos agregados después. Resoluciones de numeración, responsabilidades fiscales, regímenes, CIIU múltiple, PEP, retenciones — todos son ciudadanos de primera clase del modelo.

### R7 — Lo que no esté en los pantallazos pero sea necesario, se añade
La ausencia de un campo/entidad en las fuentes de referencia NO es razón para omitirlo. Si Odoo/Frappe lo tienen, o si la doctrina Maka o el negocio lo requieren, se modela aunque ningún pantallazo lo mostrara. El sistema debe ser **mejor** que las referencias, lo que por definición significa tener cosas que ellas no tienen o que resuelven mal.

---

## Procedimiento de modelado por SPEC

Cada SPEC de entidad central sigue estos pasos antes de cerrarse:

1. **Recolectar la evidencia** de las fuentes (pantallazos, modelo v2) — qué necesidades revela.
2. **Contrastar contra Odoo y Frappe** — investigar su modelo real (código/docs), extraer el patrón.
3. **Identificar las entidades reales** que las necesidades implican (normalización).
4. **Separar ejes ortogonales** (R4).
5. **Aplicar normalización estricta** (R1, R3).
6. **Verificar nombres** (R5).
7. **Añadir lo que falte** para ser mejor que las referencias (R7).
8. **Documentar las divergencias** conscientes respecto a Odoo/Frappe y por qué.

---

## Historial

| Fecha | Evento |
|---|---|
| jun 2026 | Principio anclado por directriz de Juan tras revisión del SPEC Parties v1, donde se detectó mapeo directo de pantallazos sin normalización (CupoCredito mal ubicado, CustomerRole con nombre colisionante, CIIU como campo único, RST como bool separado del régimen). |

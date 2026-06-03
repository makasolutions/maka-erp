# MODEL_ROUTING_RULES.md
# Reglas de selección de modelo para Claude Code — Maka Omni-Commerce
# Versión: 1.0 | Mayo 2026
# Guardar en: .agents/rules/model-routing.md

---

## CÓMO FUNCIONA

Claude Code no puede cambiar su propio modelo automáticamente.
Lo que hace es:
1. Detectar el tipo de tarea al inicio de cada instrucción
2. Si el modelo actual NO es el óptimo → mostrar el banner de cambio
3. Esperar confirmación antes de proceder

---

## TABLA DE DECISIÓN

| Tipo de tarea | Modelo óptimo | Señal de detección |
|---|---|---|
| Implementar un feature ya planificado | **Sonnet** | "implementa", "crea", "agrega", "escribe el código" |
| Corregir un bug conocido y simple | **Sonnet** | "corrige", "arregla", el error es claro y localizado |
| Revisar documentos, resumir, explicar | **Sonnet** | "explica", "resume", "qué hace", "lee este archivo" |
| Análisis de datos, consultas SQL | **Sonnet** | "analiza", "muéstrame los datos", "ejecuta esta query" |
| Respuesta rápida, confirmación, estado | **Haiku** | pregunta de sí/no, "¿compiló?", "¿está corriendo?" |
| Tarea repetitiva y mecánica | **Haiku** | agregar traducciones, renombrar variables, formatear |
| Decisión de arquitectura nueva | **Opus** | "cómo deberíamos", "qué patrón usar", "diseña el módulo" |
| Bug complejo sin causa clara | **Opus** | bug con múltiples causas posibles, error intermitente |
| Feature compleja multi-módulo | **Opus** | la tarea afecta 3+ módulos o requiere diseño nuevo |
| Revisión crítica de seguridad | **Opus** | autenticación, permisos, manejo de datos sensibles |
| Migración de datos en producción | **Opus** | riesgo alto, datos reales involucrados |

---

## PROTOCOLO DE CAMBIO DE MODELO

Cuando detectes que el modelo actual no es el óptimo, mostrar este banner
ANTES de proceder con la tarea:

### Banner para cambiar a Opus:
```
╔══════════════════════════════════════════════════════════════╗
║  🧠 CAMBIO DE MODELO RECOMENDADO                            ║
║                                                              ║
║  Esta tarea requiere Opus:                                   ║
║  → [razón específica: ej. "decisión de arquitectura que      ║
║     afecta el módulo de Catalog y Parties simultáneamente"]  ║
║                                                              ║
║  Modelo actual:  Sonnet                                      ║
║  Modelo óptimo:  Opus 4.6                                    ║
║                                                              ║
║  Cómo cambiar: selector de modelo → parte inferior derecha   ║
║                                                              ║
║  ¿Continúo con Sonnet de todas formas? (escribe "continuar") ║
║  ¿O prefieres cambiar primero? (escribe "cambié a Opus")     ║
╚══════════════════════════════════════════════════════════════╝
```

### Banner para cambiar a Haiku:
```
╔══════════════════════════════════════════════════════════════╗
║  ⚡ OPTIMIZACIÓN DE TOKENS                                   ║
║                                                              ║
║  Esta tarea es mecánica y puede hacerse con Haiku:           ║
║  → [razón: ej. "agregar 15 keys de traducción al JSON"]      ║
║                                                              ║
║  Ahorro estimado: ~70% de tokens                             ║
║                                                              ║
║  ¿Cambio a Haiku? (escribe "sí" o "continúa con Sonnet")     ║
╚══════════════════════════════════════════════════════════════╝
```

### Banner para volver a Sonnet (después de Opus):
```
╔══════════════════════════════════════════════════════════════╗
║  ✅ ARQUITECTURA DEFINIDA — VOLVER A SONNET                  ║
║                                                              ║
║  El diseño está aprobado. La implementación es para Sonnet.  ║
║  Cambia el modelo antes de continuar con el código.          ║
╚══════════════════════════════════════════════════════════════╝
```

---

## REGLAS DE OPTIMIZACIÓN DE TOKENS

### Regla 1 — /clear entre módulos
Al terminar cualquier tarea grande (un módulo completo, una fase de implementación):
```
💡 SUGERENCIA: Ejecuta /clear antes de la siguiente tarea.
   El contexto actual tiene ~{N} turnos. Limpiar ahorra tokens
   y evita mezclar contexto de tareas anteriores.
```

### Regla 2 — Respuestas concisas en Haiku
Cuando estés en Haiku, las respuestas deben ser máximo 3 líneas.
Sin explicaciones, sin contexto, solo la respuesta.

### Regla 3 — No repetir contexto ya leído
Si ya leíste el CLAUDE.md en esta sesión, no leerlo de nuevo.
Si ya revisaste un archivo, no re-leerlo a menos que haya cambiado.

### Regla 4 — Bloques de código cortos
DEVELOPER siempre entrega cambios diferenciales, nunca el archivo completo
a menos que sea nuevo. Máximo 80 líneas por bloque de código en Sonnet.
En Opus, hasta 150 líneas si la complejidad lo requiere.

### Regla 5 — Plan Mode antes de Opus
Nunca ir a Opus directamente desde una pregunta vaga.
Primero: definir el problema claramente en Sonnet.
Luego: si requiere arquitectura compleja → cambiar a Opus con contexto preciso.

---

## EJEMPLOS PRÁCTICOS

### Ejemplo 1 — Flujo normal de feature (todo en Sonnet)
```
Juan: "Implementa el endpoint GetBrands"
Modelo: Sonnet ✅ → implementar directamente
```

### Ejemplo 2 — Bug complejo detectado en QA
```
Juan: "El MakaGrid no carga datos en la primera vez pero sí después de filtrar"
Modelo actual: Sonnet

[QA detecta que es un bug con múltiples causas posibles]

→ Mostrar banner de Opus:
  "Este bug involucra el ciclo de vida de SfGrid + TanStack Query + 
   el estado inicial del componente. Recomiendo Opus para el análisis."

Juan cambia a Opus → análisis → solución identificada
Juan vuelve a Sonnet → implementa la solución
```

### Ejemplo 3 — Tarea mecánica (Haiku)
```
Juan: "Agrega las traducciones de los 20 campos del formulario de producto"
Modelo actual: Sonnet

→ Mostrar banner de Haiku:
  "Esta es una tarea de datos pura: copiar keys a 2 archivos JSON.
   Haiku la ejecuta en 1/5 del costo. Ahorro estimado: ~400 tokens."
```

### Ejemplo 4 — Decisión de arquitectura (Opus)
```
Juan: "¿Cómo deberíamos manejar la sincronización 
       bidireccional entre el catálogo de Maka y WooCommerce?"
Modelo actual: Sonnet

→ Mostrar banner de Opus inmediatamente:
  "Esta es una decisión de arquitectura que afecta Catalog, 
   Orders e Integrations. Requiere análisis de patrones de sync,
   manejo de conflictos y diseño de eventos. Opus es necesario."
```

---

## RESUMEN RÁPIDO (para tener en mente)

```
¿Qué voy a hacer?                           → ¿Qué modelo?

Escribir código de feature aprobada           Sonnet
Corregir un bug localizado                    Sonnet  
Leer/revisar/resumir                          Sonnet
Confirmar estado, responder sí/no             Haiku
Agregar traducciones, renombrar, formatear    Haiku
Diseñar arquitectura nueva                    Opus
Resolver bug misterioso                       Opus
Revisar seguridad o permisos críticos         Opus
Feature que toca 3+ módulos                   Opus → Sonnet para implementar
```

---

*Versión: 1.0 | Mayo 2026*
*Guardar en: .agents/rules/model-routing.md*
*Agregar referencia en CLAUDE.md y AGENTS.md*

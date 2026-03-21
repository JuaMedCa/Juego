# Revisión de código y mejoras para la administración del proyecto

## Resumen ejecutivo

El proyecto mezcla **código de gameplay propio** con **assets de terceros** y scripts legacy. Para mejorar la administración (mantenimiento, escalabilidad y trabajo en equipo), recomiendo priorizar:

1. **Separar código propio vs. vendor** para reducir ruido en revisiones.
2. **Estandarizar arquitectura y nombres** (ej. `PlayerMovemnt` está mal escrito).
3. **Fortalecer validaciones y configuración en Inspector** para evitar errores en runtime.
4. **Agregar pruebas automáticas mínimas** (EditMode) sobre lógica de movimiento/cambio de cámara.
5. **Definir flujo de trabajo** (PR template, checklist técnico y convención de carpetas).

---

## Hallazgos clave en código

### 1) Scripts vacíos o incompletos
- `MouseLookFPS` está vacío (sin comportamiento). Esto incrementa deuda técnica y confusión sobre qué sistema controla la cámara en FPS.  
  Archivo: `Assets/Camera/MouseLookFPS.cs`

### 2) Dependencia frágil en `Camera.main`
- `PlayerMovemnt` calcula direcciones isométricas usando `Camera.main` en runtime. Si cambia el tag/cámara activa, puede romper movimiento.  
  Archivo: `Assets/Camera/PlayerMovemnt.cs`

### 3) Falta de validaciones de null
- `CameraSwitchTrigger` asume referencias válidas de cámaras y player movement.  
- `SimpleCameraFw` asume `target` siempre asignado.  
- `IsoCameraReset` asume existencia de `CinemachineFramingTransposer`.

### 4) Naming y consistencia
- Typo en clase `PlayerMovemnt` (debería ser `PlayerMovement`).
- Mezcla de estilos (camelCase/PascalCase, inglés/español, comentarios y nombres).

### 5) Input acoplado al sistema legacy
- Se usa `Input.GetAxis` directamente en múltiples scripts (`PlayerMovemnt`, `CharController_Motor`). Esto dificulta migrar a Input System y complica testing.

### 6) Código legacy no encapsulado
- Scripts como `WaterFloat`, `DisableRenderer`, `FPSDisplay`, `CharController_Motor` son utilitarios legacy y no están claramente separados por módulo/feature.

---

## Plan recomendado (priorizado)

## Fase 1 (rápida, 1–2 días)
- Eliminar o implementar `MouseLookFPS`.
- Añadir guard clauses y `Debug.LogWarning` en `Awake/Start` para referencias críticas.
- Renombrar `PlayerMovemnt` (incluyendo archivo, clase y referencias en escenas/prefabs).
- Centralizar configuración en `[SerializeField] private` + propiedades de solo lectura cuando aplique.

## Fase 2 (estructura, 3–5 días)
- Reorganizar carpetas:
  - `Assets/_Project/Scripts/Gameplay/...`
  - `Assets/_Project/Scripts/Camera/...`
  - `Assets/_ThirdParty/...` (vendor, sin tocar salvo actualización)
- Definir `asmdef` por módulo (`Gameplay`, `Camera`, `Core`) para compilar más rápido y aislar dependencias.
- Introducir capa simple de input (`IPlayerInput`) para desacoplar lectura de ejes.

## Fase 3 (calidad continua, 1 semana)
- Añadir pruebas EditMode para:
  - cálculo de dirección en modo iso/FPS,
  - transición Enter/Exit FPS,
  - restauración de parámetros en `IsoCameraReset`.
- Configurar checklist de PR:
  - referencias en inspector validadas,
  - no uso de `Camera.main` en loops,
  - no scripts vacíos,
  - naming consistente.

---

## Reglas de administración sugeridas

- **No mezclar** cambios funcionales con reorganización de assets masivos en un mismo PR.
- **Cada feature** debe incluir:
  - objetivo funcional,
  - impacto en escena/prefab,
  - validación manual mínima,
  - riesgo y rollback.
- **Versionado interno** de escenas importantes (changelog corto por escena).
- **Dueño por módulo** (Camera, Player, Environment) para acelerar decisiones.

---

## Métricas de seguimiento (KPIs simples)

- Tiempo promedio de revisión por PR.
- Número de errores por referencias no asignadas en runtime.
- Cantidad de scripts sin uso o vacíos.
- Tiempo de compilación en Editor (antes/después de asmdef).

---

## Resultado esperado

Aplicando este plan, el proyecto gana:
- mejor mantenibilidad,
- menos regresiones por configuración manual,
- revisiones más rápidas,
- y una base más sólida para escalar features.

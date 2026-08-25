---
id: kd_builtin_memory_project_mistake_note
injectMode: full
aiEditMode: auto
maintenanceRules: |-
  - Record only verified problems, rework causes, and avoidance steps
  - Prioritize recurring pitfalls, constraints, regression points, and confirmed fixes
  - Keep each entry short and focused on one lesson or constraint
  - Keep the list within 20 items and merge duplicates regularly
  - Remove outdated issues, non-reproducible issues, and unsupported guesses
---

- SafeAreaFitter white-screen freeze (fixed): `OnRectTransformDimensionsChange` fires synchronously when Apply() writes anchors, so Apply -> callback -> Apply recursed forever and froze the Editor on Play (Boot -> MainMenu BuildUI). Avoidance: any RectTransform write inside `OnRectTransformDimensionsChange` must have a re-entrance guard (compare against last-applied value and early-return).


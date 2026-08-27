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
- Editor-script prefab creation right after adding new .cs files can serialize MonoBehaviour references with the placeholder GUID `fe87c0e1cc204ed48ad3b37840f39efc` (missing script), because `MonoScript.FromMonoBehaviour(type).GetAssetPath()` returns empty for classes not yet mapped in the asset database — even when the project compiles. Multi-class .cs files may map only their first class. Avoidance: one UI component per .cs file, and before building prefabs probe each type via `MonoScript.FromMonoBehaviour` and require a non-empty asset path.
- `Assets/Art/UI/*.png` are imported as textureType=Default (spriteMode=None), so `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/...")` returns null and the Image renders as a white square. The real UI sprites live at `Assets/Resources/UI/<same name>.png`. Avoidance: when assigning UI sprites in editor scripts, always load from `Assets/Resources/UI/`, or copy the sprite reference from an existing Image component.


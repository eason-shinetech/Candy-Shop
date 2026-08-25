---
id: kd_builtin_memory_user_preference
injectMode: rule
aiEditMode: auto
maintenanceRules: |-
  - Record only long-term user preferences that stay stable across tasks
  - Prioritize language, reporting style, code style, taboos, and explicit requirements
  - Keep each entry short and limited to stable preferences or hard constraints
  - Keep the list within 20 items and merge similar preferences
  - Remove one-off arrangements, temporary phrasing, and unconfirmed inferences
---

- Candy types follow the Art/Candy kit prefabs: one playable prefab under `Assets/Prefabs/Candy` = one `CandyTypeId`. Do not invent types from a handmade list or by re-parsing `Assets/Art/Candy` FBX meshes independently of those prefabs.
- Candy UI icons (order chips, recipe rows, thumbs) follow `Assets/Art/Candy Icon/<PrefabName>.png` (same stem as the prefab). Do not treat generated `Resources/UI/Candies` or family fallbacks as the source of truth.

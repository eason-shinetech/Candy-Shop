# Candy Shop — Documentation Index

Cursor owns **design and plan documents**. OpenCode owns **Unity implementation**. Do not treat this repo's `docs/` as code to compile.

| Document | Audience | Purpose |
| --- | --- | --- |
| [Game Design Spec](superpowers/specs/2026-08-24-candy-shop-design.md) | Design + OpenCode | Rules, economy, UI, VFX, orientation. Source of truth for *what* to build. |
| [Art Bible](superpowers/specs/2026-08-24-candy-shop-art-bible.md) | Art + OpenCode | Cartoon-cute style lock and image-generation prompts. Source of truth for *how assets look*. |
| [Supplements](superpowers/specs/2026-08-24-candy-shop-supplements.md) | Design + OpenCode | Extra play/UI/UX. **§1 = MVP**. §2 = backlog, skip. |
| [Unity UI + Impeccable](superpowers/specs/2026-08-24-candy-shop-ui-impeccable.md) | Cursor + OpenCode | Use [Impeccable](https://github.com/pbakaus/impeccable) as design skill for uGUI. Skip HTML detector / live browser. |
| [Unity UI plugins](superpowers/specs/2026-08-24-candy-shop-unity-plugins.md) | OpenCode | [UI Effect](https://github.com/mob-sakai/UIEffect) + [Tutorial Spotlight](https://assetstore.unity.com/packages/tools/gui/tutorial-spotlight-363804). |
| [i18n zh/en](superpowers/specs/2026-08-24-candy-shop-i18n.md) | OpenCode | Bilingual UI. Keys + English/Chinese table. |
| [Implementation Plan](superpowers/plans/2026-08-24-candy-shop-mvp.md) | OpenCode | File layout, task order, acceptance checks. Source of truth for *how* to build. |

**Start here for implementation:** read the spec first, then execute the plan task by task.

## Constraints for OpenCode

- Target: Unity **6000.0.77f1**, 3D URP, **Android only** (touch), **portrait only**. No iOS. Do not use another Editor version.
- All C# comments must be English.
- UI is **zh + en** ([i18n](superpowers/specs/2026-08-24-candy-shop-i18n.md)). No hardcoded player-facing strings.
- Do not invent conflicting game rules. If a number is missing, use the spec defaults and keep it data-driven (ScriptableObject).
- Candy types follow the Art/Candy kit prefabs: one playable prefab under `Assets/Prefabs/Candy` = one `CandyTypeId` (scenery props excluded by name). `Assets/Art/Candy` is the 3D kit those prefabs come from — do not invent types from a handmade list, and do not re-parse `candy_kit.fbx` independently of the prefabs.
- Candy UI icons (order chips, recipe rows, thumbs) follow `Assets/Art/Candy Icon`: one PNG per type, filename = prefab name (e.g. `Chocolate Bar.prefab` → `Chocolate Bar.png`). Do not treat generated `Resources/UI/Candies/icon_candy_*` or family fallbacks as the source of truth. Scenery in that folder (`Stick`, `Icecream Plate`, `Lollipop Ground *`) is not a catalog thumb.
- Visual style is **卡通可爱风**. Prefix every image-generation prompt with the art-bible block. Do not mix styles.
- Unity UI: follow [Impeccable adapter](superpowers/specs/2026-08-24-candy-shop-ui-impeccable.md) + root `PRODUCT.md` / `DESIGN.md`. Art bible wins over generic Impeccable SaaS looks.
- UI motion: [UI Effect](https://github.com/mob-sakai/UIEffect). First-run tutorial: [Tutorial Spotlight](https://assetstore.unity.com/packages/tools/gui/tutorial-spotlight-363804). Do not write a custom dim overlay.
- Reserve ad interface (`IAdService` stub). Restock power-up = coins AND ad. Use is free if count > 0. Other ads opt-in. No auto-play during picking.
- Daily stamina **20**; spend 1 per **current** guest; perfect +1 (max 5/day) / pass +0 / fail −3. No stamina ads. Spec §8.2.

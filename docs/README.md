# Candy Shop — Documentation Index

Cursor owns **design and plan documents**. OpenCode owns **Unity implementation**. Do not treat this repo's `docs/` as code to compile.

| Document | Audience | Purpose |
| --- | --- | --- |
| [Game Design Spec](superpowers/specs/2026-08-24-candy-shop-design.md) | Design + OpenCode | Rules, economy, UI, VFX, orientation. Source of truth for *what* to build. |
| [Art Bible](superpowers/specs/2026-08-24-candy-shop-art-bible.md) | Art + OpenCode | Cartoon-cute style lock and image-generation prompts. Source of truth for *how assets look*. |
| [Supplements](superpowers/specs/2026-08-24-candy-shop-supplements.md) | Design + OpenCode | Extra play/UI/UX. **§1 = MVP**. §2 = backlog, skip. |
| [Implementation Plan](superpowers/plans/2026-08-24-candy-shop-mvp.md) | OpenCode | File layout, task order, acceptance checks. Source of truth for *how* to build. |

**Start here for implementation:** read the spec first, then execute the plan task by task.

## Constraints for OpenCode

- Target: Unity **6000.0.77f1**, 3D URP, **Android only** (touch), **portrait only**. No iOS. Do not use another Editor version.
- All C# comments must be English.
- Do not invent conflicting game rules. If a number is missing, use the spec defaults and keep it data-driven (ScriptableObject).
- Candy models live under `Candy/` (currently meta files are present; confirm binaries exist before scene assembly).
- Visual style is **卡通可爱风**. Prefix every image-generation prompt with the art-bible block. Do not mix styles.
- Reserve ad interface (`IAdService` stub). Restock power-up = coins AND ad. Use is free if count > 0. Other ads opt-in. No auto-play during picking.

# Candy Shop — Unity UI plugins

**Date:** 2026-08-24  
**Editor:** Unity **6000.0.77f1**, uGUI + TMP, URP, Android portrait.

Do **not** write a custom fullscreen dimmer or a from-scratch button tween library. Use the two packages below.

| Plugin | URL | Use for |
| --- | --- | --- |
| **UI Effect** | https://github.com/mob-sakai/UIEffect | Button press, panel show/hide, HUD juice (grayscale, dissolve, shine, fade) |
| **Tutorial Spotlight** | https://assetstore.unity.com/packages/tools/gui/tutorial-spotlight-363804 | First-run tutorial: dark overlay + clickable hole on the target control |

Gameplay 3D particles (Magnet / Tornado / Freeze) stay **Unity Particle System** prefabs. UI Effect does not replace those.

---

## 1. UI Effect (mob-sakai)

**Install (OpenCode):** Package Manager → Add package from git URL:

```
https://github.com/mob-sakai/UIEffect.git?path=Packages/src
```

If that path fails on 6000.0.77f1, use the repo’s current UPM instructions. Do not copy random forks.

**Required uses:**

| UI event | UI Effect (typical) | Notes |
| --- | --- | --- |
| Button click (开始营业, 购买, 道具, 领取) | Press scale + short shine / highlight | Ease out-cubic. **No bounce/elastic** (Impeccable + DESIGN.md) |
| Panel / sheet show (签到, 金币不足, 暂停, 购买道具, 教程 tip) | Fade in and/or dissolve in | Also animate `CanvasGroup.alpha` if needed; keep one owner so they don’t fight |
| Panel / sheet hide | Fade / dissolve out, then `SetActive(false)` | |
| Locked recipe row | Slight grayscale | Unlock: grayscale → 0 + sparkle |
| Wrong pick HUD | Brief color / shake via effect, not a second tween stack | Spec already has haptic + star loss |
| Disabled power-up (count 0 before sheet) | Soft grayscale or dim | |

Wrap a small `UiEffectPlayer` if the API is verbose; call it from HUD/menu scripts. Comments in English.

Do **not** put UI Effect on the 3D pile.

---

## 2. Tutorial Spotlight (Asset Store)

**Package:** [Tutorial Spotlight](https://assetstore.unity.com/packages/tools/gui/tutorial-spotlight-363804) (id `363804`).

**Install:** User must add the asset to their Unity account, then OpenCode imports it into the project (do not vendor a pirated copy). Default import location is fine; do not rewrite the plugin.

**What it does:** Fullscreen dark overlay with a **rectangular hole** around a UI target. Clicks **pass through the hole only**; the rest of the screen is blocked. Optional finger / tap graphic with pulse. API (from publisher): `TutorialSpotlightManager` — `ShowSpotlight`, `HideSpotlight`, `SetSpotlightTarget`. Shader is mobile-friendly.

**Do not** implement a second overlay (custom Image + mask). First-run onboarding **must** use this plugin.

### 2.1 Flow (replaces generic “3 cards only”)

On first `开始营业`, if `tutorialDone == false`:

1. Load Game scene, **timer paused**.
2. Three spotlight steps (tip from i18n `tutorial_1`…`tutorial_3`; skip `tutorial_skip` from step 2):

| Step | Hole target | Tip |
| --- | --- | --- |
| 1 | Order-chip row **and** a `PileHotspot` RectTransform covering the pile on screen | `客人要的糖，点堆里的就可以` |
| 2 | Stars | `点错会扣星星；完美接待可以补回一颗（最多三颗）` |
| 3 | Power-up thumb row | `道具有库存就能用；没了要花金币并看广告补充` |

3. Advance when the player taps **inside the hole** (or 下一步 if a control is display-only).
4. `HideSpotlight`, set `tutorialDone = true`, **then** start the first customer timer.

`PileHotspot`: empty `RectTransform` aligned to the pile’s screen bounds so the hole lets **3D candy raycasts** through. Do not block world picks inside the hole.

Skip 跳过: hide spotlight, set `tutorialDone`, start timer (same as finishing step 3).

Finger graphic: **on** for steps 1 and 3 (tap). Pulse allowed **only** on the finger, not on every HUD widget.

Overlay color: cocoa/black at ~55–70% alpha so the pile still reads as candy shop, not a horror dim.

### 2.2 Later (not required)

Do not use Spotlight for daily challenge or ads. Those stay toasts/sheets.

---

## 3. OpenCode checklist

- [ ] UI Effect in `Packages/manifest.json`
- [ ] Tutorial Spotlight imported from Asset Store
- [ ] First run uses `TutorialSpotlightManager`, not a homemade dimmer
- [ ] Buttons/panels use UI Effect, not ad-hoc `LeanTween`/`DOTween` as the **primary** click/show system (a tiny helper on top of UI Effect is OK)
- [ ] No bounce/elastic; art bible colors

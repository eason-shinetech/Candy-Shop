# Candy Shop MVP — Implementation Plan (OpenCode)

> **Handoff:** Cursor writes docs only. OpenCode implements in Unity. Do not skip the spec.

**Goal:** Ship a portrait-only **Android** candy-shop game in Unity **6000.0.77f1** (3D URP, no iOS).

**Architecture:** ScriptableObject data + scene MonoBehaviours. Save is one JSON file. Power-ups always play particle VFX. UI and generated images follow the art bible.

**Tech stack:** Unity **6000.0.77f1**, URP 3D, Android player, Input System or `Input.GetTouch` + UI raycasts, TextMeshPro (zh + en), local JSON.

**Read first:**

- [Game Design Spec](../specs/2026-08-24-candy-shop-design.md)
- [Art Bible](../specs/2026-08-24-candy-shop-art-bible.md)
- [i18n zh/en](../specs/2026-08-24-candy-shop-i18n.md)

## Global constraints

- **Unity Editor = 6000.0.77f1** (`C:\Program Files\Unity\Hub\Editor\6000.0.77f1\`). Do not use another version. 3D URP, **Android only** (do not install or configure iOS), touch, **portrait only**.
- UI copy in **zh and en** ([i18n spec](../specs/2026-08-24-candy-shop-i18n.md)). All **code comments in English**.
- Style: **卡通可爱风**. Every generated image uses the art-bible prompt prefix. No mixed styles.
- Power-ups Magnet / Tornado / Freeze each have a particle prefab; silent use is a bug.
- Wrong candy: remove object, minus 1 star. 3 stars. Timer 0 or 0 stars = Game Over.
- Persist coins, recipes, daily streak, **and daily stamina** locally.
- Daily stamina: **20/day**; −1 when a guest becomes current; perfect +1 (max 5 refunds/day) / pass +0 / confirmed fail −3. Spec §8.2.
- Reserve `IAdService` (stub in MVP). Ads are opt-in only; see spec §14.
- Numbers: copy from the spec; expose on ScriptableObjects for tuning.

---

## File map (create these)

```
Assets/Scenes/BootScene.unity
Assets/Scenes/MainMenu.unity
Assets/Scenes/GameScene.unity
Assets/Scenes/RecipeShopScene.unity
Assets/Scripts/Bootstrap/BootLoader.cs
Assets/Scripts/Game/GameManager.cs
Assets/Scripts/Game/Candy/CandyInstance.cs
Assets/Scripts/Game/Candy/CandyPickController.cs
Assets/Scripts/Game/Candy/CandyPileRestock.cs
Assets/Scripts/Game/Orders/CustomerOrderManager.cs
Assets/Scripts/Game/Orders/CustomerOrderState.cs
Assets/Scripts/Game/PowerUps/PowerUpManager.cs
Assets/Scripts/Game/PowerUps/PowerUpVfxPlayer.cs
Assets/Scripts/Economy/EconomyManager.cs
Assets/Scripts/Save/SaveDataModel.cs
Assets/Scripts/Save/SaveDataService.cs
Assets/Scripts/Daily/DailySignInService.cs
Assets/Scripts/Daily/StaminaService.cs
Assets/ScriptableObjects/StaminaConfig.cs
Assets/Scripts/Ads/IAdService.cs
Assets/Scripts/Ads/StubAdService.cs
Assets/Scripts/Ads/AdPlacement.cs
Assets/ScriptableObjects/AdConfig.cs
Assets/Scripts/I18n/I18nService.cs
Assets/I18n/strings_zh.json
Assets/I18n/strings_en.json
Assets/Scripts/UI/GameHUDController.cs
Assets/Scripts/UI/MainMenuController.cs
Assets/Scripts/UI/RecipeShopController.cs
Assets/Scripts/UI/SafeAreaFitter.cs
Assets/Scripts/UI/UiEffectPlayer.cs
Assets/Scripts/UI/FirstRunTutorialDriver.cs
Assets/ScriptableObjects/CandyTypeDefinition.cs
Assets/ScriptableObjects/RecipeDefinition.cs
Assets/ScriptableObjects/PowerUpDefinition.cs
Assets/ScriptableObjects/CustomerOrderConfig.cs
Assets/ScriptableObjects/EconomyConfig.cs
Assets/Data/*.asset
Assets/Prefabs/VFX/Vfx_Magnet.prefab
Assets/Prefabs/VFX/Vfx_Tornado.prefab
Assets/Prefabs/VFX/Vfx_Freeze.prefab
Assets/Art/UI/ ... (see art bible)
```

Copy or import `Candy/` kit into `Assets/Art/Candy/` if Unity will not import outside `Assets/`.

---

### Task 1: Unity project + portrait lock

- Open Unity **6000.0.77f1** only (`C:\Program Files\Unity\Hub\Editor\6000.0.77f1\Editor\Unity.exe`). Set `ProjectSettings/ProjectVersion.txt` to `6000.0.77f1`.
- Create **3D URP** project in `D:\Projects\Unity\Candy Shop` if missing (open with that Editor).
- Switch platform to **Android**. Do not add iOS build support.
- Player Settings (Android): Portrait only; disable landscape and autorotate.
- `BootLoader` sets `Screen.orientation = Portrait` and turns off landscape autorotate.
- Game view / canvas 1080 x 1920, match height, Safe Area.
- Scenes in Build Settings: Boot → MainMenu → Game → RecipeShop.

**Done when:** Android device or editor Game view stays portrait if you rotate to landscape. No iOS target in Build Settings.

### Task 2: Art pass (style-locked 2D)

- Generate all files in the art bible **in one batch** with the same prompt prefix and palette.
- Import as UI sprites. No baked Chinese/English in PNGs (TMP + i18n draws text).
- Reject any asset that looks realistic or from another style.

**Done when:** Main menu background + HUD icons + 6 portraits sit in `Assets/Art/UI`, and **one thumb per catalog candy** sits in `Assets/Art/Candy Icon` (`<PrefabName>.png`), all looking like one kit.

### Task 3: Candy pile mapping

- Import the Art/Candy kit into `Assets/Art/Candy/` (FBX/GLB). Playable prefabs live in `Assets/Prefabs/Candy/`. If binaries are missing, greybox spheres **temporarily**, tagged with `CandyTypeId`, and write a short `MISSING_ASSETS.md` in `docs/`.
- Build catalog from **prefabs**, not the FBX mesh list: **each pickable candy prefab = one type = one recipe** (3 starters free). Write `docs/generated/candy-catalog.md`. Exclude scenery by name (`plate`, `ground`, `stick`, …).
- Bind each type's UI icon from `Assets/Art/Candy Icon/<PrefabName>.png`.
- `CandyInstance` on each pickable prefab: id, collider, hide/remove API.
- Click: touch raycast to `CandyInstance` (ignore UI).

**Done when:** Catalog size equals pickable candy prefabs; tapping a candy logs its type and hides it; chips/recipe rows use the Candy Icon PNG.

### Task 4: Orders, timer, stars

- Implement order generation (1–3 types, 6–30 count, unlocked types only) and timer formula from spec.
- Correct tap decrements; wrong tap removes candy and −1 star.
- 0 stars or timer 0 → Game Over. Serve complete → coins → next customer **only if stamina ≥ 1**.
- Stars persist for the whole run. Stamina spend/settle is Task 5b but wire the hooks here so a guest becoming current calls `StaminaService`.

**Done when:** A playtest can serve 2+ customers and fail both ways.

### Task 5: Economy, save, daily sign-in, recipes

- JSON save fields from spec (including `stamina`, `staminaDate`).
- Boot: daily +500, streak, 7-day recipe +5 stamina, all-unlocked +500, **stamina refresh to 20 on a new local date**.
- Daily featured-recipe challenge (§8.1): roll type, 12/12 progress, bias, shop 20% off if locked, reward 120 + 1 Freeze.
- Recipe shop: one recipe per non-starter catalog candy (spec §4). Do not hardcode 7 names.
- Serve reward formula from spec.

**Done when:** Kill the app and reopen: coins/recipes/streak/**stamina** persist; claiming twice the same day does not double 200; a new calendar date refills stamina to 20.

### Task 5b: Daily stamina (体力)

- `StaminaConfig`: dailyMax 20, cost 1, perfect +1, pass 0, fail −3.
- `StaminaService`: clamp 0–20; spend **only** when a guest becomes current (not waiting queue); save immediately.
- Main Menu: `n/20`; 开始营业 blocked at 0 → empty sheet (i18n). Generate `icon_stamina.png` per art bible.
- Serve: perfect +1 if under daily refund cap / pass +0. If then stamina < 1 → **Shift Over** (no revive, no fail −3).
- Fail (stars/timer) or confirmed 放弃本局: −3 on leave without revive. Revive: same guest, no second spend, no −3.
- HUD floating text: `体力-1` / `体力+1` / `体力-3`.
- No stamina ads, no timed regen, no overflow.

**Done when:** 20 → start guest → 19; perfect → 20; pass stays 19; fail-and-leave drops by 4 total (clamp 0); empty stamina cannot start a run; waiting portraits never spend.

### Task 6: Power-ups + VFX + ads stub

- Persistent inventory counts on save. New save: 1 of each.
- Tap: if count > 0, use immediately (no ad, no extra coins, no per-run cap).
- If count == 0: buy sheet = **deduct coins AND rewarded ad**; refund coins if ad skipped; then auto-use.
- Insufficient coins: 看广告+80, then buy still needs coins+ad.
- Magnet / Tornado / Freeze gameplay + VFX from spec.
- `IAdService` + `StubAdService` + `AdConfig` (spec §14). Serve double / revive / daily extra unchanged.

**Done when:** Using with count > 0 never shows an ad; restocking at 0 cannot complete without both payment and stub-ad success; skip refunds coins.

### Task 7: HUD / menus polish

- Chinese **and English** copy from [i18n](../specs/2026-08-24-candy-shop-i18n.md). Language toggle in Settings. `I18nService` + both JSON files. TMP CJK fallback.
- Thumb-zone power-up buttons. Cookie-like panels from art bible.
- Apply [Impeccable Unity adapter](../specs/2026-08-24-candy-shop-ui-impeccable.md) and root `DESIGN.md` (TMP rounded CJK, 8px grid, no nested cards, no Arial/Inter, no bounce easing).
- Import **UI Effect** (git UPM) and **Tutorial Spotlight** (Asset Store 363804). First-run uses `TutorialSpotlightManager`; buttons/panels use UI Effect. See [plugins](../specs/2026-08-24-candy-shop-unity-plugins.md).
- Optional: `npx impeccable install --providers=opencode` so later UI passes use `/impeccable polish` on **Game-view screenshots**, not HTML.
- Pause freezes timer without Freeze VFX.
- Ship [supplements](../specs/2026-08-24-candy-shop-supplements.md) **§1**: tutorial, front-most pick, buried hint, perfect **star restore + stamina +1**, daily 今日配方, **stamina HUD / Shift Over**, visual combo, best score, haptics, confetti, 7-dot streak.
- Do **not** build supplements §2 backlog.

**Done when:** Full loop: menu → run → shop → sign-in popup → game over → menu, all portrait and cute.

### Task 8: Manual QA

- Spec section 15 + art-bible checklist.
- Magnet only takes remaining required candies.
- Restock at 0 requires coins **and** stub ad; skip refunds coins.
- Use with count > 0 never plays an ad.
- Serve double is opt-in and skippable; no auto-play during picking.
- Tornado reveals buried candies.
- Freeze pauses timer only.
- Landscape does not reflow.
- Stamina: new day = 20; current guest −1; perfect +1; pass 0; leave-without-revive −3; Shift Over vs Game Over are different screens.

---

## Out of scope (do not implement)

iOS, landscape, IAP, live AdMob SDK (stub is in scope), cloud save, extra power-ups, full 3D walking customers.

---

## OpenCode working rule

If the spec and this plan disagree, **the spec wins**. If art and a generated image disagree, **the art bible wins**. If Impeccable generic defaults fight the candy shop, **art bible + DESIGN.md win**. UI motion = **UI Effect**; first-run tutorial = **Tutorial Spotlight**. Tune numbers only via ScriptableObjects, not hardcoded magic, unless the spec gave a constant (e.g. daily 200).

# Candy Shop — Game Design Spec

**Date:** 2026-08-24  
**Status:** Approved for OpenCode implementation (MVP)  
**Engine:** Unity **6000.0.77f1** (3D URP). Do not use a different Editor version.  
**Platform:** Android only (no iOS), **portrait only**  
**Input:** Touch tap (no mouse-only desktop layout required)  
**Persistence:** Local device only (JSON file, not PlayerPrefs for the full save blob)  
**Language of UI copy:** **Chinese (`zh`) and English (`en`)**. See [i18n](2026-08-24-candy-shop-i18n.md).  
**Language of code comments:** English

This document is the source of truth for gameplay rules. The implementation plan describes file layout and task order. Do not change these rules without updating this spec.

---

## 1. One-sentence pitch

You run a candy shop. Customers queue with mixed candy orders. You tap candies out of a 3D pile before the customer's timer hits zero. Wrong candy costs a star. Coins buy recipes and in-run power-ups.

---

## 2. Platform and presentation

| Rule | Value |
| --- | --- |
| Unity Editor | **6000.0.77f1** only. Typical path: `C:\Program Files\Unity\Hub\Editor\6000.0.77f1\Editor\Unity.exe`. `ProjectSettings/ProjectVersion.txt` must be `m_EditorVersion: 6000.0.77f1`. Template: **3D (URP)**. Do not open or generate the project with another Editor. |
| Target | **Android only.** Do not add an iOS module, iOS Player Settings, Xcode project, or App Store checklist. |
| Build | Android APK (or AAB if asked later). Editor Game view 1080 x 1920 is the layout reference. |
| Orientation | Portrait only. Landscape is **not** supported. |
| Player Settings | Android: `Default Orientation = Portrait`. Disable Auto Rotation, Landscape Left, Landscape Right. `Allowed Orientations for Auto Rotation` = Portrait only. |
| Runtime lock | On boot: `Screen.orientation = ScreenOrientation.Portrait`; `autorotateToLandscapeLeft/Right = false`. |
| Reference aspect | 9:16. Also layout for 9:19.5 / 9:20 with Safe Area (Android punch-hole / gesture nav). |
| Camera | Fixed 3D camera looking at the candy pile. No free orbit in MVP. Optional tiny idle sway is allowed; player cannot rotate the world. |
| UI canvas | Screen Space Overlay, reference resolution 1080 x 1920, match height. |
| Landscape | No landscape layouts, no orientation-change reflow, no "please rotate" overlay that unlocks landscape. If the OS still reports landscape, keep rendering portrait (letterbox if needed). |
| Art style | **卡通可爱风 (cartoon cute)**. All generated 2D images, UI, icons, VFX color, and customer portraits must share one style bible. Do not mix realistic, dark, pixel-art, or cyberpunk looks. |

### 2.1 Art direction (mandatory for every generated image)

**One style for the whole game.** If an asset looks like it belongs to a different game, it is rejected.

Use this **prompt prefix** on every image-generation request (keep it identical):

```
Cute cartoon candy-shop game art, kawaii chibi, soft rounded shapes,
thick clean outlines, pastel candy colors, glossy sugar highlights,
warm bakery lighting, cheerful and wholesome, high readability at small size,
no realism, no horror, no photoreal textures, no text unless specified.
Style must match a children's mobile game UI kit.
```

| Token | Value |
| --- | --- |
| Mood | Cheerful, sweet, safe for kids, candy store |
| Shapes | Round, squishy, oversized heads / icons, no sharp military geometry |
| Line | Medium-thick clean vector-like outline (not sketchy, not ink wash) |
| Color | Pastel + candy accents. See palette below. Max 6 colors per icon plus white highlight |
| Lighting | Soft studio / bakery window. Pink-warm key light. Specular sugar shine, not PBR metal |
| Characters | Chibi 2–3 heads tall, big eyes, simple mouths, no realistic anatomy |
| Forbidden | Photoreal, grimdark, blood, horror, noisy textures, thin-line fashion illustration, pixel art, low-poly unlit cubes as final art, mixed styles in one atlas |

**Palette (hex, use these names in prompts):**

| Name | Hex | Use |
| --- | --- | --- |
| Cream | `#FFF6E8` | Panels, cards |
| Sugar Pink | `#FF8FB8` | Primary buttons, hearts |
| Berry | `#E85A8C` | Pressed / accent |
| Sky Mint | `#7EE0C6` | Positive, serve success |
| Lemon | `#FFE07A` | Coins, stars, timer OK |
| Cocoa | `#6B3F2A` | Text on light panels (or dark brown outline) |
| Grape | `#A78BFA` | Secondary chips |
| Ice | `#B8E8FF` | Freeze |
| Magnet Red | `#FF6B6B` | Magnet (still cute, not industrial) |
| Wind | `#C8F5D4` | Tornado |

**UI chrome:** rounded rectangles (corner radius large), candy-stripe or frosting borders, soft drop shadow. Buttons look like iced cookies, not flat Material Design.

**3D candy kit:** keep the imported `Candy/` models if they already read as cute candy. Do not retarget them to realism. Lighting in GameScene: warm, slightly saturated, no harsh realistic shadows.

**Particles:** same palette (pastel sparkles, hearts, sugar dust, rounded blobs). No photoreal smoke or fire.

Full prompt sheets and file list: [Art Bible](2026-08-24-candy-shop-art-bible.md).

---

## 3. Core loop

```
Boot → load save → daily sign-in + stamina refresh → Main Menu
  → Start Run (requires stamina ≥ 1)
    → current customer starts → spend 1 stamina
    → player taps candies
    → order complete → coins → settle stamina (perfect +1 / pass +0)
         → if stamina ≥ 1: next customer
         → if stamina < 1: Shift Over (tired) → Main Menu
    → fail (0 stars or timer 0) → Game Over (optional revive)
         → leave without revive → fail −3 stamina → Main Menu
  → Recipe Shop (from Main Menu; spends coins)
```

A **run** is one continuous serving session: stars start at 3, customers keep coming until **fail** (0 stars or timer 0) or **stamina cannot start the next guest**. There is no fixed "level count" in MVP. Difficulty scales with customers served in the current run.

Daily **stamina (体力)** is the session cap: **20 per local date**. Full rules: **§8.2**.

---

## 4. Candy types and recipes

### 4.1 Source of truth = the 3D kit (not a made-up list)

Recipe count is **not** a fixed 7 or 10. It is derived from `Candy/Premium` after Unity imports the models.

**Rule:** every distinct **pickable candy mesh** in the kit is one `CandyTypeId` and one recipe (except the 3 starters, which are unlocked for free and have no shop recipe).

| Include as a candy type | Exclude |
| --- | --- |
| Each unique mesh (or mesh + material variant) in `candy_kit.fbx` / `candy_kit.glb` that is a candy prop | `terrain_kit`, `cloud_kit` |
| Extra candy materials in the folder (`Chocolate.mat`, `Waffer.mat`) if they belong to distinct candy meshes | Shop furniture, terrain, clouds, lights, cameras |
| Color / shape variants that are **separate objects** in the FBX (e.g. Candy_setA vs Av2 vs Balloon_A) | Duplicate instances of the **same** mesh used only to fill the pile |

Pile instances: many copies of `lollipop_red` in the scene still count as **one** type / **one** recipe.

Hint from current texture names (not a final count — count meshes after import):

- Atlases: `Candy_setA`, `Av2`, `Av3`, `Candy_setB`, `Bv2`, `Candy_setC`–`G`
- `Balloon_A` / `B` / `C`
- `Chocolate`, `Waffer`

OpenCode **must** after import:

1. List candy meshes (Editor script `CandyCatalogBuilder` is fine).
2. Sort by hierarchy name.
3. Write `docs/generated/candy-catalog.md` with: mesh name, proposed `CandyTypeId`, starter or recipe, cost.
4. Create one `CandyTypeDefinition` asset per row and one `RecipeDefinition` per non-starter row.

If FBX/GLB binaries are missing, stop and list missing files. Do not keep the old 10-name placeholder catalog as production data.

### 4.2 Starters vs recipes

- **Exactly 3 starters**, always free. Default: first 3 candy meshes in sorted name order. If `Chocolate` and `Waffer` meshes exist, prefer those two plus the first remaining mesh as the 3 starters (still exactly 3).
- **Every other candy mesh = one shop recipe** that unlocks that type.
- Orders may only request **unlocked** types (starters + bought recipes).
- Display names: **zh and en** on each catalog row (i18n). Do not show raw FBX names in UI.

### 4.3 Recipe prices

Let `R` = number of non-starter candies. Recipe `i` (0-based, cheapest first, same sort as catalog):

```
cost(i) = 120 + i * 60
```

Example: 7 recipes → 120, 180, 240, 320 would be wrong; use the formula (120, 180, 240, 300, 360, 420, 480).

- Cannot buy an already unlocked recipe.
- If the player cannot afford it: show cost in red; offer 看广告+80 (spec §14), not a free unlock.
- Unlock is instant and persisted.
- Daily streak-7 still unlocks the **cheapest remaining** recipe. All-unlocked **+500** uses this full catalog (`R` recipes all owned).

### 4.4 Scene pile

Tag each scene instance with the `CandyTypeId` of its source mesh. The Game scene contains a **pre-placed pile** of those meshes (many copies per type).

- Tapping a candy **removes** it from the pile (correct or wrong).
- When a type runs out in the pile, that type cannot be picked until restock.
- **Restock:** when a customer is served **or** when remaining pickable candies of a requested type drop to 0, refill missing instances of **unlocked** types so the pile never starves a valid order. Restock may lerp new candies in from above (simple drop). Do not restock mid-pick of a single candy.
- Do not spawn locked (not yet bought) types into the pile.

---

## 5. Customers and orders

### 5.1 Queue

- Visible queue: **current customer + 2 waiting** (3 portraits / speech-bubble slots).
- Off-screen pool: generate the next customer as soon as the current one is served so the queue always looks full.
- **Stamina is spent only when a guest becomes current**, not when a waiting/off-screen guest is generated. Waiting portraits are preview only (see §8.2).
- Customers are simple 3D or 2D portraits in the HUD. MVP: HUD cards are enough; full walking NPCs are optional polish, not required.

### 5.2 Order generation

For each customer:

1. `typeCount` = random integer in **[1, 3]**, clamped to the number of unlocked candy types.
2. Choose `typeCount` distinct unlocked types uniformly, then apply **daily challenge bias** if the featured type is unlocked (spec §8.1: 70% include featured).
3. `totalCandies` = random integer in **[6, 30]**.
4. Split `totalCandies` across the chosen types. Every chosen type gets **at least 1**. Remaining units distributed uniformly at random.
5. Scale slightly with run progress (optional, data-driven): after every 5 served customers, bias `totalCandies` toward the upper half of the range. Never exceed 30.

Order UI shows icons + remaining counts, e.g. `棒棒糖 x4  软糖 x8`.

### 5.3 Per-customer timer (independent)

Each customer has a **private countdown**. Serving a customer discards that timer and starts a new one for the next customer.

**Default formula** (all values on `CustomerOrderConfig`):

```
timeSeconds = baseSeconds + totalCandies * secondsPerCandy
timeSeconds = Clamp(timeSeconds, minSeconds, maxSeconds)
```

Defaults:

| Field | Default |
| --- | --- |
| `baseSeconds` | 6 |
| `secondsPerCandy` | 1.15 |
| `minSeconds` | 10 |
| `maxSeconds` | 45 |

Examples: 6 candies ≈ 12.9s → clamp 12.9; 30 candies ≈ 40.5s.

HUD shows remaining time as a bar + integer seconds. Below 5 seconds: bar turns red and pulses.

**Timer 0:** immediate Game Over (do not serve a partial order, do not award coins for that customer).

**Freeze power-up:** pauses this countdown only. Does not pause candy physics/VFX/input except as needed for Freeze VFX.

---

## 6. Stars and picking

| Rule | Value |
| --- | --- |
| Stars at run start | 3 |
| Stars persist | Across the whole run (not reset per customer) |
| Wrong tap | −1 star, **remove** the tapped candy from the pile |
| Correct tap | Remaining count for that type −1, remove candy |
| Stars hit 0 | Immediate Game Over |
| Perfect serve | 0 wrong picks this customer: extra **+5 coins**, stamp `完美`, and **restore 1 star** if stars < 3 |
| Already-taken candy | Ignore (no double deduct) |
| Tap empty space / UI | No star change |
| Tap a type not in the current order | Wrong (even if that type is unlocked) |
| Tap a type in the order whose remaining is already 0 | Wrong |

There is no "undo". There is no extra penalty beyond 1 star per wrong candy.

**Serve success:** when every required count is 0, the customer is satisfied. If `wrongPicksThisCustomer == 0` (perfect): grant +5 coins (not doubled by ad), stamp `完美`, `stars = min(3, stars + 1)` with star-fill juice, and **stamina +1** (cap `dailyMax`, see §8.2). Pass (served with ≥1 wrong pick): stamina **+0**. Then award the normal speed coins, show the reward chip (optional 看广告翻倍 on the **speed reward only**, see §14). Then, if stamina ≥ 1, slide queue and make the next guest current (that start spends 1 stamina). If stamina < 1, **Shift Over** (§8.2) instead of spawning. The double-reward button must not auto-play an ad.

---

## 7. Economy

### 7.1 Coin wallet

- Single persistent `coins` integer on the save file.
- In-run power-up **buys** spend this same wallet (use does not).
- Coins never go below 0. Failed spend is a no-op + UI shake, then offer **看广告获得金币** if the ad service is ready (see §14). Never auto-play the ad.

### 7.2 Reward for serving a customer

```
speedRatio = remainingTime / totalTime          // 0..1
reward = round(baseReward
        + totalCandies * perCandy
        + speedRatio * speedBonusMax)
reward = max(reward, minReward)
```

Defaults:

| Field | Default |
| --- | --- |
| `baseReward` | 8 |
| `perCandy` | 1 |
| `speedBonusMax` | 24 |
| `minReward` | 10 |

Faster clears pay more. HUD pops `+N` over the coin icon. A small **看广告翻倍** action sits on that chip (opt-in, skippable, see §14).

### 7.3 Game Over (stars / timer fail)

- No coin penalty on Game Over.
- Coins already earned and spent this run stay as saved.
- Show: customers served, coins earned this run, remaining stars, remaining stamina.
- Optional **看广告再试一次** (revive, once per run). No run-total double on this screen (per-serve double is enough). See §14.
- **Fail stamina −3** applies when the player **leaves** this screen without a successful revive (see §8.2). Revive continues the **same** current guest; do not spend another stamina and do not apply −3.

**Shift Over** (stamina cannot start the next guest after a successful serve) is **not** this screen: no revive, no fail −3. See §8.2.

---

## 8. Daily sign-in

Evaluated once per app session on Boot, after save load, using **device local calendar date** (`yyyy-MM-dd`).

| Event | Reward |
| --- | --- |
| First launch of a new local date | **+200 coins** |
| Consecutive days opened (streak) | `dailyStreak` += 1, cap display at 7 |
| Missed a day (last sign-in date is not yesterday and not today) | streak resets to 1 (today counts) |
| Same date already claimed | no extra 200 |
| `dailyStreak` reaches **7** on a claim | unlock **one** not-yet-owned recipe (cheapest remaining). If none remain, skip recipe and still apply the all-unlocked bonus if eligible |
| After a claim, if **all recipes** are unlocked and `allRecipesBonusClaimed` is false | **+500 coins**, set flag true |

Streak 7 does **not** auto-reset to 0 in MVP. After 7, further daily 200 still applies; the recipe reward fires only when streak **becomes** 7 (not every day after). Optional later: reset streak after 7. MVP: `recipeGrantedForThisSevenCycle` — grant recipe only on the transition to 7; next days do not grant more recipes from streak until streak is broken and rebuilt to 7 again.

Show a sign-in panel on Main Menu if a reward was granted this boot (coins and/or recipe and/or 500 bonus). Player taps 领取 / 关闭. Optional extra: **看广告再领 +50** once per day (see §14). Never auto-play.

### 8.1 Daily featured-recipe challenge (MVP)

Uses the same local date as sign-in. One featured `CandyTypeId` per day, chosen from the **full catalog** (starters + every recipe candy).

**Pick rule:** `hash(yyyy-MM-dd) % catalogCount`. If catalog length > 1 and the result equals yesterday’s featured id, take the next index. Persist `dailyChallengeTypeId` and `dailyChallengeDate`. On a new local date: reset `dailyChallengeProgress` to 0 and `dailyChallengeClaimed` to false, then roll.

**Goal:** **12 correct picks** of that type today (Magnet auto-picks count). Wrong taps of that type do not add progress. Progress is saved across runs. Quota `N=12` is on `DailyChallengeConfig`.

**If the type is locked:**

- Orders still cannot request it (existing unlock rule).
- Main Menu + Recipe Shop mark the row `今日配方` and sell it at **20% off** for that date only (`round(cost * 0.8)` to nearest 10, min 80).
- Copy: `解锁后才能完成今日挑战`.
- Discount does not skip the normal buy (still coins only in the shop — no extra ad for recipes).

**If the type is unlocked** (including after they buy it today):

- Order generation **bias:** 70% chance the featured type is one of this customer’s 1–3 types (still only from unlocked types). Do not make every order 100% that candy.
- HUD chip (under order chips): featured icon + `进度 5/12`.
- On reaching 12: toast `今日挑战完成`, grant **+120 coins** and **+1 Freeze** (inventory). Once per date (`dailyChallengeClaimed`). No ad. Do not pause picking more than a toast.

Main Menu banner: `今日配方：{name}  {progress}/12` or `已完成`. Tapping the banner opens Recipe Shop if locked, otherwise starts 开始营业 (still blocked if stamina < 1, same as the start button).

Do not roll a new type mid-day. Do not require a separate challenge scene.

### 8.2 Daily stamina (体力)

Session cap so a day cannot be an infinite shift. Same **device local calendar date** as sign-in (`yyyy-MM-dd`). Numbers live on `StaminaConfig` (ScriptableObject). Do not hardcode except as these defaults.

| Field | Default | Meaning |
| --- | --- | --- |
| `dailyMax` | **20** | Pool size and daily refresh target |
| `costPerCustomer` | **1** | Spent when a guest **becomes current** |
| `perfectRefund` | **+1** | After a perfect serve |
| `passDelta` | **+0** | After a non-perfect serve |
| `failPenalty` | **−3** | After a confirmed fail (not on revive) |

Always clamp: `stamina = Clamp(stamina, 0, dailyMax)`.

#### Refresh

Evaluated on Boot (with sign-in), when entering Main Menu, and when the player taps 开始营业 — **not** mid-customer.

| Event | What happens |
| --- | --- |
| `staminaDate` ≠ today (including first launch / missing field) | `stamina = dailyMax` (20), `staminaDate = today`. Does **not** add 20 on top of leftover. Yesterday’s leftover is discarded. |
| Same local date | Keep persisted `stamina` |
| Mid-run past midnight | Do **not** refill until Main Menu or next Boot |

New save: `stamina = 20`, `staminaDate = today`.

#### Spend (customer start)

When a guest **becomes the current** customer (first guest of a run, or the next guest after a serve):

1. If `stamina < costPerCustomer` (1): **do not** make them current.
   - From Main Menu: do not load Game scene; show the empty-stamina sheet.
   - After a serve: **Shift Over** (below).
2. Else: `stamina -= 1`, save immediately, pop a small `体力-1` on the stamina icon.

Waiting / off-screen queue entries cost **nothing** until they become current. Do not spend 3 at run start for the visible queue.

Tutorial’s first guest still costs 1 (new players have 20).

#### Settle (customer end)

| Result | When | Stamina |
| --- | --- | --- |
| **Perfect** | Served and `wrongPicksThisCustomer == 0` | **+1** (then clamp). Show `体力+1` with the `完美` stamp. |
| **Pass** | Served and at least one wrong pick | **+0**. No extra stamina juice. |
| **Fail** | Timer 0, stars 0, or confirmed 放弃本局 — and the player **does not** revive | **−3** (then clamp). Show `体力-3`. |

Net after a finished guest (before clamp):

| Result | Net vs the start-spend |
| --- | --- |
| Perfect | −1 + 1 = **0** (can continue at the same pool if you started at cap: 20 → 19 → 20) |
| Pass | −1 + 0 = **−1** |
| Fail | −1 + (−3) = **−4**, floor 0 |

Worked examples (OpenCode, treat as acceptance):

| Start stamina | Event | End stamina |
| --- | --- | --- |
| 20 | Guest starts | 19 |
| 19 | Perfect settle | 20 |
| 20 | Guest starts, then pass | 19 |
| 19 | Guest starts, then fail and leave (no revive) | 15 |
| 1 | Guest starts, then pass | 0 → Shift Over, cannot start next |
| 1 | Guest starts, then perfect | 1 → can start next |
| 2 | Guest starts, then fail and leave | 0 |
| 0 | Tap 开始营业 | Stay on menu, empty sheet; stamina stays 0 until next local date |

**Revive** (`reward_revive`): the fail is **not** confirmed. Same current guest continues. No second start-spend. No −3. Later perfect/pass/fail-and-leave uses the table above once.

Apply **−3 once** when fail is confirmed:

- Game Over → 回到主菜单 (revive unused, skipped, already used this run, or ad not ready)
- Pause → 放弃本局 → confirm
- Do **not** apply −3 if they revive successfully
- Do **not** apply −3 on Shift Over (they already served; they are out of energy, not failed)

#### Shift Over vs Game Over

| Screen | Cause | Revive | Fail −3 | Coins |
| --- | --- | --- | --- | --- |
| Game Over `营业结束` | 0 stars or timer 0 | Yes, once per run if ready | Yes, on leave without revive | Keep |
| Shift Over `打烊休息` | After a **successful** serve, `stamina < 1` so the next guest cannot start | No | No | Keep |

Both persist coins and update `bestCustomersServed` if the serve count beat best (Shift Over counts served guests the same way).

#### Main Menu gate

- Show `体力 n/20` near coins.
- 开始营业 with `stamina < 1`: stay on menu, sheet title/body from i18n (`stamina_empty_*`). No ad, no coin buy, no waiting timer regen in MVP.
- Grey/disable 开始营业 when empty; tapping still opens the same sheet (do not silently ignore).

#### HUD

- Game HUD: stamina `n/20` next to coins (icon + number, never color-only). Same candy-dot language as power-up badges.
- Start-spend and perfect/fail deltas are floating text on that icon.

#### Out of scope for stamina (do not add)

Watch-ad +stamina, coin-buy stamina, regen over minutes, overflow above `dailyMax`, stamina cost on power-ups or recipe shop.

---


## 9. Power-ups

HUD buttons during a run. Inventory is **persistent** (`magnetCount` / `tornadoCount` / `freezeCount` on the save file).

### 9.0 Use vs buy

| Situation | What happens |
| --- | --- |
| Count **> 0** | Tap = **use immediately**. Consume 1. No ad. No extra coin. No per-run cap. Can use every customer, including several times in one order if count allows. |
| Count **== 0** (道具不够) | Tap does **not** use. Open the **buy sheet**. Purchase requires **coins AND a rewarded ad**, both. Grant +1 only if coins were deducted **and** the ad completed. |

There is **no** “coins only” buy and **no** “ad only / 看广告使用” channel.

**Buy sheet (count == 0 or player taps +):**

1. Show: 购买 磁铁 +1 / 价格 / 「需观看广告」.
2. If coins < cost: 金币不足 → 看广告+80 / 取消. After +80 they still must complete **buy** (coins + purchase ad).
3. If coins >= cost and purchase ad is ready: 购买 → deduct coins → `ShowRewarded(reward_powerup_buy_*)`.
   - Ad completed: `count += 1`, save, then **auto-use** that unit (they tapped to use). Net: paid, watched, effect plays, count back to 0 if they started at 0.
   - Ad skipped / failed: **refund coins**, count unchanged, no effect.
4. If purchase ad is not ready: 广告还没准备好. Do not deduct coins.

Optional **"+"** on the badge: same buy rule even when count > 0 (stockpile). Purchase of extra **does not** auto-use; only tap-on-empty auto-uses.

New save: grant **1 of each** once (`starterPowerUpsGranted`) so the first uses need no ad.

Cannot activate Tornado/Freeze if that type is already active, or if the run is over. Magnet with 0 remaining required candies: do not consume a charge.

| Id | Name (ZH) | Buy cost (coins) | Also requires ad | Gameplay |
| --- | --- | --- | --- | --- |
| `magnet` | 磁铁 | 50 | Yes, on **buy** only | Auto-remove up to 3 remaining **required** candies. Counts as correct picks. |
| `tornado` | 龙卷风 | 40 | Yes, on **buy** only | 4s lift / orbit so buried candies are tappable. |
| `freeze` | 冰冻 | 35 | Yes, on **buy** only | 5s pause on the customer timer. Input still works. |

### 9.1 Particle VFX (required)

**Every power-up must play a dedicated particle effect. Silent activation is a bug.**

| Power-up | Prefab | Look | Lifetime |
| --- | --- | --- | --- |
| Magnet | `Assets/Prefabs/VFX/Vfx_Magnet.prefab` | Cute sparkles + pink-red suction stars (not industrial metal filings) toward each pulled candy | Burst; destroy after particle lifetime |
| Tornado | `Assets/Prefabs/VFX/Vfx_Tornado.prefab` | Pastel swirl, mint leaves / sugar dust, rounded wind ribbons around the pile | Loop for tornado duration, then stop |
| Freeze | `Assets/Prefabs/VFX/Vfx_Freeze.prefab` | Soft ice crystals, snow-sparkle, baby-blue mist (kawaii frost, not blizzard horror) | Loop for freeze duration, then stop |

VFX rules:

- Unity Particle System, URP particles, mobile budget **≤ ~200 particles** per prefab.
- `PowerUpDefinition.vfxPrefab` is required. Missing prefab → log error; still apply gameplay so the run is not soft-locked, but treat as a blocker before ship.
- World space. Readable in 9:16.
- Tornado/Freeze VFX stop in the same frame the gameplay effect ends (including Game Over).

---

## 10. Screens and UI (portrait)

**Look:** cartoon-cute (art bible) + Impeccable hierarchy (no AI-SaaS slop). OpenCode must read `PRODUCT.md`, `DESIGN.md`, [Unity UI + Impeccable](2026-08-24-candy-shop-ui-impeccable.md), and [Unity plugins](2026-08-24-candy-shop-unity-plugins.md) before building Canvases. Use uGUI + TMP. **UI Effect** for button/panel motion. **Tutorial Spotlight** for first-run holes. Do not run `npx impeccable detect` on Unity assets.

### 10.1 Boot

Splash or empty camera. Load save. Apply orientation lock. Load Main Menu.

### 10.2 Main Menu

- Title: 糖果店
- Buttons: 开始营业 / Open Shop, 配方商店 / Recipes, 设置 / Settings (音乐·Music / 音效·Sound / 振动·Vibration / **语言 Language**)
- All labels from [i18n table](2026-08-24-candy-shop-i18n.md), not hardcoded.
- Coins **and stamina** `n/20` top-right (stamina icon + number)
- `历史最佳：服务 N 位客人`
- Banner `今日配方` (see §8.1)
- Sign-in popup when needed; streak as **7 dots**
- If stamina < 1: 开始营业 looks disabled; tap opens empty-stamina sheet (§8.2). Do not enter Game scene.
- Tutorial on first 开始营业 if `tutorialDone` is false — **Tutorial Spotlight** (see plugins spec §2), not a homemade overlay. Tutorial still requires stamina ≥ 1 (new saves have 20).

### 10.3 Recipe Shop

- List of recipes: icon (catalog thumb), **localized** name, cost, 购买·Buy / 已解锁·Unlocked
- If this row is today’s featured candy: badge `今日配方`; if still locked, show **20% off** price (§8.1)
- Locked rows slightly grey; scrollable; after buy, sparkle + persist
- Back to Main Menu

### 10.4 Game HUD (top → bottom, portrait)

1. Top safe area: stars (3), **stamina n/20**, coins, pause
2. Customer queue strip (current highlighted)
3. Order chips (icon + remaining number, never color-only). Punch the chip on a matching pick. Daily-challenge chip: featured icon + `n/12`.
4. Center: 3D pile (largest region)
5. Timer bar under the top cluster or above the pile
6. Bottom thumb zone: 磁铁 / 龙卷风 / 冰冻 with **count badge**. Tap uses if count > 0. If 0, buy sheet (金币 + 看广告). Optional + to stockpile.
7. Coin-insufficient sheet and serve-success chip (翻倍) — player-initiated only

Pause: 继续, 放弃本局 (confirm: `真的要打烊吗？当前客人会失败（体力-3），本局星星和进度会结束`), 音乐/音效/振动. Pause **freezes** the customer timer. Pause is not Freeze (no Freeze VFX). Confirmed quit applies fail stamina −3 (§8.2) then Main Menu.

Timer starts after tutorial is dismissed on the first run.

### 10.5 Game Over and Shift Over

**Game Over** (0 stars or timer 0):

- 营业结束
- `本局服务 {n} 位` / `赚到 {coins} 金币` / `历史最佳 {best}`；new best → `新纪录`
- Remaining stamina; if they leave without revive, fail −3 applies on 回到主菜单 (show `体力-3` on that icon so the cost is obvious)
- 回到主菜单 (applies fail −3 if not revived)
- 看广告再试一次 (revive, hide if already used this run or ad not ready)
- Do not auto-start any ad on this screen

**Shift Over** (successful serve, next guest blocked by stamina < 1):

- 打烊休息
- Same served / coins / best / 新纪录 lines
- 回到主菜单 only. No revive. No fail −3.

### 10.6 Feedback

- Correct tap: candy flies to the matching chip, chip punch, light haptic; combo floating text `连击 xN` (visual only, no extra coins)
- Wrong tap: camera or HUD shake, star decrement, medium haptic, candy still removed
- Perfect serve (0 wrong this customer): +5 coins, stamp `完美`, restore **1 star** if below 3 (star-fill juice), **stamina +1** (cap 20). The +5 is **not** doubled by the ad.
- Serve: small pastel confetti (≤80 particles)
- After 8s with no correct pick this customer: toast `找不到？用龙卷风翻一翻` (once per customer)
- Not enough coins: button shake, then the insufficient-coins sheet (ad is opt-in)
- World tap ignores HUD; pick the **front-most** candy under the finger
- Finger drag > ~40px: not a pick

---

## 11. Audio (MVP placeholders)

Optional clips; mute via settings. If clips are missing, ship silent rather than blocking.

| Event | Intent |
| --- | --- |
| Correct tap | Soft pop |
| Wrong tap | Error thud |
| Serve customer | Register ding |
| Game Over | Short downer |
| Power-up | Match VFX (whoosh / ice / magnet) |
| BGM | Light shop loop, duck on Game Over |

---

## 12. Save data (local JSON)

Path: `Application.persistentDataPath/candy_shop_save.json`

```json
{
  "schemaVersion": 1,
  "coins": 0,
  "unlockedRecipeIds": [],
  "dailyStreak": 0,
  "lastSignInDate": "",
  "allRecipesBonusClaimed": false,
  "adsWatchedDate": "",
  "adsWatchedCountToday": 0,
  "dailyCoinAdClaimedDate": "",
  "magnetCount": 1,
  "tornadoCount": 1,
  "freezeCount": 1,
  "starterPowerUpsGranted": true,
  "tutorialDone": false,
  "bestCustomersServed": 0,
  "musicEnabled": true,
  "sfxEnabled": true,
  "hapticsEnabled": true,
  "language": "zh",
  "dailyChallengeDate": "",
  "dailyChallengeTypeId": "",
  "dailyChallengeYesterdayId": "",
  "dailyChallengeProgress": 0,
  "dailyChallengeClaimed": false,
  "stamina": 20,
  "staminaDate": ""
}
```

Starter candy types are **not** stored as recipes; they are always unlocked in code.

Missing `stamina` / `staminaDate` on an old save: treat as a new date → refresh to 20 for today (§8.2).

No cloud, no account.

---

## 13. Asset notes

**2D generated art** must follow [Art Bible](2026-08-24-candy-shop-art-bible.md). Generate as a set, not one-off unrelated images. Prefer one atlas pass (same seed/style prompt) for icons.

Folder `Candy/` currently contains Unity `.meta` files and two materials (`Waffer.mat`, `Chocolate.mat`). Expected binaries (`candy_kit.fbx` / `.glb`, textures `.png`, `terrain_kit`, `cloud_kit`) may be missing from git.

OpenCode must:

1. Confirm binaries exist on disk after Unity refresh.
2. If missing, stop scene assembly and document the missing files; do not fake candies with cubes except as a **temporary** greybox tagged with the same `CandyTypeId` so systems can be tested.

Move or copy imported kits into `Assets/Art/Candy/` if Unity requires assets under `Assets/`. Keep original `Candy/` as source if that is the artist drop folder; the plan covers the copy step.

---

## 14. Ads (reserved interface, light UX)

MVP **does not** ship a live AdMob/Unity Ads SDK. OpenCode must still implement `IAdService` so a real network can be plugged in later without rewriting UI.

**UX law (do not violate):**

- Ads are **opt-in**. Player taps a clearly labeled button. Never auto-play on boot, on customer spawn, or between candy taps.
- **No ads during picking** unless the player just tapped an ad/power-up sheet.
- While a rewarded ad is showing: pause the customer timer (same as Pause). Resume only after success or cancel.
- First run: hide **optional** ads (double, daily extra, revive, coin pack) until the player has finished **one** Game Over. **Power-up restock ads still show** when count is 0 — that buy is player-initiated and required.
- If `IsReady` is false: hide or grey the ad button; do not show a broken “watch ad” that does nothing.
- Editor / stub: simulate a 0.8s delay then `onCompleted(true)` so flows can be tested without a network.

### 14.1 Interface

```
IAdService
  bool IsReady(AdPlacement placement)
  void ShowRewarded(AdPlacement placement, Action<bool> onRewarded)
  void ShowInterstitial(AdPlacement placement, Action onClosed)
```

`StubAdService` is the MVP implementation. `AdMobAdService` (or similar) is out of MVP; keep the interface.

`AdPlacement` enum / ids:

| Id | Scene | Reward if completed |
| --- | --- | --- |
| `reward_coins` | Recipe shop and power-up / insufficient-coins **sheets** only (no always-on ad button on the pile HUD) | **+80 coins** |
| `reward_double_serve` | After a successful customer | Add **the same coin amount again** for that customer only |
| `reward_powerup_buy_magnet` | Buy sheet when restocking Magnet | Completes the purchase (with coins already deducted). **Does not** grant a free use by itself |
| `reward_powerup_buy_tornado` | Buy sheet when restocking Tornado | Same |
| `reward_powerup_buy_freeze` | Buy sheet when restocking Freeze | Same |
| `reward_daily_extra` | Sign-in popup | **+50 coins**, once per local date |
| `reward_revive` | Game Over | Restore **1 star**; if fail was timeout, restore that customer's remaining order and full timer; if fail was 0 stars, set stars to 1 and keep current order. Resume the run. Once per run. **Does not** spend stamina again and **does not** apply fail −3 |
| `interstitial_after_run` | After Game Over → Main Menu | None. **Disabled by default** (`AdConfig.interstitialEnabled = false`) |

### 14.2 Frequency caps (`AdConfig`)

| Cap | Default | Why |
| --- | --- | --- |
| Min seconds between optional rewarded ads | 45 | Does **not** apply to `reward_powerup_buy_*` (player already paid) |
| Max optional rewarded ads per local date | 8 | Coin pack / double / daily / revive. **Power-up buy ads have a separate cap** |
| Max `reward_powerup_buy_*` per date (all types summed) | 6 | Restock ceiling |
| Max `reward_coins` per date | 4 | Coin printer |
| Max `reward_double_serve` per run | 3 | Not every customer |
| Max revive per run | 1 | Forgiveness, not infinite |
| `reward_daily_extra` | 1 per date | On top of free 200 |
| Interstitial | off | Do not interrupt |

If a cap is hit, hide the button. Do not nag.

Coin grant from ads still goes through `EconomyManager` and is saved immediately.

### 14.3 Required placements (player-requested)

1. **Coins not enough** — sheet, not a full-screen takeover. Watching grants +80. Player then taps Buy again if they still want the item.
2. **Success double** — after each served customer, chip: `+N` + **看广告翻倍**. Auto-continue to the next customer in **2.5s** if ignored. Does not block tapping if they already skipped. After 3 doubles this run, hide 翻倍.
3. **Power-up restock** — when count is 0, **coins AND rewarded ad**. Using while count > 0 has no ad and no extra cost.

### 14.4 Extra placements (allowed, still light)

4. **Daily extra +50** on the sign-in panel only.
5. **Revive once** on Game Over (看广告再试一次). Not on Pause. Not after 放弃本局 confirm.

Do **not** add: ads on every use when count > 0, ads when opening the recipe list, ads on pause, unskippable interstitials between customers, rewarded ads that unlock recipes for free, “watch ad to use for free”.

### 14.5 Copy

Use i18n keys (`ad_buy_cta`, `ad_double`, …). zh/en in [i18n spec](2026-08-24-candy-shop-i18n.md). Do not hardcode.

---

## 15. Out of MVP (do not build now)

- iOS / iPhone / App Store
- Landscape
- Multiplayer / leaderboards / IAP
- Live AdMob / Unity Ads SDK (stub + `IAdService` **is** in MVP)
- Interstitials (interface reserved, flag off)
- Cloud save
- Walking 3D customers with full animation set
- More than the three specified power-ups
- Combo **coin** multiplier beyond speed-bonus (visual 连击 is in MVP)
- Narrative / dialogue trees
- Stamina refill via ads, coins, or timed regen; stamina overflow above 20

---

## 16. Acceptance (product)

A build is MVP-complete when:

1. Unity **6000.0.77f1**, Android portrait-only on device or editor Game view 1080x1920. No iOS build target.
2. A run can serve several customers, fail on stars or timer, end on **stamina Shift Over**, and persist coins **and stamina**.
3. Wrong candy removes the object and deducts exactly one star.
4. Daily 200 / streak-7 recipe / all-unlocked 500 behave as specified.
5. All three power-ups work and each shows its particle VFX.
6. Recipe shop lists every non-starter candy from the imported kit (1 mesh = 1 recipe); unlocks then appear in later orders.
7. UI copy is **zh and en** (i18n table + language toggle); comments in code are English.
8. All 2D UI, icons, customer portraits, and particles match the cartoon-cute art bible (same palette, outlines, kawaii shapes). Mixed or realistic assets fail review.
9. `IAdService` + stub: insufficient coins → opt-in +80; serve success → opt-in double; **restock power-up = coins AND ad**; **use is free if count > 0**; no auto-play during picking.
10. Tutorial once; front-most pick; UI blocks world taps; perfect serve **+5 coins, +1 star (cap 3), and +1 stamina (cap 20)**; daily featured-recipe challenge 12/12; best-serve on menu; haptics toggle. See also [supplements](2026-08-24-candy-shop-supplements.md) §1.
11. Daily stamina **20/20** refreshes on a new local date; each **current** guest costs 1; perfect +1 / pass +0 / confirmed fail −3; cannot start a run or the next guest at 0; no stamina ads.

---

## 17. Extra UX (MVP)

Ship [supplements](2026-08-24-candy-shop-supplements.md) **§1** only. Do not ship §2 backlog.

- First-run **Tutorial Spotlight** (3 holes); timer starts after dismiss.
- Raycast front-most candy; HUD eats taps; drag is not a pick.
- Buried hint toast once per customer after 8s without a correct pick.
- Visual combo only; perfect serve +5 coins **and +1 star if below 3** **and +1 stamina (cap 20)**; serve confetti.
- Daily featured-recipe challenge (§8.1): 12 correct picks, 70% order bias if unlocked, 20% off if locked, reward 120 coins + 1 Freeze.
- Daily stamina (§8.2): 20/day, −1 on current guest start, perfect +1 / pass +0 / fail −3; menu `n/20`; Shift Over when empty after a serve.
- Main menu best score + 7-day streak dots + 今日配方 banner; settings 音乐/音效/振动/**语言**.
- **zh / en** i18n ([i18n spec](2026-08-24-candy-shop-i18n.md)); toggle applies immediately.
- Subtle pile idle jiggle; low-time vignette under 5s.
- Recipe unlock toast `新糖果上架` next run.
- Game Over shows 本局 / 历史最佳 / 新纪录; Shift Over when stamina blocks the next guest.


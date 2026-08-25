# Candy Shop — Design supplements (play / UI / UX)

**Date:** 2026-08-24  
**How to use:** §1 is **in MVP** (OpenCode should ship these). §2 is **backlog** (do not build unless asked). If this file and the main spec disagree, the **main spec wins** after the MVP items below are merged.

Related: [Game Design Spec](2026-08-24-candy-shop-design.md)

---

## 1. Add to MVP (cheap, high value)

These keep the core loop (tap pile → serve → coins) and only add clarity, fairness, and juice.

### 1.1 Fairness (mis-tap)

- Raycast the **front-most** candy (closest hit to camera). Do not use a random overlapping collider.
- UI consumes the tap (`IPointerClickHandler` / GraphicRaycaster). World pick ignores taps over HUD.
- If the finger moves more than ~40px before up, treat as not a pick (scroll-safe even if the camera is fixed).
- Buried candy: after **8s with no correct pick** this customer, show a one-line toast: `找不到？用龙卷风翻一翻` (once per customer). Not an ad. Hide if they have 0 tornado and 0 coins to restock — still show, so they learn the verb.

### 1.2 First-run tutorial (once) — Tutorial Spotlight

Use **[Tutorial Spotlight](https://assetstore.unity.com/packages/tools/gui/tutorial-spotlight-363804)** (`TutorialSpotlightManager.ShowSpotlight` / `HideSpotlight` / `SetSpotlightTarget`). Dark overlay + hole; taps only through the hole. Do not ship a custom fullscreen mask.

Three steps, skip 跳过 on 2 and 3. Save `tutorialDone`. Timer starts **after** the last step (or skip). Full targeting table: [Unity plugins](2026-08-24-candy-shop-unity-plugins.md) §2.

1. `客人要的糖，点堆里的就可以` (hole: order chips + pile hotspot)
2. `点错会扣星星；完美接待可以补回一颗（最多三颗）` (hole: stars)
3. `道具有库存就能用；没了要花金币并看广告补充` (hole: power-up row)

### 1.3 Serve juice

- Correct pick: candy flies to the matching chip; chip punch; **light haptic**.
- Wrong pick: short shake; star pops; **medium haptic**.
- **Combo (visual only):** consecutive correct picks show `连击 x2` … floating text. **No extra coins** (economy stays the speed formula). Combo breaks on wrong or on serve.
- **Perfect serve:** 0 wrong picks this customer → extra **+25 coins**, stamp `完美`, restore **1 star** if stars < 3, and **stamina +1** if under the daily perfect-refund cap (max 5/day, spec §8.2). Speed-reward 看广告翻倍 does not double the +25.
- Serve: small confetti burst (pastel, ≤80 particles) + current portrait happy frame.

### 1.4 Meta on Main Menu

- `历史最佳：服务 N 位客人` (max customers in one run)
- Coins, **stamina n/20**, streak **7 dots** (filled = days this cycle)
- Settings: 音乐 / 音效 / **振动** (three toggles)
- After buying a recipe: toast `新糖果上架：{name}` on next Game scene load (once)
- Banner `今日配方` (progress / 已完成). Tap: Recipe Shop if locked, else 开始营业 (same stamina gate as the start button)

### 1.10 Daily featured-recipe challenge

Full rules: main spec **§8.1**. Quota 12 correct picks, 70% order bias when unlocked, 20% off in shop when locked, reward +450 coins and +1 Freeze, once per local date.

### 1.11 Daily stamina (体力)

Full rules: main spec **§8.2**. 20 per local date. Spend 1 when a guest becomes **current**. Perfect +1 (max **5** refunds/day) / pass +0 / confirmed fail −3. Clamp 0–20. Menu gate at 0. After a successful serve at 0 → Shift Over (no revive, no fail −3). Waiting queue portraits do not spend stamina.

### 1.5 Recipe shop UX

- Row: icon (catalog thumb), localized name, cost, 购买 / Buy / 已解锁 / Unlocked
- Locked rows slightly grey, still readable
- Scroll on portrait; do not shrink text below 28px
- After purchase, play a short icing sparkle on that row

### 1.6 HUD readability

- Order chips: **icon + remaining number** (never color-only)
- Current customer card larger; waiting two dimmed
- Timer `< 5s`: bar already red; add a **soft** screen-edge vignette (not a full red flash)
- Power-up badge count in a candy-dot; `0` shows a small `+`
- Stamina `n/20` next to coins (icon + number)
- Pause **放弃本局** already needs confirm; copy: `真的要打烊吗？当前客人会失败（体力-3），本局星星和进度会结束`

### 1.7 Pile feel

- Very small idle jiggle / sugar sparkle on the pile root (subtle, always on)
- Restock drop from above (already in spec)
- Magnet: pulled candies fly to chips (same as correct pick)

### 1.8 Game Over

- `本局服务 {n} 位` / `赚到 {coins} 金币` / `历史最佳 {best}`
- If `n` beat best, badge `新纪录`
- Primary: 回到主菜单. Secondary: revive ad (existing). Leaving without revive applies stamina fail −3.
- Distinct **Shift Over** screen when stamina blocks the next guest (no revive).

### 1.9 Haptics

Android `Handheld.Vibrate` or a tiny plugin-free pattern. Off if 振动 is off. Never vibrate on ad start.

---

## 2. Backlog (ideas, not MVP)

Do **not** implement these in the first OpenCode pass.

### 2.0 Planned design decisions (record now, ship later)

These supersede the matching MVP notes when implemented. Do not change production economy or pile spawn code until an explicit build pass.

#### GameObject Pool for candies

- Manage candy **spawn and despawn** through a **GameObject Pool** (acquire on restock / drop-in; release on pick / remove).
- Goal: cut `Instantiate` / `Destroy` churn on the pile and improve runtime performance on mid-range Android.
- Pool is keyed by candy type (or prefab); restock and serve juice should reuse pooled instances rather than destroy + new spawn.

#### Recipe star ranks (1–5) and shop prices

- Each shop recipe has a **star rank** from **1 to 5**.
- Higher rank → higher purchase price. Fixed price table (replaces main spec §4.3 linear formula when adopted):

| Stars | Cost (coins) |
| --- | --- |
| 1 | 1000 |
| 2 | 3000 |
| 3 | 5000 |
| 4 | 8000 |
| 5 | 10000 |

- UI should show the star rank on recipe rows (not coins alone). How ranks are assigned per mesh/catalog row is TBD (manual authoring vs auto from rarity).

##### Star-rank display & VFX (higher = cooler)

- **Requirement:** each star rank has a **distinct** presentation. Higher stars must read as clearly more premium / flashy than lower ones — not the same frame with only a different star count.
- **Surfaces that must tier:** Recipe Shop row (icon frame + idle FX), unlock toast / purchase sparkle, optional pile candy idle accent when that type is in the scene (keep subtle so picks stay readable).
- **Each rank has its own particle preset** (Unity ParticleSystem or VFX Graph lite). Do not reuse one sparkle for all ranks with only color tint; scale **density, motion, and accent** with rank.
- Stay on-brand (art bible pastels / lemon / grape / cream). No dark “legendary loot” chrome; keep candy-shop juice. Cap particle count on mid Android (especially ★5).

| Stars | Visual read | Particle / juice (defaults) |
| --- | --- | --- |
| 1 | Plain cream frame, soft icon | Tiny sugar dust on purchase only; no idle loop |
| 2 | Light icing rim, gentle sheen | Soft sparkle idle (sparse); short pop on unlock |
| 3 | Grape/mint accent frame, mild bob | Continuous light sparkle + small icing burst on unlock |
| 4 | Dual-tone frosting frame, stronger sheen | Richer sparkle + occasional candy-dot motes; punchier unlock burst |
| 5 | Hero frame (lemon + sugar-pink accents), clearest “wow” | Dense but short-lived glitter / confetti-lite loop + big unlock burst; optional brief screen-edge sugar shimmer on unlock only |

- Locked rows still show rank frame/stars but **mute or pause** idle particles (grey readable row per §1.5).
- Special editions may reuse the particle tier of their star rank, with an optional extra color-tint on the burst so they feel distinct without a sixth FX ladder.

#### Special-edition recipes (same mesh, different color)

- Candies that share the **same grid/mesh shape** but use a **different color** (material / tint variant) are **special-edition recipes**, not ordinary shop rows (no coin buy as the primary unlock).
- **Fantasy:** long-term **collection milestones**, not in-run mastery / pick-count grind.
- **Prerequisite:** the matching normal (same mesh) recipe must already be unlocked before that special edition can be granted.
- Special editions stay out of the pile and out of orders until unlocked (same gate as locked recipes).
- **Do not:** unlock via ads; do not use daily challenge as the main grant path; do not use pure random drop as the main path.

**Unlock split (~50% / 50% of the special-edition catalog):**

| Track | Share of special editions | How it works |
| --- | --- | --- |
| **Owned-count / shelf milestones** | ~half | Grants fire when the player crosses ownership thresholds on **normal** recipes (starters + bought shop recipes; specials do not count toward the threshold). |
| **Sign-in long line** | ~half | Grants fire on long-term sign-in milestones (beyond / after the MVP streak-7 normal-recipe gift). |

Assign each special edition to exactly one track at catalog authoring time (or alternate when auto-assigning). Prefer lower-star / softer colors on earlier milestones.

**Owned-count track (defaults — tune when catalog size is known):**

| Milestone | Condition (normal recipes owned) | Grant |
| --- | --- | --- |
| Shelf A | Own **5** normals | 1 special (lowest remaining on this track) |
| Shelf B | Own **10** normals | 1 special |
| Star band 3 | Own **all 3★** normals | 1 special |
| Star band 4 | Own **all 4★** normals | 1 special |
| Full shelf | Own **all** normals | Remaining owned-track specials (or 1 flagship + keep all-unlocked coin bonus) |

If a threshold is crossed and no eligible special remains on this track (or prerequisite base missing), skip grant; check again when a prerequisite unlocks.

**Sign-in long-line track:**

- Keep MVP **streak → 7 → cheapest remaining normal recipe** while any normal is still locked.
- After normals are all owned (or when the streak-7 recipe slot would no-op): streak reaching **7** grants **1 special** from the sign-in track instead (still once per seven-cycle; reuse `recipeGrantedForThisSevenCycle` or a parallel flag).
- Additional long-line steps (defaults): cumulative **sign-in days** or completed seven-cycles at **14 / 21 / 28** (or 2nd / 3rd / 4th time streak hits 7) each grant **1** sign-in-track special.
- Missed day still resets the short streak per §8; cumulative counters for 14/21/28 should use a separate `lifetimeSignInDays` (or `sevenCyclesCompleted`) so long-line progress is not wiped by one miss — short streak remains the daily habit loop.

**UI:** Main Menu (or Recipe Shop) shows light collection progress, e.g. `特别版 n/N`, and the next milestone hint for each track. Toast on grant: `新特别版：{name}`.

**Numbers** (5 / 10 / star bands / 14 / 21 / 28) are defaults pending final catalog count; keep the 50/50 track split when retuning.

#### Stamina from ads + sign-in

Replaces the older MVP ban on “watch ad for stamina.” Coin-buy stamina, timed regen, and overflow above `dailyMax` stay out of scope.

##### Watch-ad stamina (when low / empty)

- When **stamina is insufficient** (`stamina < 1`) to start a run or the next guest, the empty-stamina sheet / Shift Over may offer **看广告恢复体力** (opt-in only; never auto-play).
- On rewarded-ad completed: grant **`staminaAdGrant`** (default **+5**), then `Clamp(0, dailyMax)`. Persist immediately.
- New placement: `reward_stamina` (Main Menu empty sheet and Shift Over). Count toward optional rewarded caps in `AdConfig` (defaults: include in the daily optional max; separate per-date cap **`maxStaminaAdsPerDate = 3`**).
- If ad not ready: grey/hide the button; do not promise stamina.
- After a successful grant, if `stamina ≥ 1`, enable 开始营业 / allow continuing the session flow as usual (Shift Over → Main Menu then start, or stay on menu ready to start — do not auto-enter Game).
- Still no coin purchase of stamina; still no minute-based regen; still no overflow above 20.

##### Sign-in also grants stamina

- Daily sign-in rewards are **coins + (recipe on streak-7 when eligible) + stamina**, not coins/recipe alone.
- On each successful **new-date** claim (the same event that grants +500 coins): also grant **`signInStaminaGrant`** (default **+3**), then clamp to `dailyMax`.
- Show stamina on the sign-in panel reward summary (e.g. `+500 金币` / `体力+3` / recipe line when applicable). Panel copy and i18n keys must list stamina with the other rewards.
- Streak-7 recipe / all-unlocked +500 behavior unchanged; stamina grant is **every** daily claim day, not only day 7.
- Order of application on Boot: date refresh rules in §8.2 first (new local date → set pool to `dailyMax`), then apply sign-in stamina **only if** that would not double-dip oddly — **preferred:** on a new local date, refresh still sets `stamina = dailyMax`, and the sign-in stamina grant is **skipped when the refresh already filled to max** (toast can omit `体力+3`); if the player somehow claims when below max on the same date path, apply the grant. Alternate simpler rule (also acceptable): always add sign-in grant then clamp (often a no-op right after refresh). Pick one in implementation and keep it consistent; default to **always add then clamp** for simpler code.
- Optional `reward_daily_extra` (+50 coins) does **not** also grant stamina unless explicitly extended later.

#### Fail coin penalty

MVP currently keeps all coins on Game Over (`No coin penalty`). **Planned:** a confirmed **fail** also deducts **part of the player's coins** (in addition to fail stamina −3).

- **When:** same moment fail is confirmed — Game Over → leave without successful revive, or Pause → 放弃本局 confirm. **Not** on Shift Over. **Not** if revive succeeds.
- **What:** deduct from the persistent `coins` wallet; never below 0.
- **Default formula** (tunable on `EconomyConfig`):

```
runEarned = coins gained from serves this run (speed rewards + perfect +5; exclude ad doubles if easier to track gross serve payouts — pick one and document in code)
penalty = max(failCoinPenaltyMin, round(runEarned * failCoinPenaltyRatio))
penalty = min(penalty, coins)   // cannot go negative
coins -= penalty
```

| Field | Default | Meaning |
| --- | --- | --- |
| `failCoinPenaltyRatio` | **0.25** | Lose **25%** of this run’s earned coins |
| `failCoinPenaltyMin` | **20** | At least 20 coins if the wallet has that much (if `runEarned` is 0, still apply `min(failCoinPenaltyMin, coins)` so early fails hurt a little) |

- Game Over UI must show the loss clearly: e.g. `金币-{penalty}` next to the coin line / on leave. Pause confirm copy should mention coin loss as well as stamina −3 when this ships.
- Coins already spent on in-run power-up buys are not refunded; the penalty is a separate wallet hit.
- Optional later: revive could advertise “免扣金币” — not required for the first pass of this rule.

### Play

| Idea | Why later |
| --- | --- |
| Rush hour (after 10 serves, 3-type orders more often) | Already have mild scaling |
| Rare “lucky candy” in pile = bonus coins | Can feel unfair |
| Pinch-zoom / drag-rotate camera | Mis-taps; portrait pile is enough |
| Walking 3D customers | Art/anim cost |
| Shop furniture upgrades (counter skins) | Cosmetics |
| Fourth power-up (e.g. 透视 outline of needed candies) | Scope |
| Continue with less reward after revive | Revive already exists |
| Fever meter | Extra HUD |
| GameObject Pool for pile candies (see §2.0) | Perf polish after MVP loop is stable |
| Recipe star ranks + price table (see §2.0) | Economy redesign + per-rank UI/VFX tiers |
| Special-edition color variants (see §2.0) | Collection milestones: owned-count + sign-in long line (~50/50) |
| Stamina from ads + sign-in (see §2.0) | Opt-in ad when empty; daily sign-in also grants stamina |
| Fail coin penalty (see §2.0) | Confirmed fail deducts part of coins (default 25% of run earnings) |

### UX

| Idea | Why later |
| --- | --- |
| Hold candy to inspect name | Tutorial + chips are enough |
| Colorblind alternate palettes | Icons already required |
| Photo mode | Not core |
| Share score card | Needs native Android share |
| IAP remove ads | Out of MVP |
| Cloud save / account | Out of MVP |

### Live ops

| Idea | Why later |
| --- | --- |
| Weekend 2× sign-in | Needs calendar events |
| Real AdMob | Interface already reserved |

---

## 3. Explicitly do not add

- Ads on **use** when inventory > 0
- Auto-play ads during picking
- Landscape
- Coin combo that stacks with speed bonus (visual combo only)
- Free recipe from ads
- Punishing “fatigue” (smaller colliders over time). Daily **stamina** (§8.2) is in MVP and is not this.
- Coin-buy stamina / timed regen / overflow above `dailyMax` (watch-ad stamina + sign-in stamina **are** planned — see §2.0)

---

## 4. Player fantasy (keep in copy and juice)

You are a tiny shop hero: fast hands, cute guests, a messy candy mountain. UI should feel like frosting and glass jars, not a spreadsheet. Every extra widget must answer: **does this help me find the right candy or feel proud after a serve?** If not, backlog.

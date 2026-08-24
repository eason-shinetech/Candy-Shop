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
- **Perfect serve:** 0 wrong picks this customer → extra **+5 coins**, stamp `完美`, and **restore 1 star** if stars < 3. Speed-reward 看广告翻倍 does not double the +5.
- Serve: small confetti burst (pastel, ≤80 particles) + current portrait happy frame.

### 1.4 Meta on Main Menu

- `历史最佳：服务 N 位客人` (max customers in one run)
- Coins, streak **7 dots** (filled = days this cycle)
- Settings: 音乐 / 音效 / **振动** (three toggles)
- After buying a recipe: toast `新糖果上架：{name}` on next Game scene load (once)
- Banner `今日配方` (progress / 已完成). Tap: Recipe Shop if locked, else 开始营业

### 1.10 Daily featured-recipe challenge

Full rules: main spec **§8.1**. Quota 12 correct picks, 70% order bias when unlocked, 20% off in shop when locked, reward +120 coins and +1 Freeze, once per local date.

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
- Pause **放弃本局** already needs confirm; copy: `真的要打烊吗？本局星星和进度会结束`

### 1.7 Pile feel

- Very small idle jiggle / sugar sparkle on the pile root (subtle, always on)
- Restock drop from above (already in spec)
- Magnet: pulled candies fly to chips (same as correct pick)

### 1.8 Game Over

- `本局服务 {n} 位` / `赚到 {coins} 金币` / `历史最佳 {best}`
- If `n` beat best, badge `新纪录`
- Primary: 回到主菜单. Secondary: revive ad (existing)

### 1.9 Haptics

Android `Handheld.Vibrate` or a tiny plugin-free pattern. Off if 振动 is off. Never vibrate on ad start.

---

## 2. Backlog (ideas, not MVP)

Do **not** implement these in the first OpenCode pass.

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
- Punishing “fatigue” (smaller colliders over time)

---

## 4. Player fantasy (keep in copy and juice)

You are a tiny shop hero: fast hands, cute guests, a messy candy mountain. UI should feel like frosting and glass jars, not a spreadsheet. Every extra widget must answer: **does this help me find the right candy or feel proud after a serve?** If not, backlog.

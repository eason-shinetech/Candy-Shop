# Candy Shop — Unity UI + Impeccable

**Date:** 2026-08-24  
**Source:** [pbakaus/impeccable](https://github.com/pbakaus/impeccable)  
**Answer:** **Yes, use it for Unity UI — as a design skill, not as a web detector.**

Impeccable is an AI design language (hierarchy, type, color, motion, anti-AI-slop). It was built for frontend HTML/CSS, but the **commands and DESIGN.md contract** still make Unity uGUI / UI Toolkit less generic. The **CLI detector** (`npx impeccable detect`) and **`/impeccable live`** (browser) do **not** apply to `.unity` / Canvas.

**Priority if anything conflicts:** Game spec → [Art bible](2026-08-24-candy-shop-art-bible.md) → repo-root `DESIGN.md` → generic Impeccable SaaS defaults. Never let Impeccable replace candy-shop pastel with Inter + purple gradients.

---

## 1. What to use vs skip

| Use on this project | Skip |
| --- | --- |
| `/impeccable init` already done as `PRODUCT.md` + `DESIGN.md` at repo root | `npx impeccable detect` on `Assets/` (HTML/CSS rules) |
| `/impeccable shape` / `critique` / `polish` on a **screenshot** of Game view | `/impeccable live` (needs a browser) |
| `/impeccable layout` `typeset` `colorize` `delight` `onboard` `clarify` `harden` (text overflow, Chinese) | Bounce/elastic easings, nested gray cards, Inter/Arial |
| OpenCode install: `npx impeccable install --providers=opencode` so the implementer loads the skill | Comp-first web HTML mocks as the shipping UI |

Install is optional for Cursor if you only read the markdown. **OpenCode should install** the OpenCode provider skill so UI implementation sees the same vocabulary.

---

## 2. Unity mapping (do this instead of CSS)

| Impeccable idea | Unity 6000.0.77f1 |
| --- | --- |
| Design tokens | ScriptableObject `UiTheme` or a single `UITheme.asset`: colors from the art bible, TMP font sizes |
| Type | TextMeshPro, **not** Arial/Liberation Sans. Rounded display font for titles, readable rounded body. Chinese-capable (e.g. a licensed rounded CJK or TMP fallback atlas). |
| Color | Art-bible hex only. Neutrals are **cream/cocoa**, never `#000` / `#808080` on pink. Tint shadows `Cocoa` at 20–35% alpha. |
| Spacing | 8px grid. Padding on panels ≥ 24px. Thumb buttons ≥ **88px** tall (portrait). |
| Cards | One icing-border panel language. **Do not** nest cream cards inside cream cards. HUD chips are pills, not extra cards. |
| Motion | **UI Effect** ([mob-sakai/UIEffect](https://github.com/mob-sakai/UIEffect)) for click / panel show-hide. Ease **out-cubic**. No bounce/elastic. Serve confetti = particles. First-run = **Tutorial Spotlight**. |
| Contrast | Cocoa text on Cream; white/cream text only on Sugar Pink / Berry buttons. |
| Touch | No 32px icon-only hit areas. Expand `Raycast Padding`. |
| Z-order | Sort Order: world camera < HUD < popups < ads stub overlay. |

Stack: **uGUI Canvas** (Screen Space Overlay, 1080×1920, match height) for MVP. UI Toolkit is allowed later if it still hits the same theme; do not mix two HUD systems in MVP.

---

## 3. Anti-slop (Impeccable + this game)

Do **not**:

- Inter / Roboto / Arial / system default as the title font
- Purple-to-blue SaaS gradients
- Gray labels on Sugar Pink
- Three stacked rounded rectangles for one piece of info
- Tiny gray captions under every heading
- Glassmorphism, neon cyber, Material 3 default

Do:

- Cookie/frosting chrome from the art bible
- One accent per screen (pink CTAs; lemon for coins; mint for success)
- Big title, fewer words, copy from the i18n table (zh/en)
- Empty states (配方商店全解锁, 今日挑战已完成) with a cute illustration, not a blank list

---

## 4. When OpenCode builds a screen

1. Read `PRODUCT.md`, `DESIGN.md`, art bible, spec §10 and §17.
2. Build the Canvas in portrait. Apply `UiTheme` colors/fonts.
3. After a play-mode screenshot: Cursor (or OpenCode with the skill) may run `/impeccable critique` / `polish` **on that screenshot** and then patch Unity, not generate a React page.

---

## 5. Files

| File | Role |
| --- | --- |
| [PRODUCT.md](../../../PRODUCT.md) | Audience, voice, anti-references |
| [DESIGN.md](../../../DESIGN.md) | Tokens, type, components, motion |
| This spec | Unity adapter + what not to run |

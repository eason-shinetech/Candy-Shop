# DESIGN.md — Candy Shop

Impeccable design contract for **Unity uGUI**. Palette and sprites: art bible. Do not invent a second brand.

Adapter: `docs/superpowers/specs/2026-08-24-candy-shop-ui-impeccable.md`

## Build path

`code` — ship Unity Canvas, not a web comp. Screenshots of Game view are the review target.

## Color

| Token | Hex | Use |
| --- | --- | --- |
| cream | `#FFF6E8` | Panels, sheets |
| sugarPink | `#FF8FB8` | Primary CTA |
| berry | `#E85A8C` | Pressed / danger-lite |
| skyMint | `#7EE0C6` | Success, perfect, challenge done |
| lemon | `#FFE07A` | Coins, stars, timer OK |
| cocoa | `#6B3F2A` | Text, outlines |
| grape | `#A78BFA` | Secondary / recipes |
| ice | `#B8E8FF` | Freeze |
| magnetRed | `#FF6B6B` | Magnet |
| wind | `#C8F5D4` | Tornado |

No pure black. Shadows = cocoa @ 25% alpha. No gray-on-pink.

## Type

- Engine: TextMeshPro, **Latin + CJK** (fallback atlas). Switch language without missing glyphs.
- Display: rounded cute font for 糖果店 / 开始营业 (not Arial, not Inter).
- Body: rounded readable CJK, cocoa on cream, min ~28px at 1080 width for secondary, ~40px+ for primary CTA.
- One type family + weight changes. Do not mix five fonts.

## Layout

- Portrait 9:16 / 9:19.5, Safe Area.
- 8px grid. Panel padding ≥ 24px.
- Thumb zone bottom: power-ups ≥ 88px tall.
- Center of Game HUD is the 3D pile — UI must not cover more than ~35% of height combined top+bottom.
- No nested cards. Order chips = pills. Popups = one icing panel.

## Components

- **Primary button:** sugar-pink icing nine-slice, cream or white label.
- **Secondary:** grape icing.
- **Icon button:** circular cookie, 72px+ hit.
- **Chip:** cream pill, icon + number.
- **Toast:** mint or cream, auto-hide, does not block pile taps after 2s.
- **Sheet:** cream + frosting, dimmer behind (cocoa 40%).

## Motion

- **UI Effect** for buttons and panel show/hide ([plugins](docs/superpowers/specs/2026-08-24-candy-shop-unity-plugins.md)).
- Ease out-cubic, 0.15–0.35s UI; serve confetti as spec.
- No bounce, no elastic, no infinite pulse on every element (timer low-time pulse is allowed).
- First-run: **Tutorial Spotlight** overlay + hole, not a custom dimmer.

## Delight (keep candy, not chrome)

Sugar shine on CTAs, chip punch on correct pick, star fill on perfect, 7-dot streak. Do not add particle noise on every hover-equivalent.

## Quality bar

If a screen could be a generic idle-game template with a pink reskin, it fails. It should look like the same shop as the 3D pile.

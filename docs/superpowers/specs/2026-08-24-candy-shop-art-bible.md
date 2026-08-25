# Candy Shop — Art Bible (卡通可爱风)

**Date:** 2026-08-24  
**Use:** Every 2D image, icon, portrait, splash, and particle color must match this file.  
**Rule:** Generate assets as a **set**. Same prompt prefix, same palette, same outline weight. Do not commission one realistic icon and one chibi icon.

---

## 1. Style lock (paste this first in every generator)

```
Cute cartoon candy-shop game art, kawaii chibi, soft rounded shapes,
thick clean outlines, pastel candy colors, glossy sugar highlights,
warm bakery lighting, cheerful and wholesome, high readability at small size,
no realism, no horror, no photoreal textures, no text unless specified.
Style must match a children's mobile game UI kit.
```

Negative prompt (if the tool supports it):

```
photorealistic, 3d render cinematic, horror, blood, dark grim, noisy texture,
thin lineart, adult fashion, pixel art, low poly, watermark, extra text, logo mashup
```

---

## 2. Palette (do not invent new brand colors)

| Name | Hex | Use |
| --- | --- | --- |
| Cream | `#FFF6E8` | Panels, cards, shop background |
| Sugar Pink | `#FF8FB8` | Primary buttons, hearts |
| Berry | `#E85A8C` | Pressed / important accent |
| Sky Mint | `#7EE0C6` | Success, serve complete |
| Lemon | `#FFE07A` | Coins, stars, healthy timer |
| Cocoa | `#6B3F2A` | Outlines / body text on light |
| Grape | `#A78BFA` | Secondary chips, recipes |
| Ice | `#B8E8FF` | Freeze |
| Magnet Red | `#FF6B6B` | Magnet (cute, not industrial) |
| Wind | `#C8F5D4` | Tornado |

Export PNG with **transparent background** unless the row says opaque.

---

## 3. Technical

| Item | Value |
| --- | --- |
| Icon / HUD | 512 x 512 PNG, transparent, content padded 10% so round mask does not clip |
| Order candy thumb | 256 x 256 PNG, transparent |
| Customer portrait | 512 x 512 PNG, transparent, bust or full chibi |
| Button nine-slice | 256 x 256 PNG, opaque cream + pink frosting border, mark 48px border for 9-slice |
| Splash / menu BG | 1080 x 1920 PNG, portrait, opaque |
| Atlas | Optional: pack HUD icons into one 2048 atlas after generation so filtering stays consistent |

Unity import: Sprite (2D and UI), sRGB, no mipmaps for UI, compression Normal Quality.

---

## 4. Asset list and prompts

Always prepend the **style lock**. Then append the line in the Prompt column.

### 4.1 UI chrome

| File | Prompt |
| --- | --- |
| `Assets/Art/UI/bg_main_menu.png` | Portrait 1080x1920 candy shop interior, pastel, wooden counter, glass jars of candy, clouds of frosting, no people, no readable text, cute cartoon |
| `Assets/Art/UI/bg_game_shop.png` | Same interior, slightly darker edges as vignette, empty center for 3D pile, portrait, cute cartoon |
| `Assets/Art/UI/panel_cream.png` | Rounded cream card, pink frosting border, cookie icing, square, no text |
| `Assets/Art/UI/btn_primary.png` | Rounded candy button, sugar pink icing, glossy, empty center for text |
| `Assets/Art/UI/btn_secondary.png` | Same as primary but grape-lilac icing |
| `Assets/Art/UI/frame_star_empty.png` | Cute outlined star, cream fill, cocoa outline |
| `Assets/Art/UI/icon_star.png` | Glossy lemon candy star, sparkle |
| `Assets/Art/UI/icon_coin.png` | Round gold-lemon candy coin, star stamp, cute |
| `Assets/Art/UI/icon_pause.png` | Two rounded candy sticks as pause bars, pink |
| `Assets/Art/UI/icon_stamina.png` | Glossy sugar-pink heart candy (体力), frosting highlight, cute, not a realistic anatomical heart |
| `Assets/Art/UI/bar_timer_bg.png` | Horizontal rounded bar empty, cream |
| `Assets/Art/UI/bar_timer_fill.png` | Horizontal rounded bar fill, lemon to mint gradient |
| `Assets/Art/UI/popup_signin.png` | Gift box of candies, pastel, cute, no text |

### 4.2 Power-up icons (HUD)

| File | Prompt |
| --- | --- |
| `Assets/Art/UI/icon_magnet.png` | Kawaii horseshoe magnet, rounded, magnet-red and cream, sparkles, candy shop toy, not industrial tool |
| `Assets/Art/UI/icon_tornado.png` | Cute swirl tornado of mint sugar dust and candy wrappers, friendly, not disaster |
| `Assets/Art/UI/icon_freeze.png` | Kawaii snowflake popsicle, ice-blue, smile optional, soft frost |

### 4.3 Candy order icons (2D)

**Source of truth:** `Assets/Art/Candy Icon/`. Do **not** generate a fixed 10-name list, and do not treat rendered `Resources/UI/Candies/icon_candy_*` (or family fallbacks) as canonical.

Each catalog candy uses the PNG whose filename matches its prefab name:

| Prefab (`Assets/Prefabs/Candy`) | Icon (`Assets/Art/Candy Icon`) |
| --- | --- |
| `Chocolate Bar.prefab` | `Chocolate Bar.png` |
| `Donut A1.prefab` | `Donut A1.png` |
| `Cotton Candy B2.prefab` | `Cotton Candy B2.png` |

Skip scenery in that folder that is **not** a candy type: `Stick`, `Icecream Plate`, `Lollipop Ground *`.

If a catalog type is missing its PNG, that is an art gap — do not invent a replacement drawing or fall back to a sibling-family icon as the shipped thumb.

### 4.4 Customer portraits (queue)

Generate a **set of 6** so the queue can randomize. Same body proportions, different hair/hat colors.

| File | Prompt extra |
| --- | --- |
| `portrait_customer_01.png` | Chibi kid, pink hair, excited, candy shop customer |
| `portrait_customer_02.png` | Chibi kid, mint hoodie, shy smile |
| `portrait_customer_03.png` | Chibi kid, yellow cap, waving |
| `portrait_customer_04.png` | Chibi kid, grape hair bow |
| `portrait_customer_05.png` | Chibi kid, cocoa curls |
| `portrait_customer_06.png` | Chibi kid, ice-blue beanie |

Save under `Assets/Art/UI/Customers/`.

### 4.5 Recipe book

| File | Prompt |
| --- | --- |
| `icon_recipe_book.png` | Cute cookbook with candy on cover, grape and cream, no readable title |
| `icon_lock.png` | Rounded padlock, pastel, not scary |
| `icon_check.png` | Mint check badge, cute |
| `icon_ad.png` | Cute rounded TV / play-badge, sugar pink and cream, not a realistic YouTube logo, no text |

---

## 5. Consistency checklist (reject if any fail)

- [ ] Same outline thickness family across icons
- [ ] Same pastel saturation (not one neon icon next to a desaturated one)
- [ ] Transparent PNG icons, no random white boxes
- [ ] Readable at 72px on a phone
- [ ] No English/Chinese baked into the image (MVP: **no baked text**; TMP + i18n draws zh/en)
- [ ] Power-up VFX colors match Magnet Red / Wind / Ice
- [ ] Menu background and game HUD panels look like the same shop

---

## 6. 3D and lighting (not generated PNG, still style)

- Warm bakery light, slight pink fill, no cinematic contrast.
- Candy meshes from `Candy/` stay glossy and toy-like.
- Do not apply realistic dirt, rust, or photogrammetry materials.
- Particle textures: simple radial soft dots, stars, hearts — not scanned photos.

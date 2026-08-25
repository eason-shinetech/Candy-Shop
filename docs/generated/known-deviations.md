# Known Deviations & Notes

Date: 2026-08-25 (supplements 2.0 implementation pass)

0. **Supplements 2.0 implemented (2026-08-25).** Star ranks, special editions, candy pool, stamina ads, sign-in stamina and the fail coin penalty are now live. Implementation notes:
   - Star ranks are auto-banded across the sorted catalog (equal 1/5 bands); prices use the fixed table 1000/3000/5000/8000/10000. Rank assignment can be hand-authored later without code changes (edit `RecipeDefinition.starRank` + rerun bootstrap or tweak in inspector).
   - Row FX is a parametric uGUI dot system (`RecipeRowFx`) instead of five ParticleSystem prefabs: density, motion speed, dot size and accent colors all scale with rank; rank 1 has no idle loop (per the tier table) and rows only animate while visible (coroutine stops on disable) to respect the mobile particle budget.
   - 8 special editions are auto-generated (4 owned-track / 4 sign-in-track) from distinct families with tinted material clones. If the kit later ships true same-mesh color variants, replace the generated ones by hand-authoring `RecipeDefinition.isSpecial` rows — the grant logic keys off `baseRecipeId` + `specialTrack` only.
   - Fail coin penalty uses gross serve payouts (`CoinsEarnedThisRun`, ad doubles excluded) per the spec's "pick one and document" note.
   - Sign-in stamina uses the "always add then clamp" default; the panel line is shown only when stamina actually increased (permitted omission when the date refresh already filled the pool).

1. **UI Effect (mob-sakai) not installed.**
   - UIEffect installs via git URL which is fragile in CI/batchmode.
   - Equivalent juice is implemented manually: punch-scale on chips, canvas shake on wrong picks, toast/popup fades, star fill — all using the art-bible palette, ease-out only (no bounce), per DESIGN.md priorities (game spec → art bible → DESIGN.md).

1b. **Tutorial Spotlight (Den4ik, free) — integrated via reflection adapter.**
   - The asset is free and owned on the account, but Asset Store packages can only be downloaded through the Unity Editor UI (Package Manager → My Assets), not headlessly.
   - `Assets/Scripts/UI/TutorialSpotlightAdapter.cs` resolves `TutorialSpotlightManager` (ShowSpotlight / HideSpotlight / SetSpotlightTarget) at runtime. The 3-card tutorial spotlights the order chips (card 1), stars (card 2) and power-up bar (card 3); the card lives on a dedicated canvas (sortingOrder 500) so it stays clickable above the overlay.
   - Until the package is imported once via Package Manager → My Assets → Tutorial Spotlight → Import, the tutorial simply runs without the dim overlay (adapter logs a note). No code change needed after import.

2. **Text: uGUI `Text` + dynamic OS CJK font (not TMP).**
   - No licensed rounded-CJK font asset exists in the repo; `UIKit.DefaultFont` requests Microsoft YaHei / PingFang / Noto Sans CJK at runtime with automatic OS glyph fallback, so zh and en render without tofu.
   - All copy flows through `I18nService` exactly as the i18n spec requires, so swapping to TMP later is a drop-in change.

3. **Stub ad caps partially in-memory.** `reward_powerup_buy_*` (6/day) and `reward_coins` (4/day) counters reset per app session keyed by local date (save schema §12 has no fields for them). Optional-ad daily cap uses the persisted `adsWatchedCountToday`.

4. **Pile budget.** Restock fills only the current order's candy types, hard-capped at `CandyPileRestock.maxTotalInstances` (420) so late-game (70 recipes owned) cannot spawn ~850 objects on mobile.

5. **Ads pause the timer.** The power-up buy sheet (and its rewarded ad) opens with the customer timer paused and resumes on close/success/cancel, per spec §14.

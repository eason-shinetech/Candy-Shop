# Candy Shop — i18n (zh / en)

**Date:** 2026-08-24  
**Locales:** `zh` (Simplified Chinese), `en` (English). No other languages in MVP.

All player-facing text goes through `I18nService.Get(key)` (or TMP bound to the same table). **Do not hardcode** Chinese or English in MonoBehaviours. Code comments stay English.

PNGs still have **no baked text** (art bible).

---

## 1. Language selection

| Event | Locale |
| --- | --- |
| First launch, no save | `zh` if `Application.systemLanguage` is `Chinese`, `ChineseSimplified`, or `ChineseTraditional`; else `en` |
| After that | `save.language` (`"zh"` or `"en"`) |
| Settings | Toggle **中文 / English** (labels always bilingual on that row so the player can switch even if they picked the wrong language). Apply **immediately**; persist; no app restart |

`language` in `candy_shop_save.json`. Traditional Chinese devices use `zh` (Simplified strings) in MVP.

---

## 2. Implementation

- Files: `Assets/I18n/strings_zh.json`, `Assets/I18n/strings_en.json` (flat key → string). Format args: `{0}`, `{1}` via `string.Format`.
- `I18nService` loads both, `SetLanguage`, event `OnLanguageChanged` so HUD/menus refresh.
- TMP: Latin + CJK. Use a rounded display font with a **CJK fallback** atlas (or one font that covers both). English must not tofu; Chinese must not tofu.
- Candy / recipe names: `CandyTypeDefinition.displayNameKey` or `nameZh` + `nameEn` on the generated catalog. UI uses the active locale.
- Layout: English is often longer. Buttons use preferred-width + padding; do not clip 开始营业 vs Open Shop. Test both locales at 1080×1920.

---

## 3. String table (MVP)

Keys are stable. If a screen needs a new sentence, add a key to **both** files.

| Key | zh | en |
| --- | --- | --- |
| `app_title` | 糖果店 | Candy Shop |
| `btn_start` | 开始营业 | Open Shop |
| `btn_recipes` | 配方商店 | Recipes |
| `btn_settings` | 设置 | Settings |
| `btn_back` | 返回 | Back |
| `btn_continue` | 继续 | Resume |
| `btn_quit_run` | 放弃本局 | End Shift |
| `btn_quit_confirm` | 真的要打烊吗？当前客人会失败（体力-3），本局星星和进度会结束 | Close the shop? This guest counts as a fail (−3 stamina). This shift’s stars and progress will end. |
| `btn_confirm` | 确定 | OK |
| `btn_cancel` | 取消 | Cancel |
| `label_music` | 音乐 | Music |
| `label_sfx` | 音效 | Sound |
| `label_haptics` | 振动 | Vibration |
| `label_language` | 语言 | Language |
| `lang_zh` | 中文 | 中文 |
| `lang_en` | English | English |
| `best_served` | 历史最佳：服务 {0} 位客人 | Best: {0} customers |
| `daily_recipe` | 今日配方 | Today’s Recipe |
| `daily_recipe_progress` | {0} {1}/{2} | {0} {1}/{2} |
| `daily_recipe_done` | 已完成 | Done |
| `daily_recipe_unlock_hint` | 解锁后才能完成今日挑战 | Unlock this candy to finish today’s challenge |
| `signin_claim` | 领取 | Claim |
| `signin_close` | 关闭 | Close |
| `signin_streak` | 已连续签到 {0} 天 | {0}-day streak |
| `signin_coins` | 每日签到 +{0} 金币 | Daily sign-in: +{0} coins |
| `signin_streak_reward` | 连续签到奖励：解锁新配方 {0} | Streak reward: unlocked recipe {0} |
| `signin_streak_stamina` | 连续签到奖励：体力+{0} | Streak reward: +{0} stamina |
| `ad_extra_50` | 看广告再领 50 金币 | Watch ad for +50 coins |
| `recipe_buy` | 购买 | Buy |
| `recipe_owned` | 已解锁 | Unlocked |
| `recipe_new_toast` | 新糖果上架：{0} | New candy in stock: {0} |
| `hud_combo` | 连击 x{0} | Combo x{0} |
| `hud_perfect` | 完美 | Perfect |
| `hud_star_plus` | 星星+1 | Star +1 |
| `toast_buried` | 找不到？用龙卷风翻一翻 | Stuck? Use Tornado |
| `game_over_title` | 营业结束 | Shop Closed |
| `game_over_served` | 本局服务 {0} 位 | Customers served: {0} |
| `game_over_coins` | 赚到 {0} 金币 | Coins earned: {0} |
| `game_over_best` | 历史最佳 {0} | Best {0} |
| `game_over_record` | 新纪录 | New record |
| `btn_main_menu` | 回到主菜单 | Main Menu |
| `ad_revive` | 看广告再试一次 | Watch ad to try again |
| `ad_coins_80` | 看广告获得 80 金币 | Watch ad for 80 coins |
| `ad_double` | 看广告翻倍 | Watch ad to double |
| `ad_buy_need` | 购买需扣金币并看广告 | Costs coins and an ad |
| `ad_buy_cta` | 购买并观看广告 | Buy & watch ad |
| `ad_not_ready` | 广告还没准备好 | Ad not ready |
| `ad_refund` | 已退还金币 | Coins refunded |
| `coins_short` | 金币不足 | Not enough coins |
| `powerup_magnet` | 磁铁 | Magnet |
| `powerup_tornado` | 龙卷风 | Tornado |
| `powerup_freeze` | 冰冻 | Freeze |
| `powerup_sold_out_hint` | 库存 0 | Empty |
| `challenge_complete` | 今日挑战完成 | Daily challenge complete |
| `tutorial_skip` | 跳过 | Skip |
| `tutorial_next` | 下一步 | Next |
| `tutorial_1` | 客人要的糖，点堆里的就可以 | Tap the pile to grab what the guest wants |
| `tutorial_2` | 点错会扣星星；完美接待可以补回一颗（最多三颗） | Wrong tap costs a star. A perfect serve can restore one (max 3). |
| `tutorial_3` | 道具有库存就能用；没了要花金币并看广告补充 | Use a power-up if you have one. Restock costs coins and an ad. |
| `pause_title` | 暂停 | Paused |
| `label_stamina` | 体力 | Stamina |
| `stamina_frac` | {0}/{1} | {0}/{1} |
| `stamina_minus_one` | 体力-1 | Stamina −1 |
| `hud_stamina_plus` | 体力+1 | Stamina +1 |
| `hud_stamina_bonus` | 体力+{0} | Stamina +{0} |
| `hud_stamina_fail` | 体力-3 | Stamina −3 |
| `stamina_empty_title` | 今天累了 | Out of energy |
| `stamina_empty_body` | 体力用完了，明天再来营业吧 | No stamina left. Come back tomorrow. |
| `stamina_shift_title` | 打烊休息 | Time to rest |
| `stamina_shift_body` | 体力用完了，明天再接待客人 | Out of stamina. See guests tomorrow. |
| `game_over_stamina` | 剩余体力 {0} | Stamina left: {0} |

Candy mesh display names: add `name_zh` / `name_en` in `docs/generated/candy-catalog.md` when the kit is enumerated. Do not leave FBX names in the HUD.

---

## 4. OpenCode

- Create `Assets/Scripts/I18n/I18nService.cs` and the two JSON files with **every** key above.
- Settings language toggle on Main Menu and Pause.
- After `SetLanguage`, refresh all open screens.
- QA: flip zh ↔ en on menu, HUD, shop, tutorial strings, ads sheets, game over, stamina empty / Shift Over.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CandyShop
{
    // Bilingual string table (zh/en) backed by Assets/Resources/I18n/strings_*.json.
    // All player-facing copy must go through Get(); do not hardcode strings in MonoBehaviours.
    public static class I18nService
    {
        private const string PrefKey = "candy_shop_language";

        private static Dictionary<string, string> _zh;
        private static Dictionary<string, string> _en;
        private static string _language;

        public static event Action OnLanguageChanged;
        public static bool IsReady => _zh != null && _en != null;

        public static string Language => _language ?? DetectDefaultLanguage();

        // Loads both tables once and picks the saved (or system-detected) locale.
        public static void Initialize()
        {
            if (_zh == null) _zh = LoadTable("I18n/strings_zh");
            if (_en == null) _en = LoadTable("I18n/strings_en");

            var save = SaveDataService.Current;
            if (!string.IsNullOrEmpty(save.language))
                _language = save.language;
            else
            {
                _language = DetectDefaultLanguage();
                save.language = _language;
                SaveDataService.Save();
            }
        }

        private static Dictionary<string, string> LoadTable(string resourceBase)
        {
            var asset = Resources.Load<TextAsset>(resourceBase);
            if (asset == null)
            {
                Debug.LogWarning("Missing i18n table: " + resourceBase);
                return new Dictionary<string, string>();
            }

            var table = new Dictionary<string, string>();
            try
            {
                var raw = JsonUtility.FromJson<StringTable>(asset.text);
                if (raw?.entries != null)
                    foreach (var e in raw.entries)
                        if (!string.IsNullOrEmpty(e.key)) table[e.key] = e.value;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse i18n table " + resourceBase + ": " + ex.Message);
            }
            return table;
        }

        [Serializable]
        private class StringEntry { public string key; public string value; }

        [Serializable]
        private class StringTable { public StringEntry[] entries; }

        public static string Get(string key, params object[] args)
        {
            if (!IsReady) return key;
            string value;
            var table = Language == "en" ? _en : _zh;
            if (!table.TryGetValue(key, out value))
                _zh.TryGetValue(key, out value); // zh fallback for missing en keys
            if (string.IsNullOrEmpty(value)) return key;
            return args != null && args.Length > 0 ? string.Format(value, args) : value;
        }

        // Switch locale immediately, persist, and let open screens refresh.
        public static void SetLanguage(string lang)
        {
            lang = lang == "en" ? "en" : "zh";
            if (_language == lang) return;
            _language = lang;
            var save = SaveDataService.Current;
            if (save != null)
            {
                save.language = lang;
                SaveDataService.Save();
            }
            PlayerPrefs.SetString(PrefKey, lang); // mirror so detection can be overridden pre-save-load
            OnLanguageChanged?.Invoke();
        }

        public static void ToggleLanguage() => SetLanguage(Language == "en" ? "zh" : "en");

        private static string DetectDefaultLanguage()
        {
            var mirrored = PlayerPrefs.GetString(PrefKey, null);
            if (mirrored == "zh" || mirrored == "en") return mirrored;
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "zh";
                default:
                    return "en";
            }
        }
    }
}

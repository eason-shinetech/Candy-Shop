using System;
using System.IO;
using UnityEngine;

namespace CandyShop
{
    // Loads and stores the single local JSON save file.
    public static class SaveDataService
    {
        public static SaveDataModel Current { get; private set; }

        public static string SavePath =>
            Path.Combine(Application.persistentDataPath, "candy_shop_save.json");

        public static SaveDataModel Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    Current = JsonUtility.FromJson<SaveDataModel>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save load failed, starting fresh: " + e.Message);
            }

            if (Current == null)
                Current = new SaveDataModel();

            // First-launch grant: one of each power-up so the first uses need no ad.
            if (!Current.starterPowerUpsGranted)
            {
                Current.magnetCount = 1;
                Current.tornadoCount = 1;
                Current.freezeCount = 1;
                Current.starterPowerUpsGranted = true;
                Save();
            }

            return Current;
        }

        public static void Save()
        {
            if (Current == null) return;
            try
            {
                var json = JsonUtility.ToJson(Current);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError("Save write failed: " + e.Message);
            }
        }
    }
}

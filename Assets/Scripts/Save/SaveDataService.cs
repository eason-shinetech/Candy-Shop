using System;
using System.IO;
using UnityEngine;

namespace CandyShop
{
    // Loads and stores the single local JSON save file.
    // Current lazily self-loads so any scene can be play-tested directly without Boot.
    public static class SaveDataService
    {
        private const float MinWriteIntervalSeconds = 3f;

        private static SaveDataModel _current;
        private static bool _dirty;
        private static float _lastWriteTime = float.MinValue;
        private static SaveDriver _driver;

        public static SaveDataModel Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        public static string SavePath =>
            Path.Combine(Application.persistentDataPath, "candy_shop_save.json");

        public static SaveDataModel Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    _current = JsonUtility.FromJson<SaveDataModel>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save load failed, starting fresh: " + e.Message);
            }

            if (_current == null)
                _current = new SaveDataModel();

            // First-launch grant: one of each power-up so the first uses need no ad.
            if (!Current.starterPowerUpsGranted)
            {
                _current.magnetCount = 1;
                _current.tornadoCount = 1;
                _current.freezeCount = 1;
                _current.starterPowerUpsGranted = true;
                Save();
            }

            return Current;
        }

        // Throttled entry point for gameplay-frequency callers: inside the cooldown
        // window the write is deferred to the driver instead of hitting disk every pick.
        public static void Save()
        {
            if (_current == null) return;
            if (Time.realtimeSinceStartup - _lastWriteTime < MinWriteIntervalSeconds)
            {
                _dirty = true;
                EnsureDriver();
                return;
            }
            WriteNow();
        }

        // Force-write pending changes regardless of the cooldown (app pause/quit).
        public static void FlushDirty()
        {
            if (_dirty) WriteNow();
        }

        private static void WriteNow()
        {
            _dirty = false;
            _lastWriteTime = Time.realtimeSinceStartup;
            try
            {
                var json = JsonUtility.ToJson(Current);
                // Atomic write: temp file then replace so process death mid-write
                // cannot corrupt the only save blob.
                var tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Replace(tmp, SavePath, null);
                else File.Move(tmp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Save write failed: " + e.Message);
            }
        }

        private static void EnsureDriver()
        {
            if (_driver != null) return;
            var go = new GameObject("SaveDriver") { hideFlags = HideFlags.HideAndDontSave };
            _driver = go.AddComponent<SaveDriver>();
        }

        // Persists deferred writes once the cooldown elapses; survives scene loads.
        private sealed class SaveDriver : MonoBehaviour
        {
            private void Update()
            {
                if (!_dirty) return;
                if (Time.realtimeSinceStartup - _lastWriteTime < MinWriteIntervalSeconds) return;
                FlushDirty();
            }

            private void OnApplicationPause(bool paused)
            {
                if (paused) FlushDirty();
            }

            private void OnApplicationQuit()
            {
                FlushDirty();
            }
        }
    }
}

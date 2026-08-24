using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CandyShop.EditorTools
{
    // One-shot project bootstrapper: pipeline settings, player settings, candy catalog,
    // data assets, VFX prefabs and scenes. Run once via batchmode:
    // Unity.exe -batchmode -projectPath <path> -executeMethod CandyShop.EditorTools.CandyShopBootstrapper.RunAll -quit
    public static class CandyShopBootstrapper
    {
        private const string KitFbxPath = "Assets/Art/Candy/candy_kit.fbx";
        private const string DataRoot = "Assets/Resources/Data";
        private const string CatalogDir = DataRoot + "/Catalog";
        private const string RecipesDir = DataRoot + "/Recipes";
        private const string PowerUpsDir = DataRoot + "/PowerUps";
        private const string PrefabCandyDir = "Assets/Prefabs/Candy";
        private const string VfxDir = "Assets/Prefabs/VFX";

        [MenuItem("CandyShop/Bootstrap Project")]
        public static void RunAll()
        {
            EnsureFolders();
            SetupRenderPipeline();
            SetupPlayerSettings();
            AssetDatabase.Refresh();

            var catalog = BuildCandyCatalog();
            BuildPowerUpDefinitions();
            BuildConfigAssets();
            AssetDatabase.SaveAssets();
            BuildScenes();

            WriteGeneratedDocs(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("CandyShopBootstrapper: done. Catalog size = " + catalog.Count);
        }

        // ---------------- Folders / pipeline / player ----------------

        private static void EnsureFolders()
        {
            foreach (var dir in new[]
            {
                "Assets/Settings", "Assets/Resources", DataRoot, CatalogDir, RecipesDir, PowerUpsDir,
                "Assets/Prefabs", PrefabCandyDir, VfxDir,
                "Assets/Scenes", "../docs/generated"
            })
            {
                var path = dir.StartsWith("../") ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), dir)) : dir;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            AssetDatabase.Refresh();
        }

        private static void SetupRenderPipeline()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/CandyShop_URP.asset");
            if (existing != null)
            {
                GraphicsSettings.defaultRenderPipeline = existing;
                return;
            }

            var rendererData = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, "Assets/Settings/CandyShop_URP_Renderer.asset");

            var urp = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(urp, "Assets/Settings/CandyShop_URP.asset");

            var so = new SerializedObject(urp);
            var list = so.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
        }

        private static void SetupPlayerSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.companyName = "CandyShop";
            PlayerSettings.productName = "Candy Shop";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Old input manager (Input.GetTouch) kept enabled alongside the new system.
            SetActiveInputHandlerBoth();

            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void SetActiveInputHandlerBoth()
        {
            // 0 = old input manager, 1 = new input system, 2 = both.
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null)
            {
                prop.intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------- Candy catalog ----------------

        private class CatalogEntry
        {
            public string meshName;
            public string typeId;
            public string nameZh;
            public string nameEn;
            public bool isStarter;
            public int cost; // -1 for starters
            public GameObject prefab;
        }

        private static readonly Dictionary<string, string> ZhMap = new Dictionary<string, string>
        {
            { "lollipop", "棒棒糖" }, { "popsicle", "雪糕糖" }, { "cane", "拐杖糖" },
            { "gummy", "软糖" }, { "jelly", "果冻糖" }, { "chocolate", "巧克力" },
            { "wafer", "威化糖" }, { "waffer", "威化糖" }, { "waffle", "华夫糖" },
            { "balloon", "气球软糖" }, { "donut", "甜甜圈糖" }, { "macaron", "马卡龙糖" },
            { "cupcake", "杯子蛋糕糖" }, { "cake", "蛋糕糖" }, { "cookie", "曲奇糖" },
            { "cottoncandy", "棉花糖" }, { "marshmallow", "牛轧软糖" }, { "mint", "薄荷糖" },
            { "candy", "糖果" }, { "star", "星星糖" }, { "heart", "爱心糖" },
            { "bean", "糖果豆" }, { "ring", "圈圈糖" }, { "swirl", "漩涡糖" },
            { "strawberry", "草莓糖" }, { "cherry", "樱桃糖" }, { "berry", "莓果糖" },
            { "fruit", "水果糖" }, { "icecream", "冰淇淋糖" }, { "icecreamcone", "甜筒糖" },
            { "milkshake", "奶昔糖" }, { "mm_", "巧克力豆" }, { "pretzel", "蝴蝶脆饼糖" },
            { "swiss_roll", "瑞士卷糖" }, { "sweet_bread", "甜面包糖" }, { "sandwich", "夹心糖" }
        };

        // Scenery / furniture props inside the kit that are not pickable candies (spec section 4.1).
        private static readonly HashSet<string> ExcludedMeshes = new HashSet<string>
        {
            "candy_fence_A", "candy_fence_B",
            "icecream_plate",
            "lollipop_ground", "lollipop_groundv2", "lollipop_groundv3",
            "sign", "stick",
            "melted_icecream", "melted_icecreamv2", "melted_icecreamv3"
        };

        // English display names per family (i18n spec: names follow the active locale).
        private static readonly Dictionary<string, string> EnMap = new Dictionary<string, string>
        {
            { "lollipop", "Lollipop" }, { "popsicle", "Ice Pop" }, { "cane", "Candy Cane" },
            { "gummy", "Gummy" }, { "jelly", "Jelly" }, { "chocolate", "Chocolate" },
            { "wafer", "Wafer" }, { "waffer", "Wafer" }, { "waffle", "Waffle" },
            { "balloon", "Balloon Gummy" }, { "donut", "Donut" }, { "macaron", "Macaron" },
            { "cupcake", "Cupcake" }, { "cake", "Cake" }, { "cookie", "Cookie" },
            { "cottoncandy", "Cotton Candy" }, { "marshmallow", "Marshmallow" }, { "mint", "Mint" },
            { "candy", "Candy" }, { "star", "Star Candy" }, { "heart", "Heart Candy" },
            { "bean", "Candy Bean" }, { "ring", "Ring Candy" }, { "swirl", "Swirl Candy" },
            { "strawberry", "Strawberry" }, { "cherry", "Cherry" }, { "berry", "Berry" },
            { "fruit", "Fruit Candy" }, { "icecream", "Ice Cream" }, { "icecreamcone", "Cone" },
            { "milkshake", "Milkshake" }, { "mm_", "Choco Bean" }, { "pretzel", "Pretzel" },
            { "swiss_roll", "Swiss Roll" }, { "sweet_bread", "Sweet Bread" }, { "sandwich", "Sandwich" }
        };

        private static string ToEn(string meshName, int fallbackIndex)
        {
            var lower = meshName.ToLowerInvariant();
            foreach (var kvp in EnMap)
                if (lower.Contains(kvp.Key))
                    return kvp.Value;
            return "Candy " + (fallbackIndex + 1);
        }

        private static string ToZh(string meshName, int fallbackIndex)
        {
            var lower = meshName.ToLowerInvariant();
            foreach (var kvp in ZhMap)
                if (lower.Contains(kvp.Key))
                    return kvp.Value;
            return "糖果 " + (fallbackIndex + 1);
        }

        private static List<CatalogEntry> BuildCandyCatalog()
        {
            // Clean previous generated data so re-runs stay deterministic.
            foreach (var dir in new[] { CatalogDir, RecipesDir, PrefabCandyDir })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir))
                    if (!f.EndsWith(".meta")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            }

            var kit = AssetDatabase.LoadAssetAtPath<GameObject>(KitFbxPath);
            if (kit == null)
                throw new Exception("candy_kit.fbx missing at " + KitFbxPath + " — cannot build catalog.");

            // Collect one representative renderer per distinct mesh; exclude terrain/cloud props.
            var seen = new HashSet<int>();
            var representatives = new Dictionary<string, Transform>();
            foreach (var mf in kit.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (IsExcluded(mf.transform)) continue;
                if (ExcludedMeshes.Contains(mf.sharedMesh.name)) continue;
                if (mf.sharedMesh.name.ToLowerInvariant().Contains("_low")) continue; // LOD variants
                if (!seen.Add(mf.sharedMesh.GetInstanceID())) continue;
                if (!representatives.ContainsKey(mf.sharedMesh.name))
                    representatives[mf.sharedMesh.name] = mf.transform;
            }
            foreach (var smr in kit.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null || IsExcluded(smr.transform)) continue;
                if (ExcludedMeshes.Contains(smr.sharedMesh.name)) continue;
                if (smr.sharedMesh.name.ToLowerInvariant().Contains("_low")) continue;
                if (!representatives.ContainsKey(smr.sharedMesh.name))
                    representatives[smr.sharedMesh.name] = smr.transform;
            }

            var sortedNames = representatives.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

            // Starters: prefer Chocolate/Waffer meshes, then fill to exactly 3 in sorted order.
            var starterNames = sortedNames
                .Where(n => n.ToLowerInvariant().Contains("chocolate") || n.ToLowerInvariant().Contains("waffer"))
                .Take(2).ToList();
            foreach (var n in sortedNames)
            {
                if (starterNames.Count >= 3) break;
                if (!starterNames.Contains(n)) starterNames.Add(n);
            }

            var entries = new List<CatalogEntry>();
            var palette = new[]
            {
                UIKit.SugarPink, UIKit.SkyMint, UIKit.Lemon, UIKit.Grape, UIKit.Ice, UIKit.MagnetRed, UIKit.Wind
            };

            int zhIndex = 0;
            int recipeIndex = 0;
            foreach (var meshName in sortedNames)
            {
                var src = representatives[meshName];
                string typeId = Sanitize(meshName);

                // Extract an isolated prefab of this candy node.
                var clone = UnityEngine.Object.Instantiate(src.gameObject);
                clone.transform.SetParent(null, false);
                var srcLossy = src.lossyScale;
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = src.localRotation;
                clone.transform.localScale = srcLossy;
                foreach (var col in clone.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(col);
                foreach (var rb in clone.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(rb);

                string prefabPath = $"{PrefabCandyDir}/Candy_{typeId}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
                UnityEngine.Object.DestroyImmediate(clone);

                bool isStarter = starterNames.Contains(meshName);
                var entry = new CatalogEntry
                {
                    meshName = meshName,
                    typeId = typeId,
                    nameZh = ToZh(meshName, zhIndex),
                    nameEn = ToEn(meshName, zhIndex),
                    isStarter = isStarter,
                    cost = isStarter ? -1 : 120 + recipeIndex * 60,
                    prefab = prefab
                };

                var def = ScriptableObject.CreateInstance<CandyTypeDefinition>();
                def.typeId = entry.typeId;
                def.displayNameZh = entry.nameZh;
                def.displayNameEn = entry.nameEn;
                def.prefab = prefab;
                def.isStarter = isStarter;
                def.chipColor = palette[entries.Count % palette.Length];
                AssetDatabase.CreateAsset(def, $"{CatalogDir}/{entry.typeId}.asset");

                if (!isStarter)
                {
                    var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                    recipe.recipeId = entry.typeId;
                    recipe.candyType = def;
                    recipe.cost = entry.cost;
                    AssetDatabase.CreateAsset(recipe, $"{RecipesDir}/{entry.typeId}.asset");
                    recipeIndex++;
                }

                entries.Add(entry);
                zhIndex++;
            }

            AssetDatabase.SaveAssets();
            return entries;
        }

        private static bool IsExcluded(Transform t)
        {
            while (t != null)
            {
                var lower = t.name.ToLowerInvariant();
                if (lower.Contains("terrain") || lower.Contains("cloud") ||
                    lower.Contains("light") || lower.Contains("camera"))
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static string Sanitize(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            var s = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(s) ? "Candy" : s;
        }

        // ---------------- Power-ups + VFX ----------------

        private static void BuildPowerUpDefinitions()
        {
            var magnetVfx = CreateVfx("Vfx_Magnet", UIKit.MagnetRed, false, ParticleSystemShapeType.Cone);
            var tornadoVfx = CreateVfx("Vfx_Tornado", UIKit.Wind, true, ParticleSystemShapeType.Donut);
            var freezeVfx = CreateVfx("Vfx_Freeze", UIKit.Ice, true, ParticleSystemShapeType.Hemisphere);

            CreatePowerUpDef("magnet", "磁铁", 50, 0f, magnetVfx, UIKit.MagnetRed, "Magnet");
            CreatePowerUpDef("tornado", "龙卷风", 40, 4f, tornadoVfx, new Color(0.55f, 0.85f, 0.65f), "Tornado");
            CreatePowerUpDef("freeze", "冰冻", 35, 5f, freezeVfx, UIKit.Ice, "Freeze");
            AssetDatabase.SaveAssets();
        }

        private static void CreatePowerUpDef(string id, string zh, int cost, float duration, GameObject vfx, Color accent, string en = null)
        {
            var path = $"{PowerUpsDir}/PowerUp_{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<PowerUpDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<PowerUpDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.powerUpId = id;
            def.displayNameZh = zh;
            def.displayNameEn = en ?? id;
            def.buyCost = cost;
            def.effectDuration = duration;
            def.vfxPrefab = vfx;
            def.accentColor = accent;
            EditorUtility.SetDirty(def);
        }

        private static GameObject CreateVfx(string name, Color color, bool loop, ParticleSystemShapeType shape)
        {
            string path = $"{VfxDir}/{name}.prefab";

            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1.2f;
            main.loop = loop;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            main.maxParticles = 200; // mobile budget per spec
            main.gravityModifier = 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            if (loop) emission.rateOverTime = 45f;
            else
            {
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
            }

            var shapeModule = ps.shape;
            shapeModule.shapeType = shape;
            shapeModule.radius = shape == ParticleSystemShapeType.Cone ? 0.25f : 1.6f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            if (renderer.material.shader == null || renderer.material.shader.name != "Particles/Standard Unlit")
                renderer.material = new Material(Shader.Find("Sprites/Default"));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------------- Configs ----------------

        private static void BuildConfigAssets()
        {
            CreateConfig<CustomerOrderConfig>("CustomerOrderConfig", c =>
            {
                c.baseSeconds = 6f; c.secondsPerCandy = 1.15f; c.minSeconds = 10f; c.maxSeconds = 45f;
                c.minTypes = 1; c.maxTypes = 3; c.minTotal = 6; c.maxTotal = 30; c.scaleEveryCustomers = 5;
                c.waitingCount = 2; c.targetInstancesPerType = 12;
                c.buriedHintSeconds = 8f; c.doubleRewardAutoContinueSeconds = 2.5f;
                c.freezeDurationSeconds = 5f; c.tornadoDurationSeconds = 4f; c.magnetMaxPicks = 3;
            });
            CreateConfig<EconomyConfig>("EconomyConfig", c =>
            {
                c.baseReward = 8; c.perCandy = 1; c.speedBonusMax = 24; c.minReward = 10;
                c.perfectBonus = 5; c.dailyCoins = 200; c.streakRecipeDay = 7; c.allUnlockedBonus = 500;
                c.dailyExtraAdCoins = 50; c.recipeBaseCost = 120; c.recipeCostStep = 60; c.adCoinGrant = 80;
            });
            CreateConfig<AdConfig>("AdConfig", c =>
            {
                c.interstitialEnabled = false; c.stubAdDelaySeconds = 0.8f;
                c.minSecondsBetweenRewarded = 45f; c.maxOptionalRewardedPerDay = 8;
                c.maxPowerupBuyAdsPerDay = 6; c.maxRewardCoinsPerDay = 4;
                c.maxDoubleServePerRun = 3; c.maxRevivePerRun = 1;
            });
            CreateConfig<DailyChallengeConfig>("DailyChallengeConfig", c =>
            {
                c.quota = 12; c.biasChance = 0.7f; c.lockedDiscount = 0.2f;
                c.rewardCoins = 120; c.rewardFreezeCount = 1;
            });
            CreateConfig<StaminaConfig>("StaminaConfig", c =>
            {
                c.dailyMax = 20; c.costPerCustomer = 1;
                c.perfectRefund = 1; c.passDelta = 0; c.failPenalty = 3;
            });
            AssetDatabase.SaveAssets();
        }

        private static void CreateConfig<T>(string name, System.Action<T> init) where T : ScriptableObject
        {
            var path = $"{DataRoot}/{name}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<T>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(cfg, path);
            }
            init(cfg);
            EditorUtility.SetDirty(cfg);
        }

        // ---------------- Scenes ----------------

        private static void BuildScenes()
        {
            // Boot
            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = NewCamera("Main Camera");
            var loader = new GameObject("Boot");
            loader.AddComponent<BootLoader>();
            SaveScene(boot, "Assets/Scenes/BootScene.unity");

            // Main Menu
            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NewCamera("Main Camera", new Color(0.96f, 0.9f, 0.82f));
            var menuGo = new GameObject("Menu");
            menuGo.AddComponent<MainMenuController>();
            AddEventSystem();
            SaveScene(menu, "Assets/Scenes/MainMenu.unity");

            // Game
            var game = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gameCam = NewCamera("Main Camera", new Color(0.92f, 0.95f, 0.97f));
            gameCam.transform.position = new Vector3(0f, 7.2f, -6.2f);
            gameCam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.2f, 0f) - gameCam.transform.position);
            var camComp = gameCam.GetComponent<Camera>();
            camComp.fieldOfView = 52f;

            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var root = new GameObject("GameRoot");
            var gm = root.AddComponent<GameManager>();
            var pile = root.AddComponent<CandyPileRestock>();
            var orders = root.AddComponent<CustomerOrderManager>();
            var powerUps = root.AddComponent<PowerUpManager>();
            root.AddComponent<CandyPickController>();
            root.AddComponent<PowerUpVfxPlayer>();

            pile.game = gm;
            orders.pile = pile;
            orders.powerUps = powerUps;
            powerUps.game = gm;
            powerUps.orders = orders;
            powerUps.pile = pile;

            var hudGo = new GameObject("HUD");
            hudGo.AddComponent<GameHUDController>();

            AddEventSystem();
            SaveScene(game, "Assets/Scenes/GameScene.unity");

            // Recipe shop
            var shop = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NewCamera("Main Camera", new Color(0.96f, 0.9f, 0.82f));
            var shopGo = new GameObject("RecipeShop");
            shopGo.AddComponent<RecipeShopController>();
            AddEventSystem();
            SaveScene(shop, "Assets/Scenes/RecipeShopScene.unity");

            // Build settings order: Boot -> MainMenu -> Game -> RecipeShop
            var enabled = EditorBuildSettings.scenes.ToList();
            var wanted = new[]
            {
                "Assets/Scenes/BootScene.unity",
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/GameScene.unity",
                "Assets/Scenes/RecipeShopScene.unity"
            };
            enabled.Clear();
            enabled.AddRange(wanted.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = enabled.ToArray();
        }

        private static GameObject NewCamera(string name)
        {
            return NewCamera(name, new Color(0.92f, 0.95f, 0.97f));
        }

        private static GameObject NewCamera(string name, Color clearColor)
        {
            var go = new GameObject(name, typeof(Camera));
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clearColor;
            cam.tag = "MainCamera";
            return go;
        }

        private static void AddEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<StandaloneInputModule>();
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

        // ---------------- Docs ----------------

        private static void WriteGeneratedDocs(List<CatalogEntry> catalog)
        {
            var genDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "docs/generated"));
            if (!Directory.Exists(genDir)) Directory.CreateDirectory(genDir);

            File.WriteAllText(Path.Combine(genDir, "unity-editor.md"),
                "# Unity Editor\n\nSelected version: **6000.0.77f1**\n\n" +
                "Installed editors found: 2020.3.49f1, 6000.0.68f1, 6000.0.77f1, 6000.3.6f1.\n" +
                "Project pinned to 6000.0.77f1 per project decision.\n");

            var sb = new StringBuilder();
            sb.AppendLine("# Candy Catalog (generated from Assets/Art/Candy/candy_kit.fbx)");
            sb.AppendLine();
            sb.AppendLine("| Mesh | CandyTypeId | Starter / Recipe | Cost | ZH Name |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var e in catalog)
            {
                sb.AppendLine($"| {e.meshName} | {e.typeId} | {(e.isStarter ? "Starter (free)" : "Recipe")} | {(e.isStarter ? "-" : e.cost.ToString())} | {e.nameZh} |");
            }
            sb.AppendLine();
            sb.AppendLine($"Total candy types: **{catalog.Count}** ({catalog.Count(x => x.isStarter)} starters, {catalog.Count(x => !x.isStarter)} recipes).");
            File.WriteAllText(Path.Combine(genDir, "candy-catalog.md"), sb.ToString());
        }
    }
}

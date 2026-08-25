using System.IO;
using UnityEditor;
using UnityEngine;

namespace CandyShop.EditorTools
{
    // Renders one 256x256 transparent icon per CandyTypeId straight from its 3D prefab
    // (art bible section 4.3 prefers a mesh screenshot over invented drawings).
    // Run: CandyShop menu or batchmode -executeMethod CandyShop.EditorTools.CandyIconRenderer.RenderAll
    public static class CandyIconRenderer
    {
        private const int Size = 256;
        private const string OutputDir = "Assets/Resources/UI/Candies";

        [MenuItem("CandyShop/Render Candy Icons")]
        public static void RenderAll()
        {
            if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
            var catalog = AssetDatabase.LoadAssetAtPath<CandyTypeDefinition>(
                "Assets/Resources/Data/Catalog/balloon_A.asset") != null
                ? Resources.LoadAll<CandyTypeDefinition>("Data/Catalog")
                : new CandyTypeDefinition[0];

            // Offscreen stage: camera + warm bakery light on a transparent background.
            var stage = new GameObject("IconStage");
            var camGo = new GameObject("IconCam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.transform.SetParent(stage.transform);
            cam.clearFlags = CameraClearFlags.Depth; // keep alpha
            cam.orthographic = true;

            var lightGo = new GameObject("IconLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f); // warm bakery key
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            lightGo.transform.SetParent(stage.transform);

            RenderTexture rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            int count = 0;
            try
            {
                foreach (var def in catalog)
                {
                    if (def == null || def.prefab == null) continue;
                    string path = $"{OutputDir}/icon_candy_{def.typeId}.png";
                    if (File.Exists(path)) continue; // keep hand-tuned icons on re-runs

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab);
                    try
                    {
                        var bounds = ComputeBounds(instance);
                        instance.transform.position = -bounds.center;

                        cam.targetTexture = rt;
                        float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 2.15f;
                        cam.orthographicSize = extent;
                        cam.transform.position = new Vector3(0f, extent * 0.35f, -extent * 2f);
                        cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -extent * 0.15f, 1f));
                        cam.Render();

                        RenderTexture.active = rt;
                        var tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false);
                        tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                        tex.Apply();
                        RenderTexture.active = null;

                        File.WriteAllBytes(path, tex.EncodeToPNG());
                        Object.DestroyImmediate(tex);
                        count++;
                    }
                    finally
                    {
                        Object.DestroyImmediate(instance);
                    }
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(stage);
                Object.DestroyImmediate(rt);
                AssetDatabase.Refresh();
            }

            Debug.Log($"CandyIconRenderer: rendered {count} icons to {OutputDir}");
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}

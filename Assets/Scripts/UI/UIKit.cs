using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CandyShop
{
    // Shared UI helpers: art-bible palette, cartoon sprites from Resources/UI, rounded widgets.
    public static class UIKit
    {
        // Palette (design spec section 2.1)
        public static readonly Color Cream = FromHex("FFF6E8");
        public static readonly Color SugarPink = FromHex("FF8FB8");
        public static readonly Color Berry = FromHex("E85A8C");
        public static readonly Color SkyMint = FromHex("7EE0C6");
        public static readonly Color Lemon = FromHex("FFE07A");
        public static readonly Color Cocoa = FromHex("6B3F2A");
        public static readonly Color Grape = FromHex("A78BFA");
        public static readonly Color Ice = FromHex("B8E8FF");
        public static readonly Color MagnetRed = FromHex("FF6B6B");
        public static readonly Color Wind = FromHex("C8F5D4");

        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "SimHei", "Arial" }, 32);
                    if (_font == null)
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        public static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        // ---- Canvas ----

        public static Canvas CreateCanvas(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f; // spec section 2: reference 1080x1920, match height
            return canvas;
        }

        // ---- Rounded sprite (soft cookie look without external assets) ----

        private static Texture2D _roundedTex;
        private static Sprite _roundedSprite;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> _spriteCache =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        // Loads a PNG from Assets/Resources/UI (Texture2D -> Sprite). Missing files fall back to the rounded cookie.
        public static Sprite LoadSprite(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return RoundedSprite();
            Sprite cached;
            if (_spriteCache.TryGetValue(relativePath, out cached) && cached != null) return cached;
            var spriteAsset = Resources.Load<Sprite>("UI/" + relativePath);
            if (spriteAsset != null)
            {
                _spriteCache[relativePath] = spriteAsset;
                return spriteAsset;
            }
            var tex = Resources.Load<Texture2D>("UI/" + relativePath);
            if (tex == null) return RoundedSprite();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _spriteCache[relativePath] = sprite;
            return sprite;
        }

        public static Image CreateIcon(Transform parent, string resourcePath, Vector2 size)
        {
            var go = new GameObject("Icon_" + resourcePath.Replace('/', '_'), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite(resourcePath);
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            return img;
        }

        public static Image CreateBackground(Transform canvasRoot, string resourcePath)
        {
            var go = new GameObject("ArtBackground", typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            go.transform.SetAsFirstSibling();
            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite(resourcePath);
            img.preserveAspect = false;
            img.raycastTarget = false;
            img.color = Color.white;
            Stretch((RectTransform)go.transform, canvasRoot);
            return img;
        }

        // Map a catalog CandyTypeId to an icon under Resources/UI/Candies.
        // Prefers the exact per-type icon rendered from its 3D mesh (art bible 4.3);
        // falls back to the family icon when that type has no generated icon yet.
        public static string CandyIconPath(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return "Candies/icon_candy_candy";
            var exact = "Candies/icon_candy_" + typeId;
            if (_spriteCache.ContainsKey(exact)) return exact;
            if (Resources.Load<Sprite>("UI/" + exact) != null ||
                Resources.Load<Texture2D>("UI/" + exact) != null)
                return exact;
            var id = typeId.ToLowerInvariant();
            if (id.StartsWith("balloon")) return "Candies/icon_candy_balloon";
            if (id.StartsWith("cake") || id.StartsWith("swiss")) return "Candies/icon_candy_cake";
            if (id.StartsWith("chocolate")) return "Candies/icon_candy_chocolate";
            if (id.StartsWith("cookie") || id.StartsWith("sweet_bread")) return "Candies/icon_candy_cookie";
            if (id.StartsWith("cotton")) return "Candies/icon_candy_cottoncandy";
            if (id.StartsWith("cupcake")) return "Candies/icon_candy_cupcake";
            if (id.StartsWith("donut") || id.StartsWith("sweet_ring")) return "Candies/icon_candy_donut";
            if (id.StartsWith("icecream")) return "Candies/icon_candy_icecream";
            if (id.StartsWith("jelly")) return "Candies/icon_candy_jelly";
            if (id.StartsWith("lollipop")) return "Candies/icon_candy_lollipop";
            if (id.StartsWith("milkshake")) return "Candies/icon_candy_milkshake";
            if (id.StartsWith("mm")) return "Candies/icon_candy_mm";
            if (id.StartsWith("popsicle")) return "Candies/icon_candy_popsicle";
            if (id.StartsWith("pretzel")) return "Candies/icon_candy_pretzel";
            if (id.StartsWith("straw") || id.StartsWith("cherry") || id.Contains("strawberry"))
                return "Candies/icon_candy_strawberry";
            if (id.StartsWith("waffle")) return "Candies/icon_candy_waffle";
            if (id.StartsWith("waffer")) return "Candies/icon_candy_waffer";
            return "Candies/icon_candy_candy";
        }

        public static Sprite RoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            const int size = 64;
            const int radius = 18;
            if (_roundedTex == null)
            {
                _roundedTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                _roundedTex.wrapMode = TextureWrapMode.Clamp;
                float cx = radius - 0.5f, cy = radius - 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool inside = true;
                        // Corner check for each of the four corners.
                        int dx = Mathf.Min(x, size - 1 - x);
                        int dy = Mathf.Min(y, size - 1 - y);
                        if (dx < radius && dy < radius)
                        {
                            float fx = radius - dx - 0.5f;
                            float fy = radius - dy - 0.5f;
                            inside = fx * fx + fy * fy <= radius * radius + 1f;
                        }
                        _roundedTex.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                }
                _roundedTex.Apply();
            }
            if (_roundedSprite == null)
                _roundedSprite = Sprite.Create(_roundedTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _roundedSprite;
        }

        // ---- Widgets ----

        // Art-bible sprite names under Resources/UI (9-slice imports).
        public const string PanelSprite = "panel_cream";
        public const string ButtonPrimary = "btn_primary";
        public const string ButtonSecondary = "btn_secondary";

        // When spriteName is given, the panel draws that 9-slice art sprite; pass
        // Color.white to keep the art's own colors. Otherwise falls back to the
        // rounded cookie (transparent containers / small dots).
        public static RectTransform CreatePanel(Transform parent, string name, Color color,
            bool rounded = true, string spriteName = null)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            if (!string.IsNullOrEmpty(spriteName))
            {
                img.sprite = LoadSprite(spriteName);
                img.type = Image.Type.Sliced;
            }
            else
            {
                if (rounded) img.sprite = RoundedSprite();
                img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.color = color;
            img.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        public static Text CreateText(Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject("Text_" + content, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            // Cocoa outline keeps text readable on the busy cartoon art.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.25f, 0.16f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        // Buttons draw the candy 9-slice art (btn_primary / btn_secondary). The bg color
        // is ignored for art buttons (pass Color.white); state changes tint via image.color.
        public static Button CreateButton(Transform parent, string label, Vector2 size, Color bg,
            int fontSize = 40, Color? textColor = null, string spriteName = ButtonPrimary)
        {
            var go = new GameObject("Btn_" + label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            if (!string.IsNullOrEmpty(spriteName))
            {
                img.sprite = LoadSprite(spriteName);
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.sprite = RoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = bg;
            }

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;

            var txt = CreateText(go.transform, label, fontSize, textColor ?? Color.white);
            var tr = (RectTransform)txt.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(16, 8);
            tr.offsetMax = new Vector2(-16, -8);

            return btn;
        }

        public static void Stretch(RectTransform rt, Transform parent)
        {
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}

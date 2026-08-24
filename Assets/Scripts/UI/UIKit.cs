using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CandyShop
{
    // Shared UI helpers: art-bible palette, rounded cookie-like panels and simple widgets.
    // Art PNGs are optional in MVP; everything is tinted from the same palette so the kit stays coherent.
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
            scaler.matchWidthOrHeight = 0f; // match width keeps thumb-zone buttons reachable on tall phones
            return canvas;
        }

        // ---- Rounded sprite (soft cookie look without external assets) ----

        private static Texture2D _roundedTex;
        private static Sprite _roundedSprite;

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

        public static RectTransform CreatePanel(Transform parent, string name, Color color, bool rounded = true)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            if (rounded) img.sprite = RoundedSprite();
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
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
            return t;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 size, Color bg,
            int fontSize = 40, Color? textColor = null)
        {
            var go = new GameObject("Btn_" + label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.sprite = RoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = bg;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = Berry;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;

            var txt = CreateText(go.transform, label, fontSize, textColor ?? Color.white);
            var tr = (RectTransform)txt.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

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

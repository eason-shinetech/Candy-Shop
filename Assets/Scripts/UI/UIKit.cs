using UnityEngine;

namespace CandyShop
{
    // Shared UI constants and lookups only. Layout lives in prefabs under
    // Assets/Prefabs/UI — nothing in here may create GameObjects.
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

        // Art-bible sprite names under Resources/UI (9-slice imports).
        public const string PanelSprite = "panel_cream";
        public const string ButtonPrimary = "btn_primary";
        public const string ButtonSecondary = "btn_secondary";

        public static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

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

        // Prefers the catalog Sprite (Assets/Art/Candy Icon, bound on CandyTypeDefinition).
        public static Sprite CandyIcon(CandyTypeDefinition def)
        {
            if (def != null && def.icon != null) return def.icon;
            return LoadSprite(CandyIconPath(def != null ? def.typeId : null));
        }

        // Legacy Resources/UI/Candies lookup used only when the catalog icon is missing.
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

        private static Sprite _roundedSprite;

        // Soft cookie shape used by badges / FX dots. Prefabs reference the imported
        // Resources/UI/ui_rounded sprite; the procedural texture is only a fallback.
        public static Sprite RoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            _roundedSprite = Resources.Load<Sprite>("UI/ui_rounded");
            if (_roundedSprite != null) return _roundedSprite;

            const int size = 64;
            const int radius = 18;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
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
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            _roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _roundedSprite;
        }
    }
}

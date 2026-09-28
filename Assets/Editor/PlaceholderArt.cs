using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vela.EditorTools
{
    /// Generates the placeholder pixel art (PNG files under Assets/Art/Placeholder) the first
    /// time the scene is built. They are ordinary sprite assets: replace a PNG in place, or
    /// point a config's `visual.sprite` at your own sprite.
    public static partial class PlaceholderArt
    {
        public const string Folder = "Assets/Art/Placeholder";

        private static readonly Color32 Outline = Hex("1a1423");

        // ------------------------------------------------------------------ characters

        public static Sprite Player() => GetOrCreateSprite("Player", 16, 24, c =>
        {
            c.Rect(5, 0, 6, 4, Hex("3b2a2a"));
            c.Rect(9, 0, 10, 4, Hex("3b2a2a"));
            c.Rect(3, 5, 12, 7, Hex("21867a"));
            c.Rect(4, 5, 11, 13, Hex("2a9d8f"));
            c.Rect(4, 8, 11, 8, Hex("6b4226"));
            c.Rect(4, 13, 11, 14, Hex("e76f51"));
            c.Rect(12, 11, 13, 13, Hex("e76f51"));
            c.Ellipse(8, 18, 4, 4, Hex("f4c7a1"));
            c.Ellipse(8, 20, 4, 3, Hex("264653"));
            c.Rect(4, 19, 5, 21, Hex("264653"));
            c.Set(10, 18, Hex("1a1423"));
            c.Set(11, 17, Hex("e9967a"));
            c.Shade(0.8f);
            c.AddOutline(Outline);
        });

        public static Sprite Slime() => GetOrCreateSprite("Slime", 18, 13, c =>
        {
            c.Ellipse(9, 5, 8, 6, Hex("6ab04c"));
            c.Rect(1, 0, 16, 2, Hex("6ab04c"));
            c.Ellipse(5, 8, 2, 1, Hex("badc58"));
            c.Rect(11, 5, 11, 7, Hex("1a1423"));
            c.Rect(14, 5, 14, 7, Hex("1a1423"));
            c.Set(11, 7, Hex("ffffff"));
            c.Shade(0.75f);
            c.AddOutline(Outline);
        });

        public static Sprite SkeletonArcher() => GetOrCreateSprite("SkeletonArcher", 17, 24, c =>
        {
            var bone = Hex("e8e2d0");
            c.Rect(6, 0, 6, 5, bone);
            c.Rect(9, 0, 9, 5, bone);
            c.Rect(5, 6, 10, 7, bone);
            c.Rect(7, 8, 8, 14, bone);
            for (var y = 9; y <= 13; y += 2) c.Rect(5, y, 10, y, bone);
            c.Ellipse(8, 18, 4, 4, bone);
            c.Rect(6, 14, 10, 15, bone);
            c.Rect(9, 18, 10, 19, Hex("2b1d1d"));
            c.Set(10, 18, Hex("ff4d4d"));
            c.Rect(7, 16, 10, 16, Hex("2b1d1d"));
            // Bow
            for (var y = 4; y <= 20; y++)
            {
                var bend = Mathf.RoundToInt(Mathf.Sin((y - 4) / 16f * Mathf.PI) * 2f);
                c.Set(13 + bend, y, Hex("8b5a2b"));
            }
            c.Rect(13, 4, 13, 20, Hex("d8d0c0"));
            c.Rect(11, 11, 14, 11, Hex("e8e2d0"));
            c.Shade(0.8f);
            c.AddOutline(Outline);
        });

        public static Sprite Brute() => GetOrCreateSprite("Brute", 28, 28, c =>
        {
            var body = Hex("7d5a8c");
            c.Rect(7, 0, 10, 4, Hex("4a3456"));
            c.Rect(17, 0, 20, 4, Hex("4a3456"));
            c.Ellipse(14, 11, 11, 8, body);
            c.Ellipse(14, 8, 6, 4, Hex("a383b0"));
            c.Ellipse(14, 20, 6, 4, body);
            c.Ellipse(3, 8, 3, 3, Hex("5e4270"));
            c.Ellipse(25, 8, 3, 3, Hex("5e4270"));
            c.Rect(7, 22, 8, 26, Hex("e8e2d0"));
            c.Rect(20, 22, 21, 26, Hex("e8e2d0"));
            c.Set(6, 26, Hex("e8e2d0"));
            c.Set(22, 26, Hex("e8e2d0"));
            c.Rect(15, 19, 16, 20, Hex("ffd23f"));
            c.Rect(19, 19, 20, 20, Hex("ffd23f"));
            c.Rect(15, 17, 20, 17, Hex("2b1d2f"));
            c.Shade(0.75f);
            c.AddOutline(Outline);
        });

        public static Sprite Bat() => GetOrCreateSprite("Bat", 22, 13, c =>
        {
            var wing = Hex("3b2c5a");
            for (var x = 0; x <= 7; x++)
            {
                var top = 12 - x / 2;
                var bottom = 4 + (x % 3 == 0 ? 1 : 0) + x / 3;
                c.Rect(x, bottom, x, top, wing);
                c.Rect(21 - x, bottom, 21 - x, top, wing);
            }
            c.Ellipse(11, 6, 3, 4, Hex("5b4387"));
            c.Rect(9, 10, 9, 11, Hex("5b4387"));
            c.Rect(13, 10, 13, 11, Hex("5b4387"));
            c.Set(10, 7, Hex("ff4d4d"));
            c.Set(12, 7, Hex("ff4d4d"));
            c.Set(10, 4, Hex("ffffff"));
            c.Set(12, 4, Hex("ffffff"));
            c.Shade(0.8f);
            c.AddOutline(Outline);
        });

        public static Sprite Cultist() => GetOrCreateSprite("Cultist", 18, 27, c =>
        {
            for (var y = 0; y <= 16; y++)
            {
                var half = Mathf.RoundToInt(Mathf.Lerp(6f, 3.5f, y / 16f));
                c.Rect(8 - half, y, 8 + half, y, Hex("9b2335"));
            }
            c.Rect(7, 0, 9, 14, Hex("7a1b2a"));
            c.Ellipse(8, 19, 5, 5, Hex("6d1a28"));
            c.Ellipse(9, 18, 3, 3, Hex("150a10"));
            c.Set(8, 18, Hex("ffd23f"));
            c.Set(10, 18, Hex("ffd23f"));
            c.Rect(15, 0, 15, 22, Hex("6b4226"));
            c.Ellipse(15, 24, 2, 2, Hex("c77dff"));
            c.Set(15, 25, Hex("f3e8ff"));
            c.Shade(0.8f);
            c.AddOutline(Outline);
        });

        public static Sprite TrainingDummy() => GetOrCreateSprite("TrainingDummy", 18, 24, c =>
        {
            c.Rect(8, 0, 9, 10, Hex("6b4226"));
            c.Ellipse(9, 13, 5, 6, Hex("d4a373"));
            c.Rect(1, 14, 16, 15, Hex("c8965f"));
            c.Ellipse(9, 20, 3, 3, Hex("d4a373"));
            c.Ellipse(9, 12, 2, 2, Hex("c0392b"));
            c.Set(9, 12, Hex("ffffff"));
            c.Rect(6, 17, 12, 17, Hex("8d6e4a"));
            c.Shade(0.8f);
            c.AddOutline(Outline);
        });

        public static Sprite Warden() => GetOrCreateSprite("HollowWarden", 38, 46, c =>
        {
            var steel = Hex("5c6784");
            var dark = Hex("3d405b");
            c.Rect(10, 0, 15, 9, dark);
            c.Rect(21, 0, 26, 9, dark);
            c.Rect(8, 10, 28, 28, steel);
            c.Rect(11, 13, 25, 25, Hex("7b88a8"));
            c.Ellipse(18, 19, 3, 3, Hex("7df9ff"));
            c.Set(18, 19, Hex("ffffff"));
            c.Ellipse(7, 27, 5, 4, dark);
            c.Ellipse(29, 27, 5, 4, dark);
            c.Rect(3, 12, 6, 24, steel);
            c.Rect(3, 10, 6, 11, dark);
            c.Ellipse(18, 35, 6, 6, dark);
            c.Rect(14, 34, 22, 35, Hex("ff4d4d"));
            c.Rect(10, 38, 12, 44, Hex("e8e2d0"));
            c.Rect(24, 38, 26, 44, Hex("e8e2d0"));
            c.Set(9, 44, Hex("e8e2d0"));
            c.Set(27, 44, Hex("e8e2d0"));
            // Great blade
            c.Rect(32, 2, 34, 32, Hex("cfd8dc"));
            c.Rect(33, 3, 33, 31, Hex("ffffff"));
            c.Rect(30, 12, 36, 13, Hex("6b4226"));
            c.Rect(32, 33, 34, 33, Hex("cfd8dc"));
            c.Set(33, 34, Hex("cfd8dc"));
            c.Shade(0.8f);
            c.AddOutline(Outline);
        });

        // ------------------------------------------------------------------ projectiles

        public static Sprite Arrow() => GetOrCreateSprite("Arrow", 16, 5, c =>
        {
            c.Rect(2, 2, 12, 2, Hex("e0c9a6"));
            c.Rect(13, 1, 13, 3, Hex("dfe6e9"));
            c.Rect(14, 1, 14, 3, Hex("dfe6e9"));
            c.Set(15, 2, Hex("dfe6e9"));
            c.Rect(0, 0, 2, 0, Color.white);
            c.Rect(0, 4, 2, 4, Color.white);
            c.Set(1, 1, Color.white);
            c.Set(1, 3, Color.white);
        }, new Vector2(0.5f, 0.5f));

        public static Sprite Orb() => GetOrCreateSprite("Orb", 8, 8, c =>
        {
            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(4f, 4f)) / 4f;
                if (d > 1f) continue;
                var a = (byte)Mathf.RoundToInt(255 * Mathf.Clamp01(1.3f - d));
                c.Set(x, y, new Color32(255, 255, 255, a));
            }
        }, new Vector2(0.5f, 0.5f));

        public static Texture2D SoftCircle() => GetOrCreateTexture("SoftCircle", 32, 32, c =>
        {
            for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                var a = Mathf.Clamp01(1f - d);
                c.Set(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);

        // ------------------------------------------------------------------ ground & walls

        public static Texture2D Water() => Tile("Water", Hex("3a7ca5"), 0.05f, (c, rng) =>
        {
            for (var i = 0; i < 10; i++)
            {
                int x = rng.Next(28), y = rng.Next(32);
                c.Rect(x, y, x + 3, y, Hex("7fb8d8"));
            }
            for (var i = 0; i < 6; i++) c.Set(rng.Next(32), rng.Next(32), Hex("cfe9f5"));
        });

        public static Texture2D Grass() => Tile("Grass", Hex("5a8f3c"), 0.07f, (c, rng) =>
        {
            for (var i = 0; i < 26; i++)
            {
                int x = rng.Next(32), y = rng.Next(31);
                var blade = rng.NextDouble() < 0.5 ? Hex("7cb342") : Hex("44702d");
                c.Set(x, y, blade);
                c.Set(x, y + 1, blade);
            }
            for (var i = 0; i < 3; i++) c.Set(rng.Next(32), rng.Next(32), Hex("f1e3a0"));
        });

        public static Texture2D Path() => Tile("Path", Hex("a58a61"), 0.08f, (c, rng) =>
        {
            for (var i = 0; i < 14; i++)
            {
                int x = rng.Next(31), y = rng.Next(31);
                c.Rect(x, y, x + 1, y, Hex("8a7250"));
                c.Set(x, y + 1, Hex("c2a77c"));
            }
        });

        public static Texture2D Stone() => Tile("Stone", Hex("7a7f8a"), 0.06f, (c, rng) =>
        {
            var mortar = Hex("4f535c");
            for (var row = 0; row < 4; row++)
            {
                var y = row * 8;
                c.Rect(0, y, 31, y, mortar);
                var offset = row % 2 == 0 ? 0 : 8;
                for (var x = offset; x < 32; x += 16) c.Rect(x, y, x, y + 7, mortar);
                c.Rect(0, y + 7, 31, y + 7, Hex("9aa0ab"));
            }
        });

        public static Texture2D ArenaFloor() => Tile("ArenaFloor", Hex("55586a"), 0.05f, (c, rng) =>
        {
            var seam = Hex("3b3d4a");
            c.Rect(0, 0, 31, 0, seam);
            c.Rect(0, 16, 31, 16, seam);
            c.Rect(0, 0, 0, 31, seam);
            c.Rect(16, 0, 16, 31, seam);
            for (var i = 0; i < 6; i++) c.Set(rng.Next(32), rng.Next(32), Hex("6f7386"));
        });

        public static Texture2D Bark() => Tile("Bark", Hex("6b4a2f"), 0.05f, (c, rng) =>
        {
            for (var x = 0; x < 32; x += 4) c.Rect(x, 0, x, 31, Hex("4f3521"));
        });

        public static Texture2D Leaves() => Tile("Leaves", Hex("3f7d3a"), 0.1f, (c, rng) =>
        {
            for (var i = 0; i < 20; i++)
            {
                int x = rng.Next(30), y = rng.Next(30);
                c.Rect(x, y, x + 2, y + 1, rng.NextDouble() < 0.5 ? Hex("5aa04f") : Hex("2d5a2a"));
            }
        });

        private static Texture2D Tile(string name, Color32 baseColor, float noise, Action<PixelCanvas, System.Random> detail)
        {
            return GetOrCreateTexture(name, 32, 32, c =>
            {
                var rng = new System.Random(name.GetHashCode());
                for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var n = 1f + ((float)rng.NextDouble() * 2f - 1f) * noise;
                    var col = (Color)baseColor * n;
                    col.a = 1f;
                    c.Set(x, y, col);
                }
                detail(c, rng);
            }, FilterMode.Point, TextureWrapMode.Repeat);
        }

        // ------------------------------------------------------------------ asset plumbing

        public static bool Regenerate { get; set; }

        private static Sprite GetOrCreateSprite(string name, int width, int height, Action<PixelCanvas> paint,
            Vector2? pivot = null)
        {
            var path = $"{Folder}/{name}.png";
            if (Regenerate || !File.Exists(path))
            {
                var canvas = new PixelCanvas(width, height);
                paint(canvas);
                WritePng(path, canvas);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot ?? new Vector2(0.5f, 0f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D GetOrCreateTexture(string name, int width, int height, Action<PixelCanvas> paint,
            FilterMode filter, TextureWrapMode wrap)
        {
            var path = $"{Folder}/{name}.png";
            if (Regenerate || !File.Exists(path))
            {
                var canvas = new PixelCanvas(width, height);
                paint(canvas);
                WritePng(path, canvas);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = filter;
                importer.wrapMode = wrap;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = filter != FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void WritePng(string path, PixelCanvas canvas)
        {
            EnsureFolder(Folder);
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(canvas.Pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        /// Tiny pixel painter. (0,0) is bottom-left.
        private class PixelCanvas
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Color32[] Pixels;

            public PixelCanvas(int width, int height)
            {
                Width = width;
                Height = height;
                Pixels = new Color32[width * height];
            }

            public void Set(int x, int y, Color32 color)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height) return;
                Pixels[y * Width + x] = color;
            }

            public Color32 Get(int x, int y) =>
                x < 0 || y < 0 || x >= Width || y >= Height ? new Color32(0, 0, 0, 0) : Pixels[y * Width + x];

            public void Rect(int x0, int y0, int x1, int y1, Color32 color)
            {
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    Set(x, y, color);
            }

            public void Clear(int x, int y) => Set(x, y, new Color32(0, 0, 0, 0));

            public void ClearEllipse(int cx, int cy, int rx, int ry) => Ellipse(cx, cy, rx, ry, new Color32(0, 0, 0, 0));

            /// Thick line (Bresenham, stamped `thickness` px wide).
            public void Line(int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
            {
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                var err = dx + dy;
                while (true)
                {
                    Rect(x0, y0, x0 + thickness - 1, y0 + thickness - 1, color);
                    if (x0 == x1 && y0 == y1) break;
                    var e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

            public void Ellipse(int cx, int cy, int rx, int ry, Color32 color)
            {
                for (var y = cy - ry; y <= cy + ry; y++)
                for (var x = cx - rx; x <= cx + rx; x++)
                {
                    var dx = (x - cx) / (rx + 0.5f);
                    var dy = (y - cy) / (ry + 0.5f);
                    if (dx * dx + dy * dy <= 1f) Set(x, y, color);
                }
            }

            /// Darkens the lower-left of the silhouette a touch for a sense of light.
            public void Shade(float darkest)
            {
                for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                {
                    var p = Get(x, y);
                    if (p.a == 0) continue;
                    var t = Mathf.Lerp(darkest, 1f, (float)y / Height * 0.7f + (float)x / Width * 0.3f);
                    Set(x, y, new Color32((byte)(p.r * t), (byte)(p.g * t), (byte)(p.b * t), p.a));
                }
            }

            /// One-pixel dark outline around everything opaque.
            public void AddOutline(Color32 color)
            {
                var copy = (Color32[])Pixels.Clone();
                for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                {
                    if (copy[y * Width + x].a != 0) continue;
                    var neighbour = Opaque(copy, x - 1, y) || Opaque(copy, x + 1, y) ||
                                    Opaque(copy, x, y - 1) || Opaque(copy, x, y + 1);
                    if (neighbour) Set(x, y, color);
                }
            }

            private bool Opaque(Color32[] source, int x, int y) =>
                x >= 0 && y >= 0 && x < Width && y < Height && source[y * Width + x].a > 0;
        }
    }
}

using UnityEngine;
using Vela.Visual;

namespace Vela.EditorTools
{
    /// Placeholder paper-doll art: a 16x24 base body in 3 facings (down / up / side) and matching
    /// equipment layers. Every layer uses the same canvas and bottom-center pivot as the body, so
    /// they line up with zero offsets — the rule your real art must follow too.
    /// Equipment is drawn in light grey and colored by EquipmentVisual.tint (palette swap), so one
    /// helmet shape gives iron, gold, leather... variants for free.
    public static partial class PlaceholderArt
    {
        private const int DollW = 16;
        private const int DollH = 24;

        private static readonly Color32 Skin = Hex("f4c7a1");
        private static readonly Color32 SkinShade = Hex("d9a47f");
        private static readonly Color32 Tunic = Hex("7a8494");
        private static readonly Color32 Pants = Hex("3f4a5a");
        private static readonly Color32 Eye = Hex("1a1423");
        private static readonly Color32 Light = Hex("e0e0e0");
        private static readonly Color32 Mid = Hex("b4b4b4");
        private static readonly Color32 Dark = Hex("8a8a8a");

        private static Sprite Doll(string name, System.Action<PixelCanvas> paint) =>
            GetOrCreateSprite("Doll_" + name, DollW, DollH, c =>
            {
                paint(c);
                c.Shade(0.82f);
                c.AddOutline(Outline);
            });

        private static DirectionalSprites Set3(string name, System.Action<PixelCanvas> down, System.Action<PixelCanvas> up,
            System.Action<PixelCanvas> side) => new DirectionalSprites
        {
            down = Doll(name + "_Down", down),
            up = Doll(name + "_Up", up),
            side = Doll(name + "_Side", side)
        };

        // ------------------------------------------------------------------ base body

        public static DirectionalSprites BodySprites() => Set3("Body",
            c =>
            {
                c.Rect(5, 1, 6, 5, Pants);
                c.Rect(9, 1, 10, 5, Pants);
                c.Rect(5, 0, 6, 0, Eye);
                c.Rect(9, 0, 10, 0, Eye);
                c.Rect(4, 6, 11, 13, Tunic);
                c.Rect(3, 7, 3, 12, Skin);
                c.Rect(12, 7, 12, 12, Skin);
                c.Rect(7, 14, 8, 14, SkinShade);
                c.Ellipse(8, 18, 4, 4, Skin);
                c.Set(6, 18, Eye);
                c.Set(10, 18, Eye);
                c.Set(8, 16, SkinShade);
            },
            c =>
            {
                c.Rect(5, 1, 6, 5, Pants);
                c.Rect(9, 1, 10, 5, Pants);
                c.Rect(5, 0, 6, 0, Eye);
                c.Rect(9, 0, 10, 0, Eye);
                c.Rect(4, 6, 11, 13, Tunic);
                c.Rect(3, 7, 3, 12, Skin);
                c.Rect(12, 7, 12, 12, Skin);
                c.Rect(7, 14, 8, 14, SkinShade);
                c.Ellipse(8, 18, 4, 4, Skin);
            },
            c =>
            {
                c.Rect(6, 1, 7, 5, Hex("323b49"));
                c.Rect(8, 1, 9, 5, Pants);
                c.Rect(6, 0, 7, 0, Eye);
                c.Rect(8, 0, 10, 0, Eye);
                c.Rect(5, 6, 10, 13, Tunic);
                c.Rect(8, 7, 9, 12, Skin);
                c.Rect(7, 14, 8, 14, SkinShade);
                c.Ellipse(8, 18, 4, 4, Skin);
                c.Set(10, 18, Eye);
                c.Set(12, 17, SkinShade);
            });

        public static DirectionalSprites HairSprites() => Set3("Hair",
            c =>
            {
                c.Ellipse(8, 20, 4, 3, Light);
                c.Rect(4, 16, 4, 20, Light);
                c.Rect(12, 16, 12, 20, Light);
                c.Rect(5, 19, 11, 19, Mid);
                c.Clear(8, 19);
            },
            c =>
            {
                c.Ellipse(8, 18, 4, 4, Light);
                c.Rect(5, 14, 11, 15, Light);
                c.Rect(6, 16, 10, 16, Mid);
            },
            c =>
            {
                c.Ellipse(7, 20, 4, 3, Light);
                c.Rect(4, 15, 6, 20, Light);
                c.Rect(9, 19, 11, 19, Mid);
            });

        // ------------------------------------------------------------------ head

        public static DirectionalSprites HoodSprites() => Set3("Hood",
            c =>
            {
                c.Ellipse(8, 19, 5, 5, Light);
                c.Rect(4, 13, 11, 14, Mid);
                c.ClearEllipse(8, 17, 3, 3);
                c.Rect(5, 20, 11, 20, Mid);
            },
            c =>
            {
                c.Ellipse(8, 19, 5, 5, Light);
                c.Rect(4, 13, 11, 15, Mid);
                c.Rect(8, 16, 8, 22, Mid);
            },
            c =>
            {
                c.Ellipse(7, 19, 5, 5, Light);
                c.Rect(4, 13, 10, 14, Mid);
                c.ClearEllipse(10, 17, 2, 3);
            });

        public static DirectionalSprites HelmSprites() => Set3("Helm",
            c =>
            {
                c.Ellipse(8, 20, 5, 3, Light);
                c.Rect(3, 16, 4, 20, Mid);
                c.Rect(12, 16, 13, 20, Mid);
                c.Rect(8, 16, 8, 19, Mid);
                c.Rect(4, 20, 12, 20, Dark);
            },
            c =>
            {
                c.Ellipse(8, 19, 5, 4, Light);
                c.Rect(4, 15, 12, 16, Mid);
                c.Rect(8, 17, 8, 23, Dark);
            },
            c =>
            {
                c.Ellipse(8, 20, 5, 3, Light);
                c.Rect(3, 15, 6, 20, Mid);
                c.Rect(11, 16, 11, 19, Mid);
                c.Rect(4, 20, 12, 20, Dark);
            });

        public static DirectionalSprites CrownSprites() => Set3("Crown",
            c =>
            {
                c.Rect(4, 21, 12, 22, Light);
                c.Set(4, 23, Light);
                c.Set(8, 23, Light);
                c.Set(12, 23, Light);
                c.Set(8, 21, Hex("ff4d6d"));
                c.Set(5, 21, Hex("7df9ff"));
                c.Set(11, 21, Hex("7df9ff"));
            },
            c =>
            {
                c.Rect(4, 21, 12, 22, Light);
                c.Set(4, 23, Light);
                c.Set(8, 23, Light);
                c.Set(12, 23, Light);
            },
            c =>
            {
                c.Rect(5, 21, 11, 22, Light);
                c.Set(5, 23, Light);
                c.Set(8, 23, Light);
                c.Set(11, 23, Light);
                c.Set(10, 21, Hex("ff4d6d"));
            });

        // ------------------------------------------------------------------ chest

        public static DirectionalSprites VestSprites() => Set3("Vest",
            c =>
            {
                c.Rect(4, 6, 11, 13, Light);
                c.Rect(7, 8, 8, 13, Mid);
                c.Rect(4, 7, 11, 7, Dark);
                c.Rect(5, 13, 6, 13, Mid);
                c.Rect(9, 13, 10, 13, Mid);
            },
            c =>
            {
                c.Rect(4, 6, 11, 13, Light);
                c.Rect(4, 7, 11, 7, Dark);
                c.Rect(7, 9, 8, 12, Mid);
            },
            c =>
            {
                c.Rect(5, 6, 10, 13, Light);
                c.Rect(5, 7, 10, 7, Dark);
                c.Rect(9, 9, 10, 13, Mid);
            });

        public static DirectionalSprites PlateSprites() => Set3("Plate",
            c =>
            {
                c.Rect(4, 6, 11, 14, Light);
                c.Ellipse(3, 12, 2, 2, Mid);
                c.Ellipse(12, 12, 2, 2, Mid);
                c.Rect(4, 7, 11, 7, Dark);
                c.Rect(7, 9, 8, 13, Hex("f5f5f5"));
            },
            c =>
            {
                c.Rect(4, 6, 11, 14, Light);
                c.Ellipse(3, 12, 2, 2, Mid);
                c.Ellipse(12, 12, 2, 2, Mid);
                c.Rect(4, 7, 11, 7, Dark);
            },
            c =>
            {
                c.Rect(5, 6, 10, 14, Light);
                c.Ellipse(8, 12, 2, 2, Mid);
                c.Rect(5, 7, 10, 7, Dark);
            });

        // ------------------------------------------------------------------ hands & feet

        public static DirectionalSprites GloveSprites() => Set3("Gloves",
            c =>
            {
                c.Rect(2, 7, 3, 9, Light);
                c.Rect(12, 7, 13, 9, Light);
            },
            c =>
            {
                c.Rect(2, 7, 3, 9, Light);
                c.Rect(12, 7, 13, 9, Light);
            },
            c => c.Rect(8, 7, 10, 9, Light));

        public static DirectionalSprites BootSprites() => Set3("Boots",
            c =>
            {
                c.Rect(4, 0, 6, 2, Light);
                c.Rect(9, 0, 11, 2, Light);
                c.Rect(4, 2, 6, 2, Mid);
                c.Rect(9, 2, 11, 2, Mid);
            },
            c =>
            {
                c.Rect(4, 0, 6, 2, Light);
                c.Rect(9, 0, 11, 2, Light);
            },
            c =>
            {
                c.Rect(6, 0, 7, 2, Mid);
                c.Rect(8, 0, 11, 2, Light);
            });

        // ------------------------------------------------------------------ weapons (drawn in color, tint = white)

        private static readonly Color32 Steel = Hex("dfe6e9");
        private static readonly Color32 SteelDark = Hex("9aa5b1");
        private static readonly Color32 Wood = Hex("8b5a2b");
        private static readonly Color32 Gold = Hex("e0b04a");

        public static DirectionalSprites SwordSprites() => Set3("Sword",
            c =>
            {
                c.Rect(2, 9, 2, 17, Steel);
                c.Set(2, 18, Steel);
                c.Rect(1, 8, 3, 8, Gold);
                c.Rect(2, 6, 2, 7, Wood);
            },
            c =>
            {
                c.Rect(13, 9, 13, 17, SteelDark);
                c.Rect(12, 8, 14, 8, Gold);
            },
            c =>
            {
                c.Rect(11, 8, 14, 8, Steel);
                c.Set(15, 8, Steel);
                c.Rect(10, 7, 10, 9, Gold);
            });

        public static DirectionalSprites BowSprites() => Set3("Bow",
            c =>
            {
                for (var y = 5; y <= 17; y++)
                {
                    var bend = Mathf.RoundToInt(Mathf.Sin((y - 5) / 12f * Mathf.PI) * 2f);
                    c.Set(2 - bend + 2, y, Wood);
                }
                c.Rect(4, 5, 4, 17, Hex("d8d0c0"));
            },
            c =>
            {
                for (var y = 5; y <= 17; y++)
                {
                    var bend = Mathf.RoundToInt(Mathf.Sin((y - 5) / 12f * Mathf.PI) * 2f);
                    c.Set(13 + bend - 1, y, Wood);
                }
            },
            c =>
            {
                for (var y = 3; y <= 15; y++)
                {
                    var bend = Mathf.RoundToInt(Mathf.Sin((y - 3) / 12f * Mathf.PI) * 2f);
                    c.Set(11 + bend, y, Wood);
                }
                c.Rect(11, 3, 11, 15, Hex("d8d0c0"));
            });

        public static DirectionalSprites GreatswordSprites() => Set3("Greatsword",
            c =>
            {
                c.Rect(0, 8, 2, 21, Steel);
                c.Rect(1, 9, 1, 20, Hex("ffffff"));
                c.Rect(0, 22, 2, 22, Steel);
                c.Rect(0, 7, 3, 7, Gold);
                c.Rect(1, 5, 1, 6, Wood);
            },
            c =>
            {
                c.Line(3, 5, 12, 21, SteelDark, 2);
                c.Rect(4, 8, 6, 8, Gold);
            },
            c =>
            {
                c.Line(10, 8, 15, 22, Steel, 2);
                c.Rect(9, 7, 11, 7, Gold);
            });

        // ------------------------------------------------------------------ item icons

        private static Sprite Icon(string name, System.Action<PixelCanvas> paint) =>
            GetOrCreateSprite("Icon_" + name, 12, 12, c =>
            {
                paint(c);
                c.Shade(0.8f);
                c.AddOutline(Outline);
            }, new Vector2(0.5f, 0.5f));

        public static Sprite GoldIcon() => Icon("Gold", c =>
        {
            c.Ellipse(4, 3, 3, 2, Hex("e0a82e"));
            c.Ellipse(7, 4, 3, 2, Hex("f5c542"));
            c.Ellipse(6, 7, 3, 2, Hex("ffd966"));
            c.Set(6, 7, Hex("fff3b0"));
        });

        public static Sprite PotionIcon() => Icon("Potion", c =>
        {
            c.Ellipse(6, 4, 4, 4, Hex("e63946"));
            c.Rect(5, 8, 7, 10, Hex("c0c0c0"));
            c.Rect(5, 11, 7, 11, Hex("8b5a2b"));
            c.Set(4, 5, Hex("ffb3b8"));
        });

        public static Sprite GelIcon() => Icon("Gel", c =>
        {
            c.Ellipse(6, 4, 4, 3, Hex("6ab04c"));
            c.Set(4, 5, Hex("badc58"));
        });

        public static Sprite WingIcon() => Icon("Wing", c =>
        {
            for (var x = 1; x <= 10; x++) c.Rect(x, 3 + x / 3, x, 9 - (x % 3 == 0 ? 1 : 0), Hex("5b4387"));
        });

        public static Sprite BoneIcon() => Icon("Bone", c =>
        {
            c.Line(2, 2, 9, 9, Hex("e8e2d0"), 2);
            c.Ellipse(2, 2, 1, 1, Hex("e8e2d0"));
            c.Ellipse(9, 9, 1, 1, Hex("e8e2d0"));
        });

        public static Sprite EmberIcon() => Icon("Ember", c =>
        {
            c.Line(6, 1, 6, 10, Hex("ff8c42"), 1);
            c.Ellipse(6, 5, 3, 4, Hex("ff6b35"));
            c.Ellipse(6, 5, 1, 2, Hex("ffd166"));
        });
    }
}

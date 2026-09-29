using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Vela.Visual;

namespace Vela.EditorTools
{
    /// Placeholder 64×64 mannequin for the sword animation test (docs/specs/sword-animation-paperdoll.md).
    /// Every frame comes from a pose (arm angle, reach, lean, crouch, stride). The same pose
    /// draws the body and each garment layer and gives the frame's anchors (head, hand, blade
    /// tip), so art and anchors always agree. Output: one atlas PNG per layer under
    /// Assets/Art/Placeholder/Doll64, with the same cell per frame on every layer.
    /// Real art replaces this through the Aseprite pipeline; nothing reads the pose data at runtime.
    public static partial class PlaceholderArt
    {
        public const int DollCanvas = 64;
        public const string DollFolder = Folder + "/Doll64";
        private const float DollPivot = 32f;
        private const int AtlasSize = 1024;
        private const float ArmLength = 11f;

        public enum DollArtLayer
        {
            Body,
            Hair,
            Vest,
            Plate,
            Gloves,
            Boots,
            Greaves,
            Smear
        }

        /// One drawn pose. Angles in degrees, counter-clockwise from "forward" in side view.
        public struct PoseKey
        {
            public AttackPart Part;
            public int Weight;
            public float Arm;     // sword arm direction from the shoulder
            public float Reach;   // 0..1.1 of arm length
            public float Wrist;   // blade angle relative to the arm
            public float Lean;    // upper body forward (px, side view)
            public float Crouch;  // hips down (px)
            public float Stride;  // feet apart (px); negative = other foot forward
            public bool Thrust;   // blade points at the target (toward / away from camera in down / up views)
        }

        public sealed class MannequinClip
        {
            public string Name;
            public bool Attack;
            public bool Loop;
            public float Fps;
            public PoseKey[] Keys;
        }

        public sealed class MannequinCell
        {
            public string Clip;
            public Direction4 Direction;
            public int Frame;
            public int Cell;
            public DollFrame Data;
        }

        public sealed class MannequinSheets
        {
            public readonly List<MannequinCell> Cells = new List<MannequinCell>();
            public readonly Dictionary<DollArtLayer, Texture2D> Atlases = new Dictionary<DollArtLayer, Texture2D>();
            public readonly Dictionary<DollArtLayer, HashSet<int>> Empty = new Dictionary<DollArtLayer, HashSet<int>>();
            public int Columns => AtlasSize / DollCanvas;

            public Rect CellRect(int cell) =>
                new Rect(cell % Columns * DollCanvas, cell / Columns * DollCanvas, DollCanvas, DollCanvas);
        }

        private static PoseKey K(AttackPart part, int weight, float arm, float reach, float wrist, float lean,
            float crouch, float stride, bool thrust = false) => new PoseKey
        {
            Part = part, Weight = weight, Arm = arm, Reach = reach, Wrist = wrist, Lean = lean, Crouch = crouch,
            Stride = stride, Thrust = thrust
        };

        private const AttackPart W = AttackPart.Windup;
        private const AttackPart S = AttackPart.Smear;
        private const AttackPart I = AttackPart.Impact;
        private const AttackPart F = AttackPart.Follow;
        private const AttackPart R = AttackPart.Recovery;

        /// Clips and poses. Frame counts follow the spec's frame-data table.
        public static readonly MannequinClip[] MannequinClips =
        {
            new MannequinClip
            {
                Name = "idle", Loop = true, Fps = 4f, Keys = new[]
                {
                    K(W, 1, -70f, 0.9f, 10f, 0f, 0f, 3f), K(W, 1, -70f, 0.9f, 10f, 0f, 0f, 3f),
                    K(W, 1, -72f, 0.9f, 10f, 0f, 1f, 3f), K(W, 1, -72f, 0.9f, 10f, 0f, 1f, 3f)
                }
            },
            new MannequinClip
            {
                Name = "walk", Loop = true, Fps = 10f, Keys = new[]
                {
                    K(W, 1, -65f, 0.9f, 10f, 1f, 0f, 5f), K(W, 1, -70f, 0.9f, 10f, 1f, 1f, 0f),
                    K(W, 1, -75f, 0.9f, 10f, 1f, 0f, -5f), K(W, 1, -70f, 0.9f, 10f, 1f, 1f, 0f)
                }
            },
            new MannequinClip
            {
                Name = "hurt", Loop = false, Fps = 10f, Keys = new[]
                {
                    K(W, 1, -120f, 0.85f, 20f, -3f, 2f, 3f), K(W, 1, -100f, 0.9f, 10f, -1f, 1f, 3f)
                }
            },
            // Slash: windup 2 (held) · smear 1 · impact 1 · follow 2 · recovery 2
            new MannequinClip
            {
                Name = "slash", Attack = true, Keys = new[]
                {
                    K(W, 1, 100f, 0.85f, 25f, -1f, 1f, 3f), K(W, 2, 135f, 0.85f, 35f, -2f, 2f, 3f),
                    K(S, 1, 45f, 1f, 0f, 1f, 2f, 5f),
                    K(I, 1, -15f, 1f, -10f, 3f, 3f, 6f),
                    K(F, 1, -50f, 0.95f, -15f, 3f, 3f, 6f), K(F, 1, -65f, 0.9f, -10f, 2f, 2f, 5f),
                    K(R, 1, -70f, 0.9f, 0f, 1f, 1f, 4f), K(R, 1, -70f, 0.9f, 10f, 0f, 0f, 3f)
                }
            },
            // Backslash: the same shape, rising from low to high.
            new MannequinClip
            {
                Name = "backslash", Attack = true, Keys = new[]
                {
                    K(W, 1, -80f, 0.85f, -20f, -1f, 1f, 3f), K(W, 2, -100f, 0.85f, -30f, -2f, 2f, 3f),
                    K(S, 1, -20f, 1f, 0f, 1f, 2f, 5f),
                    K(I, 1, 40f, 1f, 10f, 3f, 3f, 6f),
                    K(F, 1, 75f, 0.95f, 15f, 3f, 3f, 6f), K(F, 1, 90f, 0.9f, 10f, 2f, 2f, 5f),
                    K(R, 1, -20f, 0.9f, 0f, 1f, 1f, 4f), K(R, 1, -70f, 0.9f, 10f, 0f, 0f, 3f)
                }
            },
            // Thrust finisher: windup 3 (last held ×3) · smear 1 · impact 1 · follow 3 · recovery 3
            new MannequinClip
            {
                Name = "thrust", Attack = true, Keys = new[]
                {
                    K(W, 1, 10f, 0.55f, 0f, -1f, 1f, 3f, true), K(W, 1, 15f, 0.45f, 0f, -2f, 2f, 3f, true),
                    K(W, 3, 15f, 0.4f, 0f, -3f, 3f, 4f, true),
                    K(S, 1, 5f, 0.85f, 0f, 2f, 2f, 6f, true),
                    K(I, 1, 0f, 1.1f, 0f, 4f, 3f, 8f, true),
                    K(F, 1, 0f, 1.05f, 0f, 4f, 3f, 8f, true), K(F, 1, 0f, 0.95f, 0f, 3f, 2f, 7f, true),
                    K(F, 1, -10f, 0.85f, 0f, 2f, 2f, 6f, true),
                    K(R, 1, -30f, 0.8f, 0f, 1f, 1f, 5f), K(R, 1, -55f, 0.85f, 0f, 1f, 1f, 4f),
                    K(R, 1, -70f, 0.9f, 10f, 0f, 0f, 3f)
                }
            },
            // Charged dash strike: no windup (the charge is the anticipation) · smear 2 · impact 1 · follow 3 · recovery 3
            new MannequinClip
            {
                Name = "dash_strike", Attack = true, Keys = new[]
                {
                    K(S, 1, 120f, 0.9f, 20f, 4f, 2f, 6f), K(S, 1, 50f, 1f, 0f, 5f, 3f, 7f),
                    K(I, 1, -15f, 1.05f, -10f, 6f, 4f, 9f),
                    K(F, 1, -45f, 1f, -15f, 5f, 4f, 9f), K(F, 1, -60f, 0.95f, -10f, 4f, 3f, 8f),
                    K(F, 1, -70f, 0.9f, -10f, 3f, 2f, 6f),
                    K(R, 1, -70f, 0.9f, 0f, 2f, 2f, 5f), K(R, 1, -70f, 0.9f, 5f, 1f, 1f, 4f),
                    K(R, 1, -70f, 0.9f, 10f, 0f, 0f, 3f)
                }
            }
        };

        public static readonly Direction4[] DollDirections = { Direction4.Down, Direction4.Up, Direction4.Side };

        // ------------------------------------------------------------------ skeleton

        private struct Skeleton
        {
            public Vector2 Hip, Neck, Head, Shoulder, OffShoulder, Hand, OffHand;
            public Vector2 HipA, HipB, KneeA, KneeB, FootA, FootB;
            public float Blade;
            public float Reach;
            public bool Thrust;
        }

        private static Vector2 Dir(float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        private static Vector2 PixelCenter(Vector2 v) => new Vector2(Mathf.Floor(v.x) + 0.5f, Mathf.Floor(v.y) + 0.5f);

        private static Skeleton Solve(PoseKey k, Direction4 dir)
        {
            var side = dir == Direction4.Side;
            var s = new Skeleton { Reach = k.Reach, Thrust = k.Thrust };
            var hipY = 20f - k.Crouch;
            var neckY = 33f - k.Crouch;
            var lean = side ? k.Lean : 0f;

            s.Hip = new Vector2(lean * 0.3f, hipY);
            s.Neck = new Vector2(lean, neckY);
            s.Head = new Vector2(lean + (side ? 1f : 0f), neckY + 6f);

            if (side)
            {
                s.HipA = s.HipB = s.Hip;
                s.FootA = new Vector2(k.Stride + lean * 0.2f, 1.5f);
                s.FootB = new Vector2(-k.Stride * 0.8f, 1.5f);
                s.KneeA = Vector2.Lerp(s.Hip, s.FootA, 0.5f) + new Vector2(1.5f, 0.5f);
                s.KneeB = Vector2.Lerp(s.Hip, s.FootB, 0.5f) + new Vector2(1f, 0.5f);
            }
            else
            {
                var lift = k.Stride * 0.35f;
                s.HipA = new Vector2(-2.5f, hipY);
                s.HipB = new Vector2(2.5f, hipY);
                s.FootA = new Vector2(-3.5f, 1.5f + Mathf.Max(0f, lift));
                s.FootB = new Vector2(3.5f, 1.5f + Mathf.Max(0f, -lift));
                s.KneeA = new Vector2(-3.5f, (hipY + s.FootA.y) * 0.5f);
                s.KneeB = new Vector2(3.5f, (hipY + s.FootB.y) * 0.5f);
            }

            // Sword in the right hand: screen-left when facing the camera, screen-right from behind.
            var shoulderX = side ? 0f : dir == Direction4.Down ? -6f : 6f;
            s.Shoulder = new Vector2(s.Neck.x + shoulderX, neckY - 2f);
            s.OffShoulder = new Vector2(s.Neck.x + (side ? -1f : -shoulderX), neckY - 2f);

            float arm;
            if (side)
            {
                arm = k.Arm;
                s.Blade = k.Arm + k.Wrist;
            }
            else if (k.Thrust)
            {
                // Toward the camera = down the screen; away = up.
                arm = dir == Direction4.Down ? -90f : 90f;
                s.Blade = arm;
            }
            else
            {
                // Mirror the side swing so the slash crosses the body diagonally.
                arm = 180f - k.Arm;
                s.Blade = 180f - (k.Arm + k.Wrist);
            }

            s.Hand = s.Shoulder + Dir(arm) * ArmLength * k.Reach;
            var offArm = side ? -100f : dir == Direction4.Down ? -80f : -100f;
            s.OffHand = s.OffShoulder + Dir(offArm) * 9f;
            return s;
        }

        private static DollFrame FrameData(PoseKey key, Skeleton s, Direction4 dir)
        {
            var hand = PixelCenter(s.Hand);
            return new DollFrame
            {
                part = key.Part,
                weight = Mathf.Max(1, key.Weight),
                head = PixelCenter(s.Head),
                hand = hand,
                tip = hand + Dir(s.Blade) * 8f,
                weaponInFront = dir != Direction4.Up
            };
        }

        // ------------------------------------------------------------------ painting

        private static readonly Color32 DollSkin = Hex("f4c7a1");
        private static readonly Color32 DollShirt = Hex("8e95a3");
        private static readonly Color32 DollShirtBack = Hex("737a88");
        private static readonly Color32 DollPants = Hex("3f4a5a");
        private static readonly Color32 DollPantsBack = Hex("323b49");
        private static readonly Color32 DollShoe = Hex("4a3428");
        private static readonly Color32 DollEye = Hex("1a1423");
        private static readonly Color32 SmearWhite = new Color32(255, 255, 255, 255);

        private static void Capsule(PixelCanvas c, Vector2 a, Vector2 b, float r, Color32 color, float pivotX = DollPivot)
        {
            var minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r + pivotX) - 1;
            var maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r + pivotX) + 1;
            var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r) - 1;
            var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r) + 1;
            var ab = b - a;
            var len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + 0.5f - pivotX, y + 0.5f);
                var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                if ((a + ab * t - p).sqrMagnitude <= r * r) c.Set(x, y, color);
            }
        }

        private static void Oval(PixelCanvas c, Vector2 center, float rx, float ry, Color32 color, float pivotX = DollPivot)
        {
            for (var y = Mathf.FloorToInt(center.y - ry) - 1; y <= Mathf.CeilToInt(center.y + ry) + 1; y++)
            for (var x = Mathf.FloorToInt(center.x - rx + pivotX) - 1; x <= Mathf.CeilToInt(center.x + rx + pivotX) + 1; x++)
            {
                var dx = (x + 0.5f - pivotX - center.x) / rx;
                var dy = (y + 0.5f - center.y) / ry;
                if (dx * dx + dy * dy <= 1f) c.Set(x, y, color);
            }
        }

        private static void PaintBody(PixelCanvas c, Skeleton s, Direction4 dir)
        {
            var side = dir == Direction4.Side;
            if (side)
            {
                // Far limbs first, a shade darker.
                Capsule(c, s.OffShoulder, s.OffHand, 1.6f, DollShirtBack);
                Oval(c, s.OffHand, 1.6f, 1.6f, DollSkin);
                Capsule(c, s.HipB, s.KneeB, 2f, DollPantsBack);
                Capsule(c, s.KneeB, s.FootB, 1.8f, DollPantsBack);
            }
            else
            {
                Capsule(c, s.HipB, s.KneeB, 2f, DollPants);
                Capsule(c, s.KneeB, s.FootB, 1.8f, DollPants);
            }

            Capsule(c, s.HipA, s.KneeA, 2f, DollPants);
            Capsule(c, s.KneeA, s.FootA, 1.8f, DollPants);
            Capsule(c, s.FootA + new Vector2(side ? 0.5f : 0f, -0.5f), s.FootA + new Vector2(side ? 1.5f : 0f, -0.5f), 1.2f, DollShoe);
            Capsule(c, s.FootB + new Vector2(side ? 0.5f : 0f, -0.5f), s.FootB + new Vector2(side ? 1.5f : 0f, -0.5f), 1.2f, DollShoe);

            Capsule(c, s.Neck + new Vector2(0f, -1f), s.Hip + new Vector2(0f, 1f), 4.3f, DollShirt);

            if (!side)
            {
                Capsule(c, s.OffShoulder, s.OffHand, 1.6f, DollShirt);
                Oval(c, s.OffHand, 1.6f, 1.6f, DollSkin);
            }

            Oval(c, s.Head, 5f, 5f, DollSkin);
            if (dir == Direction4.Down)
            {
                c.Set(Mathf.FloorToInt(s.Head.x - 2f + DollPivot), Mathf.FloorToInt(s.Head.y), DollEye);
                c.Set(Mathf.FloorToInt(s.Head.x + 2f + DollPivot), Mathf.FloorToInt(s.Head.y), DollEye);
            }
            else if (side)
            {
                c.Set(Mathf.FloorToInt(s.Head.x + 2.5f + DollPivot), Mathf.FloorToInt(s.Head.y), DollEye);
            }

            Capsule(c, s.Shoulder, s.Hand, 1.6f, DollShirt);
            Oval(c, s.Hand, 1.6f, 1.6f, DollSkin);
            c.Shade(0.82f);
            c.AddOutline(Outline);
        }

        private static void PaintHair(PixelCanvas c, Skeleton s, Direction4 dir)
        {
            switch (dir)
            {
                case Direction4.Up:
                    Oval(c, s.Head, 5.5f, 5.5f, Light);
                    break;
                case Direction4.Side:
                    Oval(c, s.Head + new Vector2(-1.5f, 1.5f), 4.6f, 4.3f, Light);
                    Capsule(c, s.Head + new Vector2(-4f, 1f), s.Head + new Vector2(-4f, -3f), 1.4f, Light);
                    break;
                default:
                    Oval(c, s.Head + new Vector2(0f, 2.5f), 5.4f, 3.4f, Light);
                    Capsule(c, s.Head + new Vector2(-4.6f, 1f), s.Head + new Vector2(-4.6f, -2f), 1f, Light);
                    Capsule(c, s.Head + new Vector2(4.6f, 1f), s.Head + new Vector2(4.6f, -2f), 1f, Light);
                    break;
            }
            c.Shade(0.8f);
            c.AddOutline(Outline);
        }

        private static void PaintGarment(PixelCanvas c, DollArtLayer layer, Skeleton s, Direction4 dir)
        {
            var side = dir == Direction4.Side;
            switch (layer)
            {
                case DollArtLayer.Vest:
                    Capsule(c, s.Neck + new Vector2(0f, -1.5f), s.Hip + new Vector2(0f, 2f), 4.8f, Light);
                    Capsule(c, s.Hip + new Vector2(-4f, 2.5f), s.Hip + new Vector2(4f, 2.5f), 0.6f, Dark);
                    if (dir == Direction4.Down) Capsule(c, s.Neck + new Vector2(0f, -1f), s.Neck + new Vector2(0f, -4f), 0.8f, Mid);
                    break;
                case DollArtLayer.Plate:
                    Capsule(c, s.Neck + new Vector2(0f, -1f), s.Hip + new Vector2(0f, 1.5f), 5.2f, Light);
                    Capsule(c, s.Hip + new Vector2(-4.5f, 5f), s.Hip + new Vector2(4.5f, 5f), 0.6f, Mid);
                    Oval(c, s.Shoulder + new Vector2(0f, 0.5f), 2.6f, 2f, Mid);
                    if (!side) Oval(c, s.OffShoulder + new Vector2(0f, 0.5f), 2.6f, 2f, Mid);
                    break;
                case DollArtLayer.Gloves:
                    Oval(c, s.Hand, 2f, 2f, Light);
                    Oval(c, s.OffHand, 2f, 2f, Light);
                    break;
                case DollArtLayer.Boots:
                case DollArtLayer.Greaves:
                {
                    var height = layer == DollArtLayer.Greaves ? 0.75f : 0.35f;
                    foreach (var (foot, knee) in new[] { (s.FootB, s.KneeB), (s.FootA, s.KneeA) })
                    {
                        Capsule(c, foot, Vector2.Lerp(foot, knee, height), 2.1f, Light);
                        Capsule(c, foot + new Vector2(side ? 0.5f : 0f, -0.4f), foot + new Vector2(side ? 2f : 0f, -0.4f), 1.4f, Mid);
                    }
                    break;
                }
            }
            c.Shade(0.8f);
            c.AddOutline(Outline);
        }

        /// Motion smear: an arc swept from the previous frame's blade angle to this one (or speed
        /// lines along a thrust). Brightest at the leading edge.
        private static void PaintSmear(PixelCanvas c, Skeleton s, float previousBlade)
        {
            if (s.Thrust)
            {
                var d = Dir(s.Blade);
                var n = new Vector2(-d.y, d.x);
                for (var i = -2; i <= 2; i++)
                {
                    var start = s.Hand - d * (4f + Mathf.Abs(i) * 2f) + n * i * 1.5f;
                    var end = s.Hand + d * (14f - Mathf.Abs(i) * 3f) + n * i * 1.5f;
                    Capsule(c, start, end, 0.5f, new Color32(255, 255, 255, (byte)(i == 0 ? 255 : 150)));
                }
                return;
            }

            var from = previousBlade;
            var delta = Mathf.DeltaAngle(from, s.Blade);
            var inner = ArmLength * s.Reach * 0.6f;
            var outer = ArmLength * s.Reach + 16f;
            for (var y = 0; y < c.Height; y++)
            for (var x = 0; x < c.Width; x++)
            {
                var p = new Vector2(x + 0.5f - DollPivot, y + 0.5f) - s.Shoulder;
                var r = p.magnitude;
                if (r < inner || r > outer) continue;
                var angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                var along = Mathf.DeltaAngle(from, angle) / (Mathf.Abs(delta) < 1f ? 1f : delta);
                if (along < 0f || along > 1f) continue;
                var edge = Mathf.InverseLerp(inner, outer, r);
                var alpha = Mathf.Clamp01(along * 1.2f) * Mathf.Lerp(0.35f, 1f, edge);
                if (alpha < 0.15f) continue;
                c.Set(x, y, new Color32(255, 255, 255, (byte)(alpha * 255f)));
            }
        }

        // ------------------------------------------------------------------ atlases

        /// Draws every clip × facing × frame for every layer and imports the atlases.
        public static MannequinSheets MannequinAtlases()
        {
            var sheets = new MannequinSheets();
            var layers = (DollArtLayer[])System.Enum.GetValues(typeof(DollArtLayer));
            var atlases = new Dictionary<DollArtLayer, PixelCanvas>();
            foreach (var layer in layers)
            {
                atlases[layer] = new PixelCanvas(AtlasSize, AtlasSize);
                sheets.Empty[layer] = new HashSet<int>();
            }

            foreach (var clip in MannequinClips)
            foreach (var dir in DollDirections)
            {
                float? previousBlade = null;
                for (var i = 0; i < clip.Keys.Length; i++)
                {
                    var key = clip.Keys[i];
                    var skeleton = Solve(key, dir);
                    var cell = sheets.Cells.Count;
                    sheets.Cells.Add(new MannequinCell
                    {
                        Clip = clip.Name, Direction = dir, Frame = i, Cell = cell, Data = FrameData(key, skeleton, dir)
                    });

                    foreach (var layer in layers)
                    {
                        var canvas = new PixelCanvas(DollCanvas, DollCanvas);
                        switch (layer)
                        {
                            case DollArtLayer.Body: PaintBody(canvas, skeleton, dir); break;
                            case DollArtLayer.Hair: PaintHair(canvas, skeleton, dir); break;
                            case DollArtLayer.Smear:
                                if (key.Part == AttackPart.Smear)
                                    PaintSmear(canvas, skeleton, previousBlade ?? skeleton.Blade + (dir == Direction4.Side ? 70f : -70f));
                                break;
                            default: PaintGarment(canvas, layer, skeleton, dir); break;
                        }
                        if (!Blit(canvas, atlases[layer], sheets.CellRect(cell))) sheets.Empty[layer].Add(cell);
                    }
                    previousBlade = skeleton.Blade;
                }
            }

            EnsureFolder(DollFolder);
            foreach (var layer in layers)
            {
                var path = $"{DollFolder}/{layer}.png";
                var texture = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
                texture.SetPixels32(atlases[layer].Pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                sheets.Atlases[layer] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return sheets;
        }

        /// Copies a cell into the atlas; false if the cell is fully transparent.
        private static bool Blit(PixelCanvas cell, PixelCanvas atlas, Rect rect)
        {
            var any = false;
            var ox = (int)rect.x;
            var oy = (int)rect.y;
            for (var y = 0; y < cell.Height; y++)
            for (var x = 0; x < cell.Width; x++)
            {
                var p = cell.Get(x, y);
                if (p.a == 0) continue;
                atlas.Set(ox + x, oy + y, p);
                any = true;
            }
            return any;
        }

        // ------------------------------------------------------------------ anchored gear

        /// Hats sit with their bottom center on the head anchor (+ the visual's anchorOffset).
        private static DirectionalSprites Hat(string name, System.Action<PixelCanvas, Direction4> paint)
        {
            EnsureFolder(DollFolder);
            Sprite Make(Direction4 dir) => GetOrCreateSprite($"Doll64/Hat_{name}_{dir}", 16, 16, c =>
            {
                paint(c, dir);
                c.Shade(0.8f);
                c.AddOutline(Outline);
            });
            return new DirectionalSprites { down = Make(Direction4.Down), up = Make(Direction4.Up), side = Make(Direction4.Side) };
        }

        private const float HatPivot = 8f;

        /// Offset (0, -6): covers the head down to the chin, with a face opening.
        public static DirectionalSprites HoodAnchored() => Hat("Hood", (c, dir) =>
        {
            Oval(c, new Vector2(0f, 7f), 6.4f, 6.6f, Mid, HatPivot);
            if (dir == Direction4.Down) Oval(c, new Vector2(0f, 5f), 3.6f, 3.4f, new Color32(0, 0, 0, 0), HatPivot);
            if (dir == Direction4.Side)
            {
                Oval(c, new Vector2(3f, 5f), 3f, 3.4f, new Color32(0, 0, 0, 0), HatPivot);
                Capsule(c, new Vector2(-5f, 6f), new Vector2(-6.5f, 1f), 1.4f, Mid, HatPivot);
            }
        });

        /// Offset (0, -2): a dome over the top of the head with a nose guard.
        public static DirectionalSprites HelmAnchored() => Hat("Helm", (c, dir) =>
        {
            Oval(c, new Vector2(dir == Direction4.Side ? -0.5f : 0f, 2.5f), 6.2f, 6.5f, Light, HatPivot);
            for (var x = 0; x < 16; x++) { c.Clear(x, 0); c.Clear(x, 1); }
            Capsule(c, new Vector2(-6f, 2.5f), new Vector2(6f, 2.5f), 0.6f, Dark, HatPivot);
            if (dir == Direction4.Down) Capsule(c, new Vector2(0f, 0.5f), new Vector2(0f, 3f), 0.6f, Mid, HatPivot);
            if (dir == Direction4.Side) Capsule(c, new Vector2(4.5f, 0.5f), new Vector2(4.5f, 3f), 0.6f, Mid, HatPivot);
        });

        /// Offset (0, 3): a small band with three points on top of the head.
        public static DirectionalSprites CrownAnchored() => Hat("Crown", (c, dir) =>
        {
            var width = dir == Direction4.Side ? 3.5f : 4.5f;
            Capsule(c, new Vector2(-width, 1f), new Vector2(width, 1f), 1f, Light, HatPivot);
            foreach (var x in new[] { -width, 0f, width })
                Capsule(c, new Vector2(x, 1f), new Vector2(x, 4.5f), 0.6f, Light, HatPivot);
            if (dir != Direction4.Up) c.Set(8, 1, Hex("e63946"));
        });

        /// Weapon art points right; the pivot is on the grip (hand anchor).
        private static Sprite Grip(string name, int width, int height, int gripX, int gripY, System.Action<PixelCanvas> paint)
        {
            EnsureFolder(DollFolder);
            return GetOrCreateSprite($"Doll64/Weapon_{name}", width, height, c =>
            {
                paint(c);
                c.AddOutline(Outline);
            }, new Vector2((gripX + 0.5f) / width, (gripY + 0.5f) / height));
        }

        private static void Blade(PixelCanvas c, int guardX, int endX, int y0, int y1, int height)
        {
            c.Rect(0, y0 + (y1 - y0) / 2 - 1, 1, y0 + (y1 - y0) / 2 + 1, Gold);   // pommel
            c.Rect(2, y0 + (y1 - y0) / 2, guardX - 1, y0 + (y1 - y0) / 2, Wood);  // grip
            c.Rect(guardX, 0, guardX, height - 1, Gold);                          // guard
            c.Rect(guardX + 1, y0, endX, y1, Steel);                              // blade
            c.Rect(guardX + 1, y0, endX, y0, SteelDark);
            c.Rect(endX + 1, y0 + 1, endX + 1, y1 - 1, Steel);                    // tip
        }

        public static Sprite SwordShortGrip() => Grip("SwordShort", 18, 5, 3, 2, c => Blade(c, 5, 15, 1, 3, 5));
        public static Sprite SwordLongGrip() => Grip("SwordLong", 26, 5, 3, 2, c => Blade(c, 5, 23, 1, 3, 5));
        public static Sprite SwordBroadGrip() => Grip("SwordBroad", 22, 7, 3, 3, c => Blade(c, 5, 19, 1, 5, 7));
        public static Sprite GreatswordGrip() => Grip("Greatsword", 34, 7, 4, 3, c => Blade(c, 8, 31, 1, 5, 7));

        public static Sprite BowGrip() => Grip("Bow", 9, 23, 2, 11, c =>
        {
            for (var y = 0; y < 23; y++)
            {
                var bend = Mathf.RoundToInt(Mathf.Sin(y / 22f * Mathf.PI) * 5f);
                c.Rect(2 + bend, y, 3 + bend, y, Wood);
            }
            c.Rect(2, 0, 2, 22, Light);
        });
    }
}

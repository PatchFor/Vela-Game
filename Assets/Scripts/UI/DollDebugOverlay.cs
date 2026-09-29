using UnityEngine;
using Vela.Core;
using Vela.Player;
using Vela.Visual;

namespace Vela.UI
{
    /// F3: animation debug overlay for the player (IMGUI placeholder).
    ///  - Anchor dots: head (yellow), hand (cyan), blade tip (magenta).
    ///  - Phase bar: windup (yellow) / active (red) / recovery (blue), with a cursor.
    ///  - Clip, facing, frame number, attack part, and the A/B mode.
    ///  - Hitbox arc: dots on the ground where the current swing can hit.
    [RequireComponent(typeof(PaperDoll))]
    public class DollDebugOverlay : MonoBehaviour
    {
        private static readonly Color WindupColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        private static readonly Color ActiveColor = new Color(1f, 0.25f, 0.2f, 0.9f);
        private static readonly Color RecoveryColor = new Color(0.35f, 0.6f, 1f, 0.9f);

        private PaperDoll doll;
        private PlayerCombat combat;
        private bool visible;
        private GUIStyle label;

        private void Awake()
        {
            doll = GetComponent<PaperDoll>();
            combat = GetComponent<PlayerCombat>();
        }

        private void Update()
        {
            if (VelaInput.DebugAnimationOverlayPressed) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || doll == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            label ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
            var scale = Screen.height / 1080f;

            var clip = doll.CurrentClip;
            var frame = doll.CurrentFrameData;
            var info = clip == null
                ? $"Animation: {AnimationTestbenchDescribe()}  (no drawn frames)"
                : $"{clip.name} · {clip.direction} · frame {doll.CurrentFrameIndex + 1}/{clip.Count}" +
                  (clip.isAttack && frame != null ? $" · {frame.part}" : "") +
                  $"   [{AnimationTestbenchDescribe()}]";

            var box = new Rect(Screen.width * 0.5f - 320f * scale, Screen.height - 150f * scale, 640f * scale, 70f * scale);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            label.fontSize = Mathf.RoundToInt(15 * scale);
            GUI.Label(new Rect(box.x + 10f * scale, box.y + 6f * scale, box.width, 24f * scale), info, label);
            DrawPhaseBar(new Rect(box.x + 10f * scale, box.y + 38f * scale, box.width - 20f * scale, 18f * scale));

            if (frame != null)
            {
                Dot(cam, doll.AnchorWorld(frame.head), WindupColor, scale);
                Dot(cam, doll.AnchorWorld(frame.hand), new Color(0.3f, 1f, 1f), scale);
                Dot(cam, doll.AnchorWorld(frame.tip), new Color(1f, 0.3f, 1f), scale);
            }

            DrawHitArc(cam, scale);
        }

        private string AnimationTestbenchDescribe() => Gameplay.AnimationTestbench.Describe(doll.Detail);

        private void DrawPhaseBar(Rect bar)
        {
            var timing = doll.AttackTiming;
            var total = timing.x + timing.y + timing.z;
            if (!doll.IsAttacking || total <= 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.15f);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }

            var x = bar.x;
            Segment(ref x, bar, timing.x / total, WindupColor);
            Segment(ref x, bar, timing.y / total, ActiveColor);
            Segment(ref x, bar, timing.z / total, RecoveryColor);

            var cursor = bar.x + bar.width * Mathf.Clamp01(doll.AttackElapsed / total);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cursor - 1f, bar.y - 4f, 3f, bar.height + 8f), Texture2D.whiteTexture);

            // Frame count at 60 fps for animators (CLAUDE.md rule 7).
            GUI.Label(new Rect(bar.x, bar.y - 2f, bar.width, bar.height + 4f),
                $"  {Mathf.RoundToInt(timing.x * 60f)}f windup · {Mathf.RoundToInt(timing.y * 60f)}f active · {Mathf.RoundToInt(timing.z * 60f)}f recovery",
                label);
        }

        private static void Segment(ref float x, Rect bar, float fraction, Color color)
        {
            var w = bar.width * fraction;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, bar.y, w, bar.height), Texture2D.whiteTexture);
            x += w;
        }

        private static void Dot(Camera cam, Vector3? world, Color color, float scale)
        {
            if (!world.HasValue) return;
            var screen = cam.WorldToScreenPoint(world.Value);
            if (screen.z < 0f) return;
            var size = 7f * scale;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(screen.x - size * 0.5f - 1f, Screen.height - screen.y - size * 0.5f - 1f, size + 2f, size + 2f), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(screen.x - size * 0.5f, Screen.height - screen.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawHitArc(Camera cam, float scale)
        {
            var step = combat != null ? combat.CurrentStep : null;
            if (step == null || step.kind != Config.AttackKind.MeleeArc) return;

            var forward = combat.AttackDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;
            forward.Normalize();

            const int points = 13;
            for (var i = 0; i < points; i++)
            {
                var angle = Mathf.Lerp(-step.arcDegrees * 0.5f, step.arcDegrees * 0.5f, i / (points - 1f));
                var dir = Quaternion.Euler(0f, angle, 0f) * forward;
                Dot(cam, transform.position + dir * step.range + Vector3.up * 0.05f, new Color(1f, 0.4f, 0.3f, 0.8f), scale * 0.7f);
            }
        }
    }
}

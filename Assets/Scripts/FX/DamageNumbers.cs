using System.Collections.Generic;
using UnityEngine;
using Vela.Gameplay;

namespace Vela.FX
{
    /// Floating combat text. World position is projected to the screen every frame, so
    /// numbers stay crisp at any zoom. Style lives in CombatFeelConfig.
    ///  - Size and color follow the hit weight (light hits small, finishers big and orange).
    ///  - Crits are bigger, gold, shake for a moment and carry a small "CRITICAL" label.
    ///  - Rapid hits on the same target stack upward instead of piling on top of each other.
    public class DamageNumbers : MonoBehaviour
    {
        private struct Entry
        {
            public Vector3 World;
            public Vector3 Drift;
            public string Text;
            public string Label;
            public Color Color;
            public float Scale;
            public float Age;
            public float Lifetime;
            public bool Crit;
        }

        private struct StackState
        {
            public float LastTime;
            public int Count;
        }

        private static DamageNumbers instance;

        private readonly List<Entry> entries = new List<Entry>();
        private readonly Dictionary<int, StackState> stacks = new Dictionary<int, StackState>();
        private GUIStyle style;
        private GUIStyle labelStyle;

        private static DamageNumbers Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("[DamageNumbers]").AddComponent<DamageNumbers>();
                return instance;
            }
        }

        /// A damage number. `stackKey` groups numbers per target (use the victim's instance id).
        public static void SpawnDamage(Vector3 worldPosition, int amount, Color color, float scale, bool crit, int stackKey)
        {
            var feel = VelaSettings.Feel;
            if (!feel.showDamageNumbers) return;

            var position = worldPosition + Vector3.up * Instance.NextStackOffset(stackKey);
            Instance.Add(position, amount.ToString(), crit ? feel.critLabel : null, color, scale,
                crit ? feel.lifetime * 1.35f : feel.lifetime, crit);
        }

        /// Free text (weapon names, "BREAK!", heals...).
        public static void Spawn(Vector3 worldPosition, string text, Color color, float scale = 1f, int stackKey = 0)
        {
            var feel = VelaSettings.Feel;
            if (!feel.showDamageNumbers) return;

            var offset = stackKey != 0 ? Instance.NextStackOffset(stackKey) : 0f;
            Instance.Add(worldPosition + Vector3.up * offset, text, null, color, scale, feel.lifetime * 1.2f, false);
        }

        private float NextStackOffset(int key)
        {
            var feel = VelaSettings.Feel;
            var now = Time.unscaledTime;
            stacks.TryGetValue(key, out var state);
            state.Count = now - state.LastTime <= feel.stackWindow ? state.Count + 1 : 0;
            state.LastTime = now;
            stacks[key] = state;
            return Mathf.Min(state.Count, 5) * feel.stackOffset;
        }

        private void Add(Vector3 world, string text, string label, Color color, float scale, float lifetime, bool crit)
        {
            var feel = VelaSettings.Feel;
            var scatter = Random.insideUnitCircle * feel.horizontalScatter;
            entries.Add(new Entry
            {
                World = world,
                Drift = new Vector3(scatter.x, 0f, scatter.y * 0.3f),
                Text = text,
                Label = label,
                Color = color,
                Scale = scale,
                Lifetime = lifetime,
                Crit = crit
            });
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.Age += dt;
                if (e.Age >= e.Lifetime)
                {
                    entries.RemoveAt(i);
                    continue;
                }
                entries[i] = e;
            }
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null || entries.Count == 0) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    clipping = TextClipping.Overflow
                };
                style.normal.textColor = Color.white;
                labelStyle = new GUIStyle(style);
            }

            var feel = VelaSettings.Feel;
            var uiScale = Screen.height / 1080f;
            var previous = GUI.color;

            foreach (var e in entries)
            {
                var t = e.Age / e.Lifetime;

                // Crits hang in place for a beat before rising; normal numbers rise at once.
                var hang = e.Crit ? 0.18f : 0f;
                var riseT = Mathf.Clamp01((t - hang) / (1f - hang));
                var rise = feel.riseSpeed * e.Lifetime * (1f - (1f - riseT) * (1f - riseT));
                var world = e.World + Vector3.up * rise + e.Drift * riseT;
                var screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f) continue;

                // Pop: overshoot then settle. Bigger overshoot for crits.
                var peak = e.Crit ? 1.8f : 1.35f;
                var pop = e.Age < 0.07f ? Mathf.Lerp(0.5f, peak, e.Age / 0.07f)
                    : Mathf.Lerp(peak, 1f, Mathf.Clamp01((e.Age - 0.07f) / 0.14f));
                var size = feel.fontSize * e.Scale * pop * uiScale * 1.4f;
                style.fontSize = Mathf.Max(8, Mathf.RoundToInt(size));

                var shake = Vector2.zero;
                if (e.Crit && e.Age < 0.25f)
                {
                    var s = (1f - e.Age / 0.25f) * 4f * uiScale;
                    shake = new Vector2(Mathf.Sin(e.Age * 160f) * s, Mathf.Cos(e.Age * 130f) * s);
                }

                var alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                var rect = new Rect(screen.x - 150f + shake.x, Screen.height - screen.y - 40f + shake.y, 300f, 80f);
                DrawOutlined(rect, e.Text, style, e.Color, alpha, uiScale);

                if (!string.IsNullOrEmpty(e.Label))
                {
                    labelStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(feel.fontSize * 0.6f * uiScale * 1.4f));
                    var labelRect = new Rect(rect.x, rect.y - size * 0.75f, rect.width, rect.height);
                    DrawOutlined(labelRect, e.Label, labelStyle, e.Color, alpha, uiScale);
                }
            }

            GUI.color = previous;
        }

        private static void DrawOutlined(Rect rect, string text, GUIStyle guiStyle, Color color, float alpha, float uiScale)
        {
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.85f);
            var o = Mathf.Max(1f, 2f * uiScale);
            GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), text, guiStyle);
            GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), text, guiStyle);
            GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), text, guiStyle);
            GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), text, guiStyle);

            var c = color;
            c.a *= alpha;
            GUI.color = c;
            GUI.Label(rect, text, guiStyle);
        }
    }
}

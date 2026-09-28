using System.Collections.Generic;
using UnityEngine;
using Vela.Gameplay;

namespace Vela.FX
{
    /// Floating combat text. World position is projected to the screen every frame, so
    /// numbers stay crisp at any zoom. Style lives in CombatFeelConfig.
    public class DamageNumbers : MonoBehaviour
    {
        private struct Entry
        {
            public Vector3 World;
            public Vector3 Drift;
            public string Text;
            public Color Color;
            public int FontSize;
            public float Age;
            public float Lifetime;
            public bool Big;
        }

        private static DamageNumbers instance;

        private readonly List<Entry> entries = new List<Entry>();
        private GUIStyle style;

        public static void Spawn(Vector3 worldPosition, int amount, bool crit, bool playerTookIt)
        {
            var feel = VelaSettings.Feel;
            if (!feel.showDamageNumbers) return;

            var color = playerTookIt ? feel.takenColor : crit ? feel.critColor : feel.dealtColor;
            var text = crit ? amount + feel.critSuffix : amount.ToString();
            Spawn(worldPosition, text, color, crit ? feel.critFontSize : feel.fontSize, crit);
        }

        public static void Spawn(Vector3 worldPosition, string text, Color color, int fontSize, bool big = false)
        {
            if (instance == null) instance = new GameObject("[DamageNumbers]").AddComponent<DamageNumbers>();

            var feel = VelaSettings.Feel;
            var scatter = Random.insideUnitCircle * feel.horizontalScatter;
            instance.entries.Add(new Entry
            {
                World = worldPosition,
                Drift = new Vector3(scatter.x, 0f, scatter.y * 0.3f),
                Text = text,
                Color = color,
                FontSize = fontSize,
                Lifetime = big ? feel.lifetime * 1.3f : feel.lifetime,
                Big = big
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
            }

            var feel = VelaSettings.Feel;
            var uiScale = Screen.height / 1080f;
            var previous = GUI.color;

            foreach (var e in entries)
            {
                var t = e.Age / e.Lifetime;
                // Fast rise that eases out, plus sideways drift.
                var rise = feel.riseSpeed * e.Lifetime * (1f - (1f - t) * (1f - t));
                var world = e.World + Vector3.up * rise + e.Drift * t;
                var screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f) continue;

                // Pop: overshoot then settle.
                var pop = e.Age < 0.08f ? Mathf.Lerp(0.6f, e.Big ? 1.6f : 1.3f, e.Age / 0.08f)
                    : Mathf.Lerp(e.Big ? 1.6f : 1.3f, 1f, Mathf.Clamp01((e.Age - 0.08f) / 0.12f));
                style.fontSize = Mathf.Max(8, Mathf.RoundToInt(e.FontSize * pop * uiScale * 1.4f));

                var alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                var rect = new Rect(screen.x - 100f, Screen.height - screen.y - 30f, 200f, 60f);

                GUI.color = new Color(0f, 0f, 0f, alpha * 0.85f);
                var o = Mathf.Max(1f, 2f * uiScale);
                GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), e.Text, style);
                GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), e.Text, style);
                GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), e.Text, style);
                GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), e.Text, style);

                var c = e.Color;
                c.a *= alpha;
                GUI.color = c;
                GUI.Label(rect, e.Text, style);
            }

            GUI.color = previous;
        }
    }
}

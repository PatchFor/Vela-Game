using System.Collections.Generic;
using UnityEngine;

namespace Vela.Core
{
    /// A scrolling list of "what just happened" lines for the HUD.
    /// Static so any system can post to it without wiring references; the HUD
    /// clears it when a scene starts so nothing survives a restart.
    public static class CombatFeed
    {
        public struct Line
        {
            public string text;
            public Color color;
            public float bornAt;
        }

        private const int Capacity = 8;
        private static readonly List<Line> lines = new List<Line>(Capacity);

        public static IList<Line> Lines { get { return lines; } }

        public static void Clear()
        {
            lines.Clear();
        }

        public static void Post(string text, Color color)
        {
            Line line = new Line();
            line.text = text;
            line.color = color;
            line.bornAt = Time.unscaledTime;

            lines.Add(line);
            if (lines.Count > Capacity) lines.RemoveAt(0);
        }
    }

    /// Floating damage numbers. Positions are world space; the HUD projects them.
    public static class DamagePopups
    {
        public class Popup
        {
            public Vector3 position;
            public Vector3 drift;
            public string text;
            public Color color;
            public float size;
            public float life;
            public float age;
        }

        private static readonly List<Popup> active = new List<Popup>(32);

        public static IList<Popup> Active { get { return active; } }

        public static void Clear()
        {
            active.Clear();
        }

        public static void Spawn(Vector3 worldPosition, string text, Color color, float size, float life)
        {
            Popup popup = new Popup();
            popup.position = worldPosition;
            popup.drift = new Vector3(Random.Range(-0.6f, 0.6f), 2.2f, Random.Range(-0.6f, 0.6f));
            popup.text = text;
            popup.color = color;
            popup.size = size;
            popup.life = life;

            active.Add(popup);
            if (active.Count > 48) active.RemoveAt(0);
        }

        public static void Tick(float unscaledDelta)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Popup popup = active[i];
                popup.age += unscaledDelta;
                popup.position += popup.drift * unscaledDelta;
                popup.drift.y -= 3.2f * unscaledDelta;

                if (popup.age >= popup.life) active.RemoveAt(i);
            }
        }
    }

    /// Additive camera shake. The camera rig samples Offset() every LateUpdate.
    public static class ScreenShake
    {
        private static float amplitude;
        private static float decay;

        public static void Clear()
        {
            amplitude = 0f;
            decay = 0f;
        }

        public static void Add(float strength, float duration)
        {
            if (strength <= 0f) return;

            amplitude = Mathf.Max(amplitude, strength);
            decay = Mathf.Max(0.05f, duration);
        }

        public static Vector3 Sample(float unscaledDelta)
        {
            if (amplitude <= 0.0001f) return Vector3.zero;

            amplitude = Mathf.Max(0f, amplitude - (amplitude / decay) * unscaledDelta - 0.02f * unscaledDelta);

            float t = Time.unscaledTime * 38f;
            return new Vector3(
                (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * amplitude,
                (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * amplitude,
                0f);
        }
    }

    /// Freeze-frame on impact. The single cheapest "this hit landed" cue there is.
    public class Hitstop : MonoBehaviour
    {
        private static Hitstop instance;

        private float remaining;

        public static void Request(float seconds, float timeScale)
        {
            if (seconds <= 0f) return;

            if (instance == null)
            {
                GameObject host = new GameObject("~Hitstop");
                host.hideFlags = HideFlags.HideAndDontSave;
                instance = host.AddComponent<Hitstop>();
            }

            instance.Begin(seconds, Mathf.Clamp(timeScale, 0f, 1f));
        }

        public static void Request(float seconds)
        {
            Request(seconds, 0.05f);
        }

        private void Begin(float seconds, float timeScale)
        {
            remaining = Mathf.Max(remaining, seconds);
            Time.timeScale = timeScale;
        }

        private void Update()
        {
            if (remaining <= 0f) return;

            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                remaining = 0f;
                Time.timeScale = 1f;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (instance == this) instance = null;
        }
    }
}

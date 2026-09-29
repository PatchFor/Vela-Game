using System.Collections.Generic;
using UnityEngine;

namespace Vela.Gameplay
{
    /// Short toasts ("Inventory full", "+ Iron Helm") drawn by the HUD above the skill bar.
    /// Repeating the same text just refreshes it instead of stacking duplicates.
    public static class HudMessages
    {
        public struct Message
        {
            public string Text;
            public Color Color;
            public float Time;
            public float Duration;
        }

        private const int MaxMessages = 5;
        private static readonly List<Message> Items = new List<Message>();

        public static IReadOnlyList<Message> Active => Items;

        public static void Show(string text, Color color, float duration = 1.8f)
        {
            var now = UnityEngine.Time.unscaledTime;
            for (var i = 0; i < Items.Count; i++)
            {
                if (Items[i].Text != text) continue;
                var m = Items[i];
                m.Time = now;
                m.Color = color;
                m.Duration = duration;
                Items.RemoveAt(i);
                Items.Add(m);
                return;
            }

            Items.Add(new Message { Text = text, Color = color, Time = now, Duration = duration });
            while (Items.Count > MaxMessages) Items.RemoveAt(0);
        }

        public static void Prune()
        {
            var now = UnityEngine.Time.unscaledTime;
            Items.RemoveAll(m => now - m.Time > m.Duration);
        }

        public static void Clear() => Items.Clear();
    }
}

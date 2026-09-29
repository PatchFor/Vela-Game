using System.Collections.Generic;
using UnityEngine;
using Vela.Gameplay;

namespace Vela.Audio
{
    /// Fire-and-forget sound: `Sfx.Play(SfxEvent.HitHeavy, position)`.
    /// Pooled AudioSources, random pitch, per-event repeat limit, screen-position panning.
    /// Uses the clips in SfxLibrary, or a generated placeholder when an event has none.
    public class Sfx : MonoBehaviour
    {
        private const int Voices = 20;

        private static Sfx instance;

        private readonly List<AudioSource> sources = new List<AudioSource>();
        private readonly Dictionary<SfxEvent, float> lastPlayed = new Dictionary<SfxEvent, float>();
        private int next;

        private static Sfx Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("[Sfx]").AddComponent<Sfx>();
                return instance;
            }
        }

        private void Awake()
        {
            for (var i = 0; i < Voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources.Add(source);
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void Play(SfxEvent sfx, Vector3? worldPosition = null, float volumeScale = 1f, float pitchScale = 1f)
        {
            var library = VelaSettings.Sfx;
            var entry = library.Find(sfx);

            AudioClip clip = null;
            if (entry != null && entry.clips != null && entry.clips.Length > 0)
            {
                clip = entry.clips[Random.Range(0, entry.clips.Length)];
            }
            if (clip == null && library.placeholderWhenEmpty) clip = ProceduralSfx.Get(sfx);
            if (clip == null) return;

            var self = Instance;
            var minInterval = entry != null ? entry.minInterval : 0.03f;
            if (self.lastPlayed.TryGetValue(sfx, out var last) && Time.unscaledTime - last < minInterval) return;
            self.lastPlayed[sfx] = Time.unscaledTime;

            var source = self.sources[self.next];
            self.next = (self.next + 1) % self.sources.Count;

            var variance = entry != null ? entry.pitchVariance : 0.08f;
            source.clip = clip;
            source.volume = library.masterVolume * (entry != null ? entry.volume : 1f) * volumeScale;
            source.pitch = pitchScale * (1f + Random.Range(-variance, variance));
            source.panStereo = worldPosition.HasValue ? Pan(worldPosition.Value) * library.stereoPan : 0f;
            source.Play();
        }

        private static float Pan(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null) return 0f;
            var viewport = cam.WorldToViewportPoint(world);
            return Mathf.Clamp(viewport.x * 2f - 1f, -1f, 1f);
        }
    }
}

using UnityEngine;

namespace Vela.Core
{
    /// Freeze-frame on impact. Drops Time.timeScale for a few real-time milliseconds.
    public class HitStop : MonoBehaviour
    {
        private static HitStop instance;

        private float until;
        private bool active;

        public static void Request(float seconds, float timeScale = 0.03f)
        {
            if (seconds <= 0f) return;

            if (instance == null)
            {
                instance = new GameObject("[HitStop]").AddComponent<HitStop>();
            }

            instance.until = Mathf.Max(instance.until, Time.unscaledTime + seconds);
            instance.active = true;
            Time.timeScale = timeScale;
        }

        private void Update()
        {
            if (!active || Time.unscaledTime < until) return;

            active = false;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            instance = null;
            Time.timeScale = 1f;
        }
    }
}

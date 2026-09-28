using UnityEngine;
using Vela.Gameplay;

namespace Vela.FX
{
    public class AfterImage : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime;
        private float age;
        private Color color;

        public void Play(SpriteRenderer source, Color newColor, float newLifetime)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var from = source.transform;
            transform.SetPositionAndRotation(from.position, from.rotation);
            transform.localScale = from.lossyScale;

            spriteRenderer.sharedMaterial = VelaSettings.FlashMaterial;
            spriteRenderer.sprite = source.sprite;
            spriteRenderer.flipX = source.flipX;
            spriteRenderer.color = newColor;

            color = newColor;
            lifetime = Mathf.Max(0.02f, newLifetime);
            age = 0f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                gameObject.SetActive(false);
                return;
            }

            var c = color;
            c.a *= 1f - age / lifetime;
            spriteRenderer.color = c;
        }
    }
}

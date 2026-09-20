using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Enemies
{
    /// A slow, readable orb: the caster's whole threat. Slow enough to sidestep,
    /// fast enough to punish standing still.
    public class Projectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed = 9f;
        private float radius = 0.3f;
        private float lifeRemaining = 4f;
        private DamageInfo payload;

        public static Projectile Spawn(Vector3 position, Vector3 direction, float speed, float radius,
            DamageInfo payload, Color color, float life)
        {
            GameObject go = PrimitiveFx.CreateShape(PrimitiveType.Sphere, color);
            go.name = "Projectile";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (radius * 2f);

            Projectile projectile = go.AddComponent<Projectile>();
            direction.y = 0f;
            projectile.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            projectile.speed = speed;
            projectile.radius = radius;
            projectile.payload = payload;
            projectile.lifeRemaining = life;

            return projectile;
        }

        private void Update()
        {
            float delta = Time.deltaTime;

            lifeRemaining -= delta;
            if (lifeRemaining <= 0f)
            {
                Burst(new Color(0.7f, 0.7f, 0.8f, 0.7f));
                return;
            }

            float step = speed * delta;
            RaycastHit hit;

            if (Physics.SphereCast(transform.position, radius, direction, out hit, step,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                Health victim = hit.collider.GetComponentInParent<Health>();
                if (victim != null && victim.Side != payload.source && victim.IsAlive)
                {
                    payload.direction = direction;
                    victim.Apply(payload);
                    Burst(new Color(1f, 0.5f, 0.9f, 0.8f));
                    return;
                }

                if (victim == null)
                {
                    // Scenery.
                    Burst(new Color(0.75f, 0.75f, 0.85f, 0.7f));
                    return;
                }
            }

            transform.position += direction * step;
        }

        private void Burst(Color color)
        {
            PrimitiveFx.Spark(transform.position, color, 0.5f);
            Destroy(gameObject);
        }
    }
}

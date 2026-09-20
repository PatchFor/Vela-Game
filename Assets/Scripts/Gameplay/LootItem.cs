using System.Collections.Generic;
using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Gameplay
{
    public enum LootKind
    {
        Gold = 0,
        Potion = 1,
        Shard = 2
    }

    /// A thing on the floor. Pops out of a dead monster, lands, spins, and waits
    /// for the player to walk over and press F.
    public class LootItem : MonoBehaviour
    {
        /// Everything currently lying on the ground — PlayerPickup and the HUD read this.
        public static readonly List<LootItem> All = new List<LootItem>();

        [SerializeField] private LootKind kind = LootKind.Gold;
        [SerializeField] private string displayName = "Gold";
        [SerializeField] private int scoreValue = 5;
        [SerializeField] private int healAmount;
        [SerializeField] private float lifeSeconds = 30f;

        private Vector3 velocity;
        private float restHeight;
        private bool settled;
        private float age;
        private float bobPhase;
        private float nextRefusalMessageAt;

        public LootKind Kind { get { return kind; } }
        public string DisplayName { get { return displayName; } }
        public int ScoreValue { get { return scoreValue; } }
        public int HealAmount { get { return healAmount; } }
        public float Age { get { return age; } }
        public float LifeSeconds { get { return lifeSeconds; } }

        public static void ClearAll()
        {
            All.Clear();
        }

        public static LootItem Spawn(LootKind kind, Vector3 origin)
        {
            Color color;
            PrimitiveType shape;
            float scale;
            string label;
            int score;
            int heal;

            switch (kind)
            {
                case LootKind.Potion:
                    color = new Color(1f, 0.38f, 0.45f);
                    shape = PrimitiveType.Sphere;
                    scale = 0.5f;
                    label = "Red Potion";
                    score = 2;
                    heal = 1;
                    break;

                case LootKind.Shard:
                    color = new Color(0.4f, 0.85f, 1f);
                    shape = PrimitiveType.Capsule;
                    scale = 0.45f;
                    label = "Blue Shard";
                    score = 15;
                    heal = 0;
                    break;

                default:
                    color = new Color(0.98f, 0.82f, 0.28f);
                    shape = PrimitiveType.Cube;
                    scale = 0.42f;
                    label = "Gold";
                    score = 5;
                    heal = 0;
                    break;
            }

            GameObject go = PrimitiveFx.CreateShape(shape, color);
            go.name = "Loot_" + label;
            go.transform.position = origin + Vector3.up * 0.4f;
            go.transform.localScale = Vector3.one * scale;

            LootItem item = go.AddComponent<LootItem>();
            item.kind = kind;
            item.displayName = label;
            item.scoreValue = score;
            item.healAmount = heal;
            item.velocity = new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(4f, 6f), Random.Range(-2.2f, 2.2f));
            item.bobPhase = Random.Range(0f, 6.28f);

            return item;
        }

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            age += delta;

            if (!settled)
            {
                velocity.y -= 18f * delta;
                transform.position += velocity * delta;

                if (velocity.y < 0f && transform.position.y <= GroundHeight())
                {
                    settled = true;
                    restHeight = GroundHeight();
                    Vector3 position = transform.position;
                    position.y = restHeight;
                    transform.position = position;
                    PrimitiveFx.Ring(transform.position, 0.7f, new Color(1f, 0.95f, 0.6f, 0.35f), 0.3f, 0.3f, 1.2f);
                }
            }
            else
            {
                Vector3 position = transform.position;
                position.y = restHeight + Mathf.Sin(Time.time * 2.6f + bobPhase) * 0.14f;
                transform.position = position;
            }

            transform.Rotate(Vector3.up, 110f * delta, Space.World);

            if (lifeSeconds > 0f && age >= lifeSeconds)
            {
                CombatFeed.Post(displayName + " faded away", new Color(0.6f, 0.6f, 0.65f));
                Destroy(gameObject);
            }
        }

        private float GroundHeight()
        {
            // Rays are cast from above, so the corpse the item just fell out of
            // (and the player) have to be skipped or the item lands mid-air.
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 3f, Vector3.down, 14f, ~0,
                QueryTriggerInteraction.Ignore);

            float best = 0.45f;
            bool found = false;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null) continue;
                if (hits[i].collider.GetComponentInParent<Health>() != null) continue;

                float candidate = hits[i].point.y + 0.45f;
                if (!found || candidate > best)
                {
                    best = candidate;
                    found = true;
                }
            }

            return best;
        }

        /// Returns false when the pickup is refused (full HP on a potion, say).
        public bool TryPickup(GameObject picker)
        {
            if (picker == null) return false;

            if (healAmount > 0)
            {
                Health health = picker.GetComponent<Health>();
                if (health != null && !health.Heal(healAmount))
                {
                    if (Time.time >= nextRefusalMessageAt)
                    {
                        nextRefusalMessageAt = Time.time + 1f;
                        DamagePopups.Spawn(transform.position + Vector3.up * 1f, "HP already full",
                            new Color(0.8f, 0.8f, 0.85f), 0.95f, 0.8f);
                    }

                    return false;
                }
            }

            if (PrototypeGameManager.Instance != null) PrototypeGameManager.Instance.RegisterPickup(this);

            string suffix = healAmount > 0 ? " (+" + healAmount + " HP)" : " (+" + scoreValue + ")";
            DamagePopups.Spawn(transform.position + Vector3.up * 1.2f, displayName + suffix,
                new Color(1f, 0.92f, 0.5f), 1.1f, 1f);
            CombatFeed.Post("Picked up " + displayName + suffix, new Color(1f, 0.92f, 0.5f));
            PrimitiveFx.Ring(transform.position, 1f, new Color(1f, 0.95f, 0.55f, 0.5f), 0.3f, 1.2f, 0.2f);

            Destroy(gameObject);
            return true;
        }
    }
}

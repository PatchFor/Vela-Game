using UnityEngine;
using Vela.Core;

namespace Vela.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private int scoreValue = 1;
        [SerializeField] private int healAmount;
        [SerializeField] private bool countsTowardGoal = true;

        [Header("Motion")]
        [SerializeField] private float spinDegreesPerSecond = 120f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float bobSpeed = 2.5f;

        private Vector3 anchor;

        public int ScoreValue => scoreValue;
        public bool CountsTowardGoal => countsTowardGoal;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            anchor = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            var offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = anchor + Vector3.up * offset;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (healAmount > 0)
            {
                var health = other.GetComponent<Health>();
                if (health != null && !health.Heal(healAmount)) return;
            }

            if (PrototypeGameManager.Instance != null)
            {
                PrototypeGameManager.Instance.Collect(this);
            }

            Destroy(gameObject);
        }
    }
}

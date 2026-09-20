using UnityEngine;

namespace Vela.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private int scoreValue = 1;
        [SerializeField] private float spinDegreesPerSecond = 120f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float bobSpeed = 2.5f;

        private Vector3 anchor;

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

            if (PrototypeGameManager.Instance != null)
            {
                PrototypeGameManager.Instance.Collect(scoreValue);
            }

            Destroy(gameObject);
        }
    }
}

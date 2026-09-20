using UnityEngine;
using UnityEngine.SceneManagement;
using Vela.Core;

namespace Vela.Gameplay
{
    public class PrototypeGameManager : MonoBehaviour
    {
        public static PrototypeGameManager Instance { get; private set; }

        [SerializeField] private bool showHud = true;

        private int score;
        private int collected;
        private int total;
        private float elapsed;
        private bool finished;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            total = FindObjectsByType<Collectible>(FindObjectsSortMode.None).Length;
        }

        private void Update()
        {
            if (!finished) elapsed += Time.deltaTime;
            if (VelaInput.RestartPressed) Restart();
        }

        public void Collect(int value)
        {
            score += value;
            collected++;

            if (collected >= total && total > 0) finished = true;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            if (!showHud) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            GUI.Label(new Rect(20, 16, 400, 30), $"Shards: {collected}/{total}   Score: {score}", style);
            GUI.Label(new Rect(20, 44, 400, 30), $"Time: {elapsed:0.0}s", style);
            GUI.Label(new Rect(20, 72, 600, 30), "WASD move · Space dash · Q/E rotate camera · R restart", style);

            if (!finished) return;

            var banner = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 50),
                $"All shards collected in {elapsed:0.0}s — press R to replay", banner);
        }
    }
}

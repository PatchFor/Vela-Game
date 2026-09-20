using UnityEngine;
using UnityEngine.SceneManagement;
using Vela.Core;
using Vela.Player;

namespace Vela.Gameplay
{
    public class PrototypeGameManager : MonoBehaviour
    {
        public static PrototypeGameManager Instance { get; private set; }

        [SerializeField] private bool showHud = true;
        [SerializeField] private float damageFlashSeconds = 0.3f;

        private Health playerHealth;
        private IsometricPlayerController playerController;

        private int score;
        private int collected;
        private int total;
        private int lastKnownHealth;
        private float elapsed;
        private float flashUntil;
        private bool won;
        private bool dead;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            total = 0;
            foreach (var collectible in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            {
                if (collectible.CountsTowardGoal) total++;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            playerController = player.GetComponent<IsometricPlayerController>();
            playerHealth = player.GetComponent<Health>();

            if (playerHealth == null) return;

            lastKnownHealth = playerHealth.Current;
            playerHealth.Changed += OnHealthChanged;
            playerHealth.Died += OnPlayerDied;
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed -= OnHealthChanged;
                playerHealth.Died -= OnPlayerDied;
            }

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!won && !dead) elapsed += Time.deltaTime;
            if (VelaInput.RestartPressed) Restart();
        }

        public void Collect(Collectible collectible)
        {
            score += collectible.ScoreValue;

            if (!collectible.CountsTowardGoal) return;

            collected++;
            if (collected >= total && total > 0 && !dead) won = true;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnHealthChanged(Health health)
        {
            if (health.Current < lastKnownHealth) flashUntil = Time.time + damageFlashSeconds;
            lastKnownHealth = health.Current;
        }

        private void OnPlayerDied(Health health)
        {
            dead = true;
            won = false;
            if (playerController != null) playerController.enabled = false;
        }

        private void OnGUI()
        {
            if (!showHud) return;

            DrawDamageFlash();

            var label = new GUIStyle(GUI.skin.label) { fontSize = 18 };

            DrawHealthBar(new Rect(20f, 18f, 260f, 20f));
            GUI.Label(new Rect(20f, 44f, 500f, 26f), $"Orbs: {collected}/{total}    Score: {score}", label);
            GUI.Label(new Rect(20f, 68f, 500f, 26f), $"Time: {elapsed:0.0}s", label);
            GUI.Label(new Rect(20f, 92f, 700f, 26f),
                "WASD move · Space dash (i-frames) · Q/E rotate camera · R restart", label);
            GUI.Label(new Rect(20f, 116f, 700f, 26f),
                "Monsters flash yellow before lunging — dash through the attack to dodge.", label);

            DrawBanner();
        }

        private void DrawHealthBar(Rect rect)
        {
            if (playerHealth == null) return;

            var previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f),
                Texture2D.whiteTexture);

            var segments = Mathf.Max(1, playerHealth.Max);
            var gap = 3f;
            var segmentWidth = (rect.width - gap * (segments - 1)) / segments;
            var fillColor = playerHealth.Normalized > 0.5f
                ? new Color(0.35f, 0.85f, 0.45f)
                : playerHealth.Normalized > 0.25f
                    ? new Color(0.95f, 0.78f, 0.30f)
                    : new Color(0.92f, 0.32f, 0.32f);

            for (var i = 0; i < segments; i++)
            {
                var cell = new Rect(rect.x + i * (segmentWidth + gap), rect.y, segmentWidth, rect.height);
                GUI.color = i < playerHealth.Current ? fillColor : new Color(1f, 1f, 1f, 0.12f);
                GUI.DrawTexture(cell, Texture2D.whiteTexture);
            }

            GUI.color = previous;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            GUI.Label(new Rect(rect.xMax + 14f, rect.y - 2f, 160f, 24f),
                $"HP {playerHealth.Current}/{playerHealth.Max}", style);
        }

        private void DrawDamageFlash()
        {
            if (Time.time >= flashUntil) return;

            var remaining = Mathf.Clamp01((flashUntil - Time.time) / Mathf.Max(0.01f, damageFlashSeconds));
            var previous = GUI.color;
            GUI.color = new Color(0.9f, 0.1f, 0.1f, 0.35f * remaining);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawBanner()
        {
            if (!won && !dead) return;

            var banner = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter
            };

            var message = dead
                ? "The monsters got you — press R to try again"
                : $"All orbs collected in {elapsed:0.0}s — press R to replay";

            GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 60f), message, banner);
        }
    }
}

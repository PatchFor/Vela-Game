using UnityEngine;
using UnityEngine.SceneManagement;
using Vela.CameraRig;
using Vela.Config;
using Vela.Core;
using Vela.Enemies;

namespace Vela.Gameplay
{
    /// Scene bootstrap and test hotkeys. Runs before everything else so the shared configs
    /// are in place when other components wake up.
    [DefaultExecutionOrder(-100)]
    public class CombatGameManager : MonoBehaviour
    {
        [SerializeField] private CombatFeelConfig feel;
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private FxLibrary fx;
        [SerializeField] private Items.LootConfig lootConfig;
        [SerializeField] private Audio.SfxLibrary sfxLibrary;
        [Tooltip("Where B teleports the player (the boss arena entrance).")]
        [SerializeField] private Transform bossArenaEntrance;

        [Header("Debug")]
        [Tooltip("F5 drops this table around the player (test inventory full, rarity looks...).")]
        [SerializeField] private Items.LootTable debugLoot;
        [Tooltip("O dresses the player in random pieces from this list.")]
        [SerializeField] private Items.ItemDefinition[] wardrobe = new Items.ItemDefinition[0];

        private EnemySpawnPoint[] spawnPoints = new EnemySpawnPoint[0];

        public static CombatGameManager Instance { get; private set; }

        public bool GodMode { get; private set; }
        public bool ShowHelp { get; private set; } = true;
        public bool BossDefeated { get; private set; }
        public int Kills { get; private set; }

        public void ConfigureAudio(Audio.SfxLibrary library) => sfxLibrary = library;

        public void ConfigureLoot(Items.LootConfig newLoot, Items.LootTable newDebugLoot, Items.ItemDefinition[] newWardrobe)
        {
            lootConfig = newLoot;
            debugLoot = newDebugLoot;
            wardrobe = newWardrobe;
        }

        public void Configure(CombatFeelConfig newFeel, CameraConfig newCamera, FxLibrary newFx, Transform bossEntrance)
        {
            feel = newFeel;
            cameraConfig = newCamera;
            fx = newFx;
            bossArenaEntrance = bossEntrance;
        }

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            CameraShake.Reset();
            Combat.ComboTracker.Reset();
            CombatRegistry.Clear();

            VelaSettings.Feel = feel;
            VelaSettings.Camera = cameraConfig;
            VelaSettings.Fx = fx;
            VelaSettings.Loot = lootConfig;
            VelaSettings.Sfx = sfxLibrary;
            HitStop.Mode = VelaSettings.Feel.hitStopMode;
            HudMessages.Clear();
        }

        private void Start()
        {
            spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
            if (spawnPoints.Length == 0)
            {
                Debug.LogWarning("Vela: no EnemySpawnPoints in this scene. Run Vela > Build Combat Prototype Scene.");
            }
            SpawnAll();
        }

        private void OnEnable() => EnemyBrain.AnyDefeated += OnEnemyDefeated;

        private void OnDisable() => EnemyBrain.AnyDefeated -= OnEnemyDefeated;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (VelaInput.RestartPressed) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (VelaInput.RespawnEnemiesPressed) SpawnAll();
            if (VelaInput.HelpPressed) ShowHelp = !ShowHelp;

            if (VelaInput.GodModePressed)
            {
                GodMode = !GodMode;
                var hero = CombatRegistry.Player;
                if (hero != null) hero.Health.Immortal = GodMode;
            }

            if (VelaInput.TeleportToBossPressed && bossArenaEntrance != null) TeleportPlayer(bossArenaEntrance.position);

            var player = CombatRegistry.Player;
            if (player != null && VelaInput.DebugLootPressed && debugLoot != null)
            {
                Items.LootSpawner.Drop(debugLoot, player.transform.position + player.Facing * 1.5f);
            }

            if (player != null && VelaInput.DebugOutfitPressed)
            {
                var inventory = player.GetComponent<Player.PlayerInventory>();
                if (inventory != null) inventory.RandomOutfit(wardrobe);
            }
        }

        public void SpawnAll()
        {
            BossDefeated = false;

            // Clear everything, including boss summons that have no spawn point.
            foreach (var enemy in CombatRegistry.Enemies.ToArray())
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            CombatRegistry.Enemies.Clear();

            var player = CombatRegistry.Player;
            if (player != null && player.IsAlive) player.Health.Heal(player.Health.Max);

            foreach (var point in spawnPoints)
            {
                if (point == null || !point.SpawnOnStart) continue;
                point.Spawn();
            }
        }

        private void OnEnemyDefeated(EnemyBrain brain)
        {
            Kills++;
            if (brain.Config is BossConfig) BossDefeated = true;
        }

        private static void TeleportPlayer(Vector3 position)
        {
            var player = CombatRegistry.Player;
            if (player == null) return;

            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = position;
            if (controller != null) controller.enabled = true;
        }
    }
}

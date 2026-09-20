using UnityEngine;
using UnityEngine.SceneManagement;
using Vela.Core;
using Vela.Enemies;
using Vela.Player;

namespace Vela.Gameplay
{
    /// Runs the test arena and draws the whole HUD with IMGUI, so the prototype
    /// needs no canvas, no fonts and no imported assets — every readable thing
    /// on screen is code you can tweak in one file.
    public class PrototypeGameManager : MonoBehaviour
    {
        public static PrototypeGameManager Instance { get; private set; }

        [SerializeField] private bool showHud = true;
        [SerializeField] private bool showHelpAtStart = true;
        [SerializeField] private float damageFlashSeconds = 0.35f;
        [SerializeField] private float feedLineSeconds = 6f;

        private Health playerHealth;
        private IsometricPlayerController playerMotor;
        private PlayerCombat playerCombat;
        private PlayerPickup playerPickup;
        private Camera view;

        private int score;
        private int kills;
        private int itemsPicked;
        private int lastKnownHealth;
        private float elapsed;
        private float flashUntil;
        private float healFlashUntil;
        private bool dead;
        private bool showHelp;

        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle bannerStyle;
        private GUIStyle popupStyle;

        private void Awake()
        {
            Instance = this;

            // Static feeds outlive a scene reload; start every run clean.
            CombatFeed.Clear();
            DamagePopups.Clear();
            ScreenShake.Clear();
            Time.timeScale = 1f;

            showHelp = showHelpAtStart;
        }

        private void Start()
        {
            view = Camera.main;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            playerMotor = player.GetComponent<IsometricPlayerController>();
            playerCombat = player.GetComponent<PlayerCombat>();
            playerPickup = player.GetComponent<PlayerPickup>();
            playerHealth = player.GetComponent<Health>();

            if (playerHealth == null) return;

            lastKnownHealth = playerHealth.Current;
            playerHealth.Changed += OnHealthChanged;
            playerHealth.Died += OnPlayerDied;

            CombatFeed.Post("Arena ready — H toggles the controls overlay", new Color(0.7f, 0.85f, 1f));
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
            if (!dead) elapsed += Time.deltaTime;

            DamagePopups.Tick(Time.unscaledDeltaTime);

            if (VelaInput.HelpPressed) showHelp = !showHelp;
            if (VelaInput.RestartPressed) Restart();
        }

        public void RegisterKill(MonsterBase monster)
        {
            kills++;
        }

        public void RegisterPickup(LootItem item)
        {
            itemsPicked++;
            score += item.ScoreValue;
            if (item.HealAmount > 0) healFlashUntil = Time.time + 0.25f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnHealthChanged(Health health)
        {
            if (health.Current < lastKnownHealth) flashUntil = Time.time + damageFlashSeconds;
            lastKnownHealth = health.Current;
        }

        private void OnPlayerDied(Health health, DamageInfo info)
        {
            dead = true;

            if (playerMotor != null) playerMotor.enabled = false;
            if (playerCombat != null) playerCombat.enabled = false;

            ScreenShake.Add(0.5f, 0.6f);
            Hitstop.Request(0.4f, 0.15f);
            CombatFeed.Post("You died — press R to restart", new Color(1f, 0.4f, 0.4f));
        }

        private void OnGUI()
        {
            if (!showHud) return;

            EnsureStyles();
            if (view == null) view = Camera.main;

            DrawScreenTint();
            DrawMonsterPlates();
            DrawLootMarkers();
            DrawPopups();

            DrawPlayerPanel();
            DrawStatsPanel();
            DrawMoveBar();
            DrawFeed();
            DrawPickupPrompt();
            DrawBanner();

            if (showHelp) DrawHelp();
        }

        // ---------------------------------------------------------------- HUD

        private void DrawPlayerPanel()
        {
            if (playerHealth == null) return;

            Rect panel = new Rect(16f, 14f, 320f, playerMotor != null ? 96f : 58f);
            Fill(panel, new Color(0f, 0f, 0f, 0.55f));

            Rect bar = new Rect(panel.x + 12f, panel.y + 14f, 220f, 20f);
            DrawSegmentedHealth(bar, playerHealth);

            Label(new Rect(bar.xMax + 10f, bar.y - 3f, 100f, 24f),
                "HP " + playerHealth.Current + "/" + playerHealth.Max, labelStyle, Color.white);

            if (playerMotor == null) return;

            Rect dodge = new Rect(panel.x + 12f, panel.y + 44f, 220f, 14f);
            float ready = playerMotor.DodgeReadyNormalized;
            Fill(Inflate(dodge, 2f), new Color(1f, 1f, 1f, 0.12f));
            Fill(new Rect(dodge.x, dodge.y, dodge.width * ready, dodge.height),
                ready >= 1f ? new Color(0.45f, 0.85f, 1f) : new Color(0.3f, 0.5f, 0.65f));

            string dodgeText = playerMotor.IsDodging
                ? "ROLLING — I-FRAMES"
                : ready >= 1f ? "DODGE READY (Space)" : "DODGE...";
            Label(new Rect(dodge.xMax + 10f, dodge.y - 6f, 200f, 24f), dodgeText, smallStyle,
                ready >= 1f ? new Color(0.6f, 0.9f, 1f) : new Color(0.6f, 0.65f, 0.7f));

            string state = playerHealth.IsInvulnerable ? "INVULNERABLE" : "";
            if (playerMotor.HasFocus)
            {
                state = "FOCUS — next hit +1 dmg (" + playerMotor.FocusRemaining.ToString("0.0") + "s)";
            }

            if (!string.IsNullOrEmpty(state))
            {
                Label(new Rect(panel.x + 12f, panel.y + 66f, 300f, 22f), state, smallStyle,
                    playerMotor.HasFocus ? new Color(1f, 0.9f, 0.5f) : new Color(0.6f, 0.9f, 1f));
            }
        }

        private void DrawSegmentedHealth(Rect rect, Health health)
        {
            Fill(Inflate(rect, 3f), new Color(0f, 0f, 0f, 0.6f));

            int segments = Mathf.Max(1, health.Max);
            float gap = 3f;
            float segmentWidth = (rect.width - gap * (segments - 1)) / segments;

            Color fill = health.Normalized > 0.5f
                ? new Color(0.35f, 0.85f, 0.45f)
                : health.Normalized > 0.25f
                    ? new Color(0.95f, 0.78f, 0.30f)
                    : new Color(0.92f, 0.32f, 0.32f);

            for (int i = 0; i < segments; i++)
            {
                Rect cell = new Rect(rect.x + i * (segmentWidth + gap), rect.y, segmentWidth, rect.height);
                Fill(cell, i < health.Current ? fill : new Color(1f, 1f, 1f, 0.12f));
            }
        }

        private void DrawStatsPanel()
        {
            Rect panel = new Rect(Screen.width - 236f, 14f, 220f, 92f);
            Fill(panel, new Color(0f, 0f, 0f, 0.55f));

            Label(new Rect(panel.x + 12f, panel.y + 8f, 200f, 22f),
                "Time  " + elapsed.ToString("0.0") + "s", smallStyle, new Color(0.8f, 0.85f, 0.9f));
            Label(new Rect(panel.x + 12f, panel.y + 28f, 200f, 22f),
                "Kills  " + kills, smallStyle, new Color(1f, 0.75f, 0.6f));
            Label(new Rect(panel.x + 12f, panel.y + 48f, 200f, 22f),
                "Items  " + itemsPicked, smallStyle, new Color(1f, 0.9f, 0.55f));
            Label(new Rect(panel.x + 12f, panel.y + 68f, 200f, 22f),
                "Score  " + score, smallStyle, new Color(0.7f, 0.9f, 1f));
        }

        private void DrawMoveBar()
        {
            if (playerCombat == null) return;

            int count = playerCombat.MoveCount;
            if (count == 0) return;

            float slotWidth = 150f;
            float slotHeight = 58f;
            float spacing = 10f;
            float totalWidth = count * slotWidth + (count - 1) * spacing;
            float left = (Screen.width - totalWidth) * 0.5f;
            float top = Screen.height - slotHeight - 26f;

            for (int i = 0; i < count; i++)
            {
                AttackMove move = playerCombat.GetMove(i);
                if (move == null) continue;

                Rect slot = new Rect(left + i * (slotWidth + spacing), top, slotWidth, slotHeight);
                float ready = playerCombat.CooldownNormalized(i);
                bool isCurrent = playerCombat.CurrentMoveIndex == i;

                Fill(slot, new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(slot.x, slot.yMax - 5f, slot.width * ready, 5f),
                    ready >= 1f ? move.color : new Color(0.35f, 0.38f, 0.45f));

                if (isCurrent)
                {
                    Color edge = playerCombat.CurrentPhase == PlayerCombat.Phase.Active
                        ? new Color(1f, 1f, 1f, 0.5f)
                        : new Color(1f, 1f, 1f, 0.22f);
                    Fill(slot, edge);
                }

                Color nameColor = ready >= 1f ? move.color : new Color(0.55f, 0.58f, 0.65f);
                Label(new Rect(slot.x + 10f, slot.y + 6f, slot.width - 16f, 22f), move.moveName, labelStyle, nameColor);
                Label(new Rect(slot.x + 10f, slot.y + 28f, slot.width - 16f, 20f),
                    move.keyLabel + "   " + move.damage + " dmg", smallStyle, new Color(0.75f, 0.78f, 0.85f));
            }

            // Combo pips for the slash chain.
            if (playerCombat.ComboActive)
            {
                float pipY = top - 22f;
                float pipWidth = 26f;
                float pipsTotal = playerCombat.ComboLength * (pipWidth + 6f);
                float pipLeft = (Screen.width - pipsTotal) * 0.5f;

                for (int i = 0; i < playerCombat.ComboLength; i++)
                {
                    Rect pip = new Rect(pipLeft + i * (pipWidth + 6f), pipY, pipWidth, 6f);
                    Fill(pip, i < playerCombat.ComboStep
                        ? new Color(1f, 0.85f, 0.4f)
                        : new Color(1f, 1f, 1f, 0.15f));
                }

                if (playerCombat.ComboStep >= playerCombat.ComboLength)
                {
                    Label(new Rect(0f, pipY - 24f, Screen.width, 22f), "FINISHER READY", centeredStyle,
                        new Color(1f, 0.85f, 0.4f));
                }
            }
        }

        private void DrawFeed()
        {
            float lineHeight = 20f;
            float bottom = Screen.height - 110f;
            int shown = 0;

            for (int i = CombatFeed.Lines.Count - 1; i >= 0; i--)
            {
                CombatFeed.Line line = CombatFeed.Lines[i];
                float age = Time.unscaledTime - line.bornAt;
                if (age > feedLineSeconds) continue;

                float alpha = Mathf.Clamp01((feedLineSeconds - age) / 1.2f);
                Color color = line.color;
                color.a = alpha;

                Rect rect = new Rect(18f, bottom - shown * lineHeight, 520f, lineHeight);
                Label(rect, line.text, smallStyle, color);

                shown++;
                if (shown >= 6) break;
            }
        }

        private void DrawPickupPrompt()
        {
            if (playerPickup == null || playerPickup.Nearest == null) return;

            LootItem item = playerPickup.Nearest;
            Rect rect = new Rect(0f, Screen.height * 0.62f, Screen.width, 28f);
            Fill(new Rect(Screen.width * 0.5f - 170f, rect.y - 4f, 340f, 30f), new Color(0f, 0f, 0f, 0.6f));
            Label(rect, "[F]  Pick up " + item.DisplayName, centeredStyle, new Color(1f, 0.92f, 0.5f));
        }

        private void DrawMonsterPlates()
        {
            if (view == null) return;

            for (int i = 0; i < MonsterBase.All.Count; i++)
            {
                MonsterBase monster = MonsterBase.All[i];
                if (monster == null || monster.IsDead || monster.Life == null) continue;

                Vector3 screen = view.WorldToScreenPoint(monster.transform.position + Vector3.up * 2.4f);
                if (screen.z <= 0f) continue;

                float x = screen.x - 60f;
                float y = Screen.height - screen.y;

                Label(new Rect(x - 30f, y - 20f, 180f, 18f), monster.DisplayName, smallStyle, monster.BaseTint);

                Rect bar = new Rect(x, y, 120f, 7f);
                Fill(Inflate(bar, 2f), new Color(0f, 0f, 0f, 0.65f));
                Fill(new Rect(bar.x, bar.y, bar.width * monster.Life.Normalized, bar.height),
                    Color.Lerp(new Color(0.9f, 0.25f, 0.25f), new Color(0.45f, 0.9f, 0.45f),
                        monster.Life.Normalized));

                Color phaseColor = monster.IsTelegraphing ? new Color(1f, 0.92f, 0.35f) : monster.PhaseColor;
                Label(new Rect(x - 30f, y + 8f, 180f, 18f), monster.PhaseLabel, smallStyle, phaseColor);

                BruteMonster brute = monster as BruteMonster;
                if (brute != null && brute.PlayerInArmorArc)
                {
                    Label(new Rect(x - 30f, y + 24f, 180f, 18f), "FRONT ARMOUR — flank it", smallStyle,
                        new Color(0.7f, 0.82f, 1f));
                }
            }
        }

        private void DrawLootMarkers()
        {
            if (view == null) return;

            for (int i = 0; i < LootItem.All.Count; i++)
            {
                LootItem item = LootItem.All[i];
                if (item == null) continue;

                Vector3 screen = view.WorldToScreenPoint(item.transform.position + Vector3.up * 0.9f);
                if (screen.z <= 0f) continue;

                bool expiring = item.LifeSeconds > 0f && item.Age > item.LifeSeconds - 5f;
                Color color = expiring && Mathf.Repeat(Time.unscaledTime, 0.4f) < 0.2f
                    ? new Color(1f, 0.55f, 0.4f)
                    : new Color(1f, 0.92f, 0.55f, 0.9f);

                Label(new Rect(screen.x - 70f, Screen.height - screen.y - 18f, 140f, 18f),
                    item.DisplayName, centeredStyle, color);
            }
        }

        private void DrawPopups()
        {
            if (view == null) return;

            for (int i = 0; i < DamagePopups.Active.Count; i++)
            {
                DamagePopups.Popup popup = DamagePopups.Active[i];
                Vector3 screen = view.WorldToScreenPoint(popup.position);
                if (screen.z <= 0f) continue;

                float t = Mathf.Clamp01(popup.age / popup.life);
                Color color = popup.color;
                color.a = 1f - t * t;

                popupStyle.fontSize = Mathf.RoundToInt(18f * popup.size);
                Label(new Rect(screen.x - 100f, Screen.height - screen.y - 20f, 200f, 30f),
                    popup.text, popupStyle, color);
            }
        }

        private void DrawScreenTint()
        {
            if (Time.time < flashUntil)
            {
                float remaining = Mathf.Clamp01((flashUntil - Time.time) / Mathf.Max(0.01f, damageFlashSeconds));
                Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.9f, 0.1f, 0.1f, 0.35f * remaining));
            }

            if (Time.time < healFlashUntil)
            {
                float remaining = Mathf.Clamp01((healFlashUntil - Time.time) / 0.25f);
                Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.2f, 0.9f, 0.4f, 0.18f * remaining));
            }

            if (playerHealth != null && playerHealth.IsAlive && playerHealth.Normalized <= 0.34f)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                Fill(new Rect(0f, 0f, Screen.width, 6f), new Color(0.9f, 0.15f, 0.15f, 0.25f + 0.25f * pulse));
                Fill(new Rect(0f, Screen.height - 6f, Screen.width, 6f),
                    new Color(0.9f, 0.15f, 0.15f, 0.25f + 0.25f * pulse));
            }
        }

        private void DrawBanner()
        {
            if (!dead) return;

            Fill(new Rect(0f, Screen.height * 0.40f, Screen.width, 90f), new Color(0f, 0f, 0f, 0.65f));
            Label(new Rect(0f, Screen.height * 0.40f + 12f, Screen.width, 40f), "YOU DIED", bannerStyle,
                new Color(1f, 0.35f, 0.35f));
            Label(new Rect(0f, Screen.height * 0.40f + 54f, Screen.width, 26f),
                "Kills " + kills + "   ·   Items " + itemsPicked + "   ·   Score " + score + "   —   press R to restart",
                centeredStyle, Color.white);
        }

        private void DrawHelp()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 300f, 120f, 600f, 268f);
            Fill(panel, new Color(0f, 0f, 0f, 0.78f));

            string[] lines =
            {
                "VELA — COMBAT FEEL TEST ARENA          (H hides this)",
                "",
                "WASD / arrows    move (camera-relative)",
                "Space / Shift    dodge roll — i-frames; roll through an attack for a PERFECT DODGE",
                "J / LMB          Slash — fast 3-hit chain, the 3rd swing is a knockback finisher",
                "K / RMB          Lunge Thrust — gap closer, good against the Caster",
                "L / MMB          Spin Smash — slow 360 heavy hit, staggers anything it touches",
                "F                pick up the drop you are standing on",
                "Q / E            rotate camera      ·      R restart",
                "",
                "Rusher (red)     flashes yellow, then lunges. Roll through it, hit the recovery.",
                "Caster (violet)  kites and fires slow orbs, blinks when cornered. Thrust in.",
                "Brute (blue)     armoured in front, huge telegraphed slam. Roll behind and hit its back.",
            };

            for (int i = 0; i < lines.Length; i++)
            {
                Color color = i == 0 ? new Color(1f, 0.85f, 0.4f) : new Color(0.85f, 0.88f, 0.95f);
                Label(new Rect(panel.x + 18f, panel.y + 12f + i * 19f, panel.width - 30f, 20f),
                    lines[i], smallStyle, color);
            }
        }

        // ------------------------------------------------------------ plumbing

        private void EnsureStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 15;
                labelStyle.fontStyle = FontStyle.Bold;
            }

            if (smallStyle == null)
            {
                smallStyle = new GUIStyle(GUI.skin.label);
                smallStyle.fontSize = 13;
            }

            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle(GUI.skin.label);
                centeredStyle.fontSize = 15;
                centeredStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label);
                bannerStyle.fontSize = 34;
                bannerStyle.fontStyle = FontStyle.Bold;
                bannerStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (popupStyle == null)
            {
                popupStyle = new GUIStyle(GUI.skin.label);
                popupStyle.fontSize = 18;
                popupStyle.fontStyle = FontStyle.Bold;
                popupStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = previous;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Rect Inflate(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }
    }
}

using UnityEngine;
using Vela.Combat;
using Vela.Core;
using Vela.Enemies;
using Vela.Gameplay;
using Vela.Player;

namespace Vela.UI
{
    /// IMGUI HUD: player HP, weapon slots + charge meter, the boss bar across the top, and
    /// banners. Crude on purpose; swap for UGUI once the feel is right.
    public class CombatHUD : MonoBehaviour
    {
        private static readonly Color Back = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Empty = new Color(1f, 1f, 1f, 0.1f);

        private PlayerCombat combat;
        private float playerChip;
        private float bossChip;
        private float hurtFlashUntil;
        private float hurtFlashDuration = 0.25f;
        private GUIStyle label;
        private GUIStyle center;
        private GUIStyle small;

        private void OnEnable() => DamageFeedback.PlayerHurt += OnPlayerHurt;
        private void OnDisable() => DamageFeedback.PlayerHurt -= OnPlayerHurt;

        private void OnPlayerHurt(Health health, DamageInfo info)
        {
            hurtFlashDuration = Mathf.Max(0.01f, VelaSettings.Feel.playerHurtScreenFlash);
            hurtFlashUntil = Time.unscaledTime + hurtFlashDuration;
        }

        private void Update()
        {
            var player = CombatRegistry.Player;
            if (player != null && combat == null) combat = player.GetComponent<PlayerCombat>();

            // "Chip" bars trail behind the real value so big hits read clearly.
            var dt = Time.unscaledDeltaTime;
            if (player != null) playerChip = Chase(playerChip, player.Health.Normalized, dt);
            var boss = BossController.Current;
            if (boss != null) bossChip = Chase(bossChip, boss.Health.Normalized, dt);
        }

        private static float Chase(float chip, float value, float dt)
        {
            if (chip < value) return value;
            return Mathf.MoveTowards(chip, value, 0.5f * dt);
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = Screen.height / 1080f;
            label.fontSize = Mathf.RoundToInt(20 * s);
            small.fontSize = Mathf.RoundToInt(16 * s);
            center.fontSize = Mathf.RoundToInt(34 * s);

            DrawHurtFlash();
            DrawPlayer(s);
            DrawWeapons(s);
            DrawCharge(s);
            DrawBoss(s);
            DrawHelp(s);
            DrawBanner(s);
        }

        private void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            label.normal.textColor = Color.white;
            small = new GUIStyle(GUI.skin.label);
            small.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
            center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            center.normal.textColor = Color.white;
        }

        private static void Fill(Rect r, Color c)
        {
            var previous = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Bar(Rect r, float value, float chip, Color color)
        {
            Fill(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), Back);
            Fill(r, Empty);
            Fill(new Rect(r.x, r.y, r.width * chip, r.height), new Color(1f, 1f, 1f, 0.75f));
            Fill(new Rect(r.x, r.y, r.width * value, r.height), color);
        }

        private void DrawHurtFlash()
        {
            if (Time.unscaledTime >= hurtFlashUntil) return;
            var t = (hurtFlashUntil - Time.unscaledTime) / hurtFlashDuration;
            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.9f, 0.1f, 0.1f, 0.3f * t));
        }

        private void DrawPlayer(float s)
        {
            var player = CombatRegistry.Player;
            if (player == null) return;

            var h = player.Health;
            var rect = new Rect(24f * s, 24f * s, 340f * s, 22f * s);
            var color = h.Normalized > 0.5f ? new Color(0.35f, 0.85f, 0.45f)
                : h.Normalized > 0.25f ? new Color(0.95f, 0.78f, 0.3f)
                : new Color(0.92f, 0.3f, 0.3f);
            Bar(rect, h.Normalized, playerChip, color);
            GUI.Label(new Rect(rect.x + 8f * s, rect.y - 2f * s, 300f * s, rect.height + 6f * s), $"{h.Current} / {h.Max}", label);

            var dash = 1f - player.DashCooldownNormalized;
            var dashRect = new Rect(rect.x, rect.yMax + 10f * s, 120f * s, 6f * s);
            Bar(dashRect, dash, dash, new Color(0.4f, 0.9f, 1f));
            GUI.Label(new Rect(dashRect.xMax + 8f * s, dashRect.y - 9f * s, 200f * s, 24f * s), "DASH", small);

            var manager = CombatGameManager.Instance;
            if (manager != null)
            {
                var line = $"Kills: {manager.Kills}";
                if (manager.GodMode) line += "    GOD MODE";
                GUI.Label(new Rect(rect.x, dashRect.yMax + 8f * s, 400f * s, 26f * s), line, small);
            }
        }

        private void DrawWeapons(float s)
        {
            if (combat == null || combat.Weapons.Count == 0) return;

            var count = combat.Weapons.Count;
            var w = 150f * s;
            var gap = 10f * s;
            var total = count * w + (count - 1) * gap;
            var x = (Screen.width - total) * 0.5f;
            var y = Screen.height - 70f * s;

            for (var i = 0; i < count; i++)
            {
                var weapon = combat.Weapons[i];
                var r = new Rect(x + i * (w + gap), y, w, 44f * s);
                var selected = i == combat.WeaponIndex;
                Fill(r, selected ? new Color(weapon.uiColor.r, weapon.uiColor.g, weapon.uiColor.b, 0.55f) : Back);
                if (selected) Fill(new Rect(r.x, r.yMax - 4f * s, r.width, 4f * s), weapon.uiColor);

                if (weapon.icon != null)
                {
                    var iconRect = new Rect(r.x + 6f * s, r.y + 6f * s, 32f * s, 32f * s);
                    GUI.DrawTexture(iconRect, weapon.icon.texture, ScaleMode.ScaleToFit);
                }

                GUI.Label(new Rect(r.x + 10f * s, r.y + 8f * s, r.width, r.height), $"{i + 1}  {weapon.displayName}", label);
            }

            var current = combat.CurrentWeapon;
            if (current != null && current.combo.Length > 1)
            {
                var hint = $"Combo {combat.ComboIndex + 1}/{current.combo.Length}";
                GUI.Label(new Rect(x, y - 28f * s, 300f * s, 26f * s), hint, small);
            }
        }

        private void DrawCharge(float s)
        {
            if (combat == null || !combat.IsCharging) return;

            var cam = Camera.main;
            var player = CombatRegistry.Player;
            if (cam == null || player == null) return;

            var screen = cam.WorldToScreenPoint(player.transform.position + Vector3.up * 2.4f);
            if (screen.z < 0f) return;

            var charge = combat.ChargeNormalized;
            var r = new Rect(screen.x - 40f * s, Screen.height - screen.y, 80f * s, 8f * s);
            var color = charge >= 1f ? Color.white : combat.CurrentWeapon.uiColor;
            Bar(r, charge, charge, color);
        }

        private void DrawBoss(float s)
        {
            var boss = BossController.Current;
            if (boss == null || !boss.IsEngaged || boss.IsDead) return;

            var width = Mathf.Min(900f * s, Screen.width - 80f * s);
            var r = new Rect((Screen.width - width) * 0.5f, 56f * s, width, 20f * s);

            GUI.Label(new Rect(r.x, r.y - 34f * s, width, 32f * s), boss.Config.displayName, label);
            var phaseText = $"{boss.PhaseName}";
            var phaseSize = small.CalcSize(new GUIContent(phaseText));
            GUI.Label(new Rect(r.xMax - phaseSize.x, r.y - 28f * s, phaseSize.x + 4f, 28f * s), phaseText, small);

            var color = boss.PhaseIndex == 0 ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.95f, 0.45f, 0.1f);
            Bar(r, boss.Health.Normalized, bossChip, color);

            // Tick marks where later phases start.
            for (var i = 1; i < boss.PhaseCount; i++)
            {
                var x = r.x + r.width * boss.PhaseThreshold(i);
                Fill(new Rect(x - 1.5f * s, r.y - 4f * s, 3f * s, r.height + 8f * s), new Color(1f, 1f, 1f, 0.9f));
            }

            GUI.Label(new Rect(r.x, r.yMax + 2f * s, width, 24f * s), $"{boss.Health.Current} / {boss.Health.Max}", small);

            if (Time.time - boss.AnnouncementTime < 2.2f && !string.IsNullOrEmpty(boss.Announcement))
            {
                var a = 1f - Mathf.Clamp01((Time.time - boss.AnnouncementTime - 1.6f) / 0.6f);
                var previous = GUI.color;
                GUI.color = new Color(1f, 0.6f, 0.3f, a);
                GUI.Label(new Rect(0f, Screen.height * 0.25f, Screen.width, 60f * s), boss.Announcement, center);
                GUI.color = previous;
            }
        }

        private void DrawHelp(float s)
        {
            var manager = CombatGameManager.Instance;
            if (manager == null || !manager.ShowHelp) return;

            const string text =
                "WASD move   ·   Mouse aim   ·   LMB attack / combo   ·   hold RMB charge, release when full\n" +
                "Space / Shift dash (i-frames)   ·   1 Sword  2 Bow  3 Greatsword  (Tab cycles)   ·   Mouse wheel / +/- zoom\n" +
                "T respawn monsters   ·   B go to boss   ·   G god mode   ·   R restart   ·   H / F1 hide help";
            var r = new Rect(24f * s, Screen.height - 170f * s, Screen.width - 48f * s, 90f * s);
            GUI.Label(r, text, small);
        }

        private void DrawBanner(float s)
        {
            var player = CombatRegistry.Player;
            var manager = CombatGameManager.Instance;
            string message = null;

            if (player != null && !player.IsAlive) message = "You fell — press R to restart";
            else if (manager != null && manager.BossDefeated) message = "Boss defeated! — T respawns everything";

            if (message == null) return;
            GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 60f * s), message, center);
        }
    }
}

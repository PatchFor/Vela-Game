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

            DrawScreenEffects();
            DrawEnemyBars(s);
            DrawCombo(s);
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
            comboStyle = new GUIStyle(center) { alignment = TextAnchor.MiddleRight, clipping = TextClipping.Overflow };

            // Transparent middle, opaque edges: used for hurt / low-HP / crit screen effects.
            const int size = 64;
            vignette = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                vignette.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.05f, d))));
            }
            vignette.Apply();
        }

        private GUIStyle comboStyle;
        private Texture2D vignette;

        private void OnDestroy()
        {
            if (vignette != null) Destroy(vignette);
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

        private void DrawVignette(Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vignette, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private void DrawScreenEffects()
        {
            var feel = VelaSettings.Feel;
            var now = Time.unscaledTime;

            // Low health: slow red pulse on the screen edges.
            var player = CombatRegistry.Player;
            if (player != null && player.IsAlive && player.Health.Normalized <= feel.lowHealthWarning)
            {
                var pulse = 0.25f + 0.2f * Mathf.Sin(now * 5f);
                DrawVignette(new Color(0.8f, 0.05f, 0.05f, pulse));
            }

            // Took a hit: strong red edges plus a faint full-screen tint.
            if (now < hurtFlashUntil)
            {
                var t = (hurtFlashUntil - now) / hurtFlashDuration;
                DrawVignette(new Color(1f, 0.1f, 0.1f, 0.85f * t));
                Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.9f, 0.1f, 0.1f, 0.12f * t));
            }

            // Crit: very short white pop.
            var critAge = now - ComboTracker.LastCritTime;
            if (critAge < 0.1f && feel.critScreenFlash > 0f)
            {
                var t = 1f - critAge / 0.1f;
                Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(1f, 1f, 0.9f, feel.critScreenFlash * t));
                DrawVignette(new Color(feel.critColor.r, feel.critColor.g, feel.critColor.b, 0.5f * t));
            }
        }

        /// Small HP bar (and poise bar for armored enemies) over each recently hit monster.
        private void DrawEnemyBars(float s)
        {
            var feel = VelaSettings.Feel;
            var cam = Camera.main;
            if (!feel.showEnemyHealthBars || cam == null) return;

            foreach (var enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.Config == null) continue;
                if (enemy.GetComponent<BossController>() != null) continue;

                var age = Time.time - enemy.LastHitTime;
                if (age > feel.enemyBarLinger) continue;

                var height = enemy.Config.visual.worldHeight + enemy.Config.visual.hoverHeight + 0.35f;
                var screen = cam.WorldToScreenPoint(enemy.transform.position + Vector3.up * height);
                if (screen.z < 0f) continue;

                var alpha = Mathf.Clamp01((feel.enemyBarLinger - age) / 0.4f);
                var w = Mathf.Clamp(enemy.Config.colliderRadius * 110f, 50f, 110f) * s;
                var r = new Rect(screen.x - w * 0.5f, Screen.height - screen.y, w, 6f * s);

                Fill(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), new Color(0f, 0f, 0f, 0.7f * alpha));
                Fill(r, new Color(1f, 1f, 1f, 0.12f * alpha));
                Fill(new Rect(r.x, r.y, r.width * enemy.Health.Normalized, r.height),
                    new Color(0.9f, 0.25f, 0.25f, alpha));

                if (enemy.Config.poise > 0f)
                {
                    var p = new Rect(r.x, r.yMax + 3f * s, r.width, 3f * s);
                    Fill(p, new Color(1f, 1f, 1f, 0.1f * alpha));
                    var poiseColor = enemy.IsStaggered ? Color.white : feel.breakColor;
                    var poise = enemy.IsStaggered ? 1f : enemy.PoiseNormalized;
                    Fill(new Rect(p.x, p.y, p.width * poise, p.height), new Color(poiseColor.r, poiseColor.g, poiseColor.b, alpha));
                }
            }
        }

        private void DrawCombo(float s)
        {
            var feel = VelaSettings.Feel;
            if (!feel.showComboCounter || !ComboTracker.IsActive || ComboTracker.Count < feel.comboMinimum) return;

            var popAge = Time.unscaledTime - ComboTracker.PopTime;
            var pop = popAge < 0.12f ? Mathf.Lerp(1.5f, 1f, popAge / 0.12f) : 1f;
            var count = ComboTracker.Count;
            var color = count >= 30 ? new Color(1f, 0.45f, 0.3f) : count >= 15 ? feel.critColor : Color.white;

            var right = Screen.width - 40f * s;
            var y = Screen.height * 0.32f;
            comboStyle.fontSize = Mathf.RoundToInt(64f * s * pop);
            var shadow = new Rect(right - 400f * s + 3f, y + 3f, 400f * s, 80f * s);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(shadow, count.ToString(), comboStyle);
            GUI.color = color;
            GUI.Label(new Rect(right - 400f * s, y, 400f * s, 80f * s), count.ToString(), comboStyle);

            comboStyle.fontSize = Mathf.RoundToInt(20f * s);
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.Label(new Rect(right - 400f * s, y + 62f * s, 400f * s, 30f * s), "HITS", comboStyle);
            GUI.color = previous;

            var bar = new Rect(right - 120f * s, y + 92f * s, 120f * s, 4f * s);
            Fill(bar, new Color(1f, 1f, 1f, 0.15f));
            Fill(new Rect(bar.xMax - bar.width * ComboTracker.TimeLeftNormalized, bar.y,
                bar.width * ComboTracker.TimeLeftNormalized, bar.height), color);
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

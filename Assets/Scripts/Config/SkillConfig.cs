using UnityEngine;

namespace Vela.Config
{
    /// What a mouse button does. Chosen in the inventory window (I) or in Player.asset.
    public enum MouseAction
    {
        None,
        BasicAttack,
        ChargedAttack,
        Skill1,
        Skill2,
        Skill3,
        Skill4
    }

    /// An active skill on keys 1–4 (or a mouse button). Uses the same AttackStep as weapons,
    /// so melee sweeps, projectiles, hit weight and slash FX all work the same way.
    [CreateAssetMenu(menuName = "Vela/Skill Config", fileName = "Skill")]
    public class SkillConfig : ScriptableObject
    {
        public string displayName = "Skill";
        [TextArea] public string description = "";
        public Sprite icon;
        public Color color = Color.white;
        public float cooldown = 4f;
        [Tooltip("Crit chance for this skill.")]
        [Range(0f, 1f)] public float critChance = 0.15f;
        public float critMultiplier = 1.75f;
        public AttackStep attack = new AttackStep { name = "Skill" };
    }
}

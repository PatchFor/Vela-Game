using UnityEngine;
using Vela.Core;
using Vela.Items;
using Vela.Visual;

namespace Vela.Gameplay
{
    /// Test keys for the sword animation A/B test (see docs/specs/sword-animation-paperdoll.md):
    ///  - F6 cycles the animation detail: Full → KeyPoses → Procedural (old single frame).
    ///  - F7 swaps the sword skin (short / long / broad). It uses the same animation, and the swap
    ///    works mid-swing.
    /// Sits on the player next to PaperDoll.
    [RequireComponent(typeof(PaperDoll))]
    public class AnimationTestbench : MonoBehaviour
    {
        [Tooltip("F7 cycles through these weapon looks. They all share the same sword animation.")]
        [SerializeField] private EquipmentVisual[] swordSkins = new EquipmentVisual[0];

        private PaperDoll doll;
        private int skinIndex;

        public void Configure(EquipmentVisual[] skins) => swordSkins = skins ?? new EquipmentVisual[0];

        private void Awake() => doll = GetComponent<PaperDoll>();

        private void Update()
        {
            if (doll == null) return;

            if (VelaInput.DebugAnimationDetailPressed)
            {
                if (doll.Rig == null || doll.Rig.animationSet == null)
                {
                    HudMessages.Show("No animation set on the rig (rebuild the scene)", new Color(1f, 0.6f, 0.5f));
                }
                else
                {
                    doll.Detail = Next(doll.Detail);
                    HudMessages.Show($"Animation: {Describe(doll.Detail)}", new Color(0.8f, 0.9f, 1f));
                }
            }

            if (VelaInput.DebugWeaponSkinPressed && swordSkins.Length > 0)
            {
                skinIndex = (skinIndex + 1) % swordSkins.Length;
                var skin = swordSkins[skinIndex];
                doll.SetEquipment(EquipmentSlot.Weapon, skin);
                HudMessages.Show($"Weapon look: {(skin != null ? skin.name.Replace("Visual", "") : "none")}", new Color(0.8f, 0.9f, 1f));
            }
        }

        private static AnimationDetail Next(AnimationDetail current) => current switch
        {
            AnimationDetail.Full => AnimationDetail.KeyPoses,
            AnimationDetail.KeyPoses => AnimationDetail.Procedural,
            _ => AnimationDetail.Full
        };

        public static string Describe(AnimationDetail detail) => detail switch
        {
            AnimationDetail.Full => "C · Full (every frame + smear)",
            AnimationDetail.KeyPoses => "B · Key poses (windup / impact / follow)",
            _ => "A · Procedural (single frame + squash)"
        };
    }
}

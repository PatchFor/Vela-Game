using System;
using UnityEngine;
using Vela.Items;

namespace Vela.Visual
{
    /// One sprite per facing. Side art faces right; left is mirrored.
    [Serializable]
    public class DirectionalSprites
    {
        public Sprite down;
        public Sprite up;
        public Sprite side;

        public Sprite Get(Direction4 direction) => direction switch
        {
            Direction4.Up => up != null ? up : down,
            Direction4.Side => side != null ? side : down,
            _ => down
        };

        public bool IsEmpty => down == null && up == null && side == null;
    }

    /// The look of one wearable piece (helmet, armor, gloves, boots, weapon). All sprites must
    /// use the same canvas size and bottom-center pivot as the character rig's body, so the
    /// layers line up without offsets.
    [CreateAssetMenu(menuName = "Vela/Equipment Visual", fileName = "EquipmentVisual")]
    public class EquipmentVisual : ScriptableObject
    {
        public EquipmentSlot slot = EquipmentSlot.Head;
        public DirectionalSprites sprites = new DirectionalSprites();

        [Tooltip("Multiplied into the sprites. Grey art + tint = cheap color variants (palette swap).")]
        public Color tint = Color.white;

        [Tooltip("Hide the hair layer while worn (full helmets).")]
        public bool hidesHair;
    }
}

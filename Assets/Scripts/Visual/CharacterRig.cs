using System;
using UnityEngine;
using Vela.Items;

namespace Vela.Visual
{
    /// Paper-doll layers, back to front order is decided per facing.
    public enum DollLayer
    {
        Body,
        Hair,
        Feet,
        Chest,
        Hands,
        Head,
        Weapon
    }

    /// Base body + hair for a layered (paper-doll) character, and the draw order of every
    /// layer in each facing. Example: facing Up (back to camera) the weapon goes behind the body.
    [CreateAssetMenu(menuName = "Vela/Character Rig", fileName = "CharacterRig")]
    public class CharacterRig : ScriptableObject
    {
        [Serializable]
        public class LayerOrder
        {
            [Tooltip("Back to front.")]
            public DollLayer[] order;
        }

        public DirectionalSprites body = new DirectionalSprites();
        public DirectionalSprites hair = new DirectionalSprites();
        public Color hairTint = Color.white;

        [Header("Draw order per facing (back → front)")]
        public LayerOrder down = new LayerOrder
        {
            order = new[] { DollLayer.Body, DollLayer.Feet, DollLayer.Chest, DollLayer.Hands, DollLayer.Hair, DollLayer.Head, DollLayer.Weapon }
        };

        public LayerOrder up = new LayerOrder
        {
            order = new[] { DollLayer.Weapon, DollLayer.Body, DollLayer.Feet, DollLayer.Chest, DollLayer.Hands, DollLayer.Hair, DollLayer.Head }
        };

        public LayerOrder side = new LayerOrder
        {
            order = new[] { DollLayer.Body, DollLayer.Feet, DollLayer.Chest, DollLayer.Hair, DollLayer.Head, DollLayer.Weapon, DollLayer.Hands }
        };

        public DollLayer[] OrderFor(Direction4 direction) => direction switch
        {
            Direction4.Up => up.order,
            Direction4.Side => side.order,
            _ => down.order
        };

        public static DollLayer LayerFor(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Head => DollLayer.Head,
            EquipmentSlot.Chest => DollLayer.Chest,
            EquipmentSlot.Hands => DollLayer.Hands,
            EquipmentSlot.Feet => DollLayer.Feet,
            EquipmentSlot.Weapon => DollLayer.Weapon,
            _ => DollLayer.Body
        };
    }
}

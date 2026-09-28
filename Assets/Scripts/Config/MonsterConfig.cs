using UnityEngine;

namespace Vela.Config
{
    [CreateAssetMenu(menuName = "Vela/Monster Config", fileName = "Monster")]
    public class MonsterConfig : EnemyConfigBase
    {
        public EnemyBehaviour behaviour = new EnemyBehaviour();

        [Header("Training dummy")]
        [Tooltip("Can't die; heals back to full after not being hit for a while.")]
        public bool immortal;

        public override EnemyBehaviour InitialBehaviour => behaviour;
    }
}

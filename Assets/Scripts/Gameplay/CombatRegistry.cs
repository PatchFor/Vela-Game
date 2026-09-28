using System.Collections.Generic;
using Vela.Enemies;
using Vela.Player;

namespace Vela.Gameplay
{
    /// Who's alive in the scene. Avoids FindObjectsByType calls every frame.
    public static class CombatRegistry
    {
        public static readonly List<EnemyBrain> Enemies = new List<EnemyBrain>();

        public static PlayerController Player { get; set; }

        public static void Clear()
        {
            Enemies.Clear();
            Player = null;
        }
    }
}

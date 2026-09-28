using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Vela.Core;
using Vela.Visual;

namespace Vela.Tests
{
    public class CombatRulesTests
    {
        [TestCase(8f, 12, HitWeight.Light)]
        [TestCase(15f, 12, HitWeight.Medium)]
        [TestCase(30f, 20, HitWeight.Heavy)]
        [TestCase(60f, 20, HitWeight.Finisher)]
        [TestCase(0f, 50, HitWeight.Finisher)]
        public void AutoHitWeight_FollowsStaggerAndDamage(float stagger, int damage, HitWeight expected)
        {
            Assert.AreEqual(expected, DamageInfo.Resolve(HitWeight.Auto, stagger, damage));
        }

        [Test]
        public void ExplicitHitWeight_IsKept()
        {
            Assert.AreEqual(HitWeight.Light, DamageInfo.Resolve(HitWeight.Light, 100f, 100));
        }

        [Test]
        public void Immortal_NeverDropsBelowOne()
        {
            var go = new GameObject("dummy");
            var health = go.AddComponent<Health>();
            health.Configure(10, Team.Enemy, 0f);
            health.Immortal = true;

            health.ApplyDamage(new DamageInfo { Amount = 999, SourceTeam = Team.Player });

            Assert.AreEqual(1, health.Current);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SameTeam_CannotHurt()
        {
            var go = new GameObject("ally");
            var health = go.AddComponent<Health>();
            health.Configure(10, Team.Enemy, 0f);

            Assert.IsFalse(health.ApplyDamage(new DamageInfo { Amount = 5, SourceTeam = Team.Enemy }));
            Assert.AreEqual(10, health.Current);
            Object.DestroyImmediate(go);
        }
    }

    public class PaperDollTests
    {
        [Test]
        public void EveryFacing_DrawsEveryLayerExactlyOnce()
        {
            var rig = ScriptableObject.CreateInstance<CharacterRig>();
            var all = System.Enum.GetValues(typeof(DollLayer)).Cast<DollLayer>().ToArray();

            foreach (Direction4 direction in System.Enum.GetValues(typeof(Direction4)))
            {
                var order = rig.OrderFor(direction);
                CollectionAssert.AreEquivalent(all, order, $"{direction} must list each layer once");
            }
        }

        [Test]
        public void FacingAway_WeaponIsBehindBody()
        {
            var rig = ScriptableObject.CreateInstance<CharacterRig>();
            var up = rig.OrderFor(Direction4.Up).ToList();
            Assert.Less(up.IndexOf(DollLayer.Weapon), up.IndexOf(DollLayer.Body));
        }

        [Test]
        public void FacingCamera_HelmetIsInFrontOfHair()
        {
            var rig = ScriptableObject.CreateInstance<CharacterRig>();
            var down = rig.OrderFor(Direction4.Down).ToList();
            Assert.Greater(down.IndexOf(DollLayer.Head), down.IndexOf(DollLayer.Hair));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Vela.FX;
using Vela.Gameplay;

namespace Vela.World
{
    /// A marked jump across a gap (water, a ledge), like Alabaster Dawn: stand near one end and
    /// dash toward the other end to leap across. Place two child points, A and B.
    /// While the player is near an end, a pulsing arrow on the ground shows where the jump goes.
    public class JumpLink : MonoBehaviour
    {
        private static readonly List<JumpLink> All = new List<JumpLink>();

        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [Tooltip("How close to an end the player must be for a dash to become a jump.")]
        [SerializeField] private float triggerRadius = 1.8f;
        [SerializeField] private bool bidirectional = true;
        [SerializeField] private Color hintColor = new Color(0.55f, 0.9f, 1f, 0.5f);

        private Telegraph hint;
        private bool hintFromA;

        public void Configure(Transform a, Transform b, float radius)
        {
            pointA = a;
            pointB = b;
            triggerRadius = radius;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable()
        {
            All.Remove(this);
            if (hint != null) hint.Release();
            hint = null;
        }

        /// A jump that starts near `position` and goes roughly along `direction`, or null.
        public static JumpLink Find(Vector3 position, Vector3 direction, float maxAngle, out Vector3 destination)
        {
            destination = position;
            direction.y = 0f;
            JumpLink best = null;
            var bestDistance = float.MaxValue;

            foreach (var link in All)
            {
                if (link == null || link.pointA == null || link.pointB == null) continue;

                if (link.Check(position, direction, maxAngle, link.pointA.position, link.pointB.position, ref bestDistance))
                {
                    best = link;
                    destination = link.pointB.position;
                }

                if (link.bidirectional &&
                    link.Check(position, direction, maxAngle, link.pointB.position, link.pointA.position, ref bestDistance))
                {
                    best = link;
                    destination = link.pointA.position;
                }
            }

            return best;
        }

        /// True if the player stands near an end of any link (for the HUD prompt).
        public static bool PlayerNearAny(Vector3 position)
        {
            foreach (var link in All)
            {
                if (link == null || link.pointA == null || link.pointB == null) continue;
                if (Flat(position - link.pointA.position).magnitude <= link.triggerRadius) return true;
                if (link.bidirectional && Flat(position - link.pointB.position).magnitude <= link.triggerRadius) return true;
            }
            return false;
        }

        private bool Check(Vector3 position, Vector3 direction, float maxAngle, Vector3 from, Vector3 to, ref float bestDistance)
        {
            var distance = Flat(position - from).magnitude;
            if (distance > triggerRadius || distance >= bestDistance) return false;
            if (direction.sqrMagnitude > 0.001f && Vector3.Angle(direction, Flat(to - from)) > maxAngle) return false;
            bestDistance = distance;
            return true;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private void Update()
        {
            var player = CombatRegistry.Player;
            if (player == null || pointA == null || pointB == null)
            {
                HideHint();
                return;
            }

            var p = player.transform.position;
            var nearA = Flat(p - pointA.position).magnitude <= triggerRadius * 1.3f;
            var nearB = bidirectional && Flat(p - pointB.position).magnitude <= triggerRadius * 1.3f;

            if (!nearA && !nearB || player.IsJumping)
            {
                HideHint();
                return;
            }

            var fromA = nearA;
            var from = fromA ? pointA.position : pointB.position;
            var to = fromA ? pointB.position : pointA.position;
            if (hint == null || hintFromA != fromA)
            {
                HideHint();
                hint = Telegraph.Create(TelegraphShape.Line, from, to - from, Flat(to - from).magnitude, 0.7f, hintColor);
                hintFromA = fromA;
            }

            hint.SetPose(from, to - from);
            hint.SetProgress(Mathf.Repeat(Time.time * 1.2f, 1f));
        }

        private void HideHint()
        {
            if (hint == null) return;
            hint.Release();
            hint = null;
        }

        private void OnDrawGizmos()
        {
            if (pointA == null || pointB == null) return;
            Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.9f);
            var previous = pointA.position;
            for (var i = 1; i <= 16; i++)
            {
                var t = i / 16f;
                var point = Vector3.Lerp(pointA.position, pointB.position, t) + Vector3.up * (4f * 1.8f * t * (1f - t));
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
            Gizmos.DrawWireSphere(pointA.position, triggerRadius);
            Gizmos.DrawWireSphere(pointB.position, triggerRadius);
        }
    }
}

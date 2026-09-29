using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vela.Visual;

namespace Vela.Tests
{
    /// Edge cases from docs/specs/sword-animation-paperdoll.md that don't need a rendered frame.
    public class DollAnimationTests
    {
        private const float Frame = 1f / 60f;

        // Slash: windup 2 (second held ×2) · smear 1 · impact 1 · follow 2 · recovery 2
        private static readonly AttackPart[] Slash =
        {
            AttackPart.Windup, AttackPart.Windup, AttackPart.Smear, AttackPart.Impact,
            AttackPart.Follow, AttackPart.Follow, AttackPart.Recovery, AttackPart.Recovery
        };
        private static readonly int[] SlashWeights = { 1, 2, 1, 1, 1, 1, 1, 1 };

        private static int At(float t, float windup = 0.05f, float active = 0.08f, float recovery = 0.2f,
            AnimationDetail detail = AnimationDetail.Full) =>
            PhaseTimeline.FrameAt(Slash, SlashWeights, t, windup, active, recovery, detail);

        [Test]
        public void ImpactFrameStartsExactlyWhenActiveStarts()
        {
            Assert.AreEqual(AttackPart.Impact, Slash[At(0.05f)]);
            Assert.AreEqual(AttackPart.Smear, Slash[At(0.05f - 0.001f)], "the frame just before damage is the smear");
        }

        [Test]
        public void ImpactStaysOnTimeWhenTheConfigIsRetuned()
        {
            foreach (var windup in new[] { 0.03f, 0.1f, 0.36f })
                Assert.AreEqual(AttackPart.Impact, Slash[At(windup, windup)], $"windup {windup}");
        }

        [Test]
        public void HeldFrameGetsItsWeightInTime()
        {
            // Windup 0.08 over weights 1 + 2 + 1 → 0.02 / 0.04 / 0.02.
            Assert.AreEqual(0, At(0.01f, 0.08f));
            Assert.AreEqual(1, At(0.03f, 0.08f));
            Assert.AreEqual(1, At(0.055f, 0.08f));
            Assert.AreEqual(2, At(0.07f, 0.08f));
        }

        [Test]
        public void FramesNeverGoBackwardsDuringAnAttack()
        {
            var previous = 0;
            for (var t = 0f; t < 0.4f; t += Frame)
            {
                var frame = At(t);
                Assert.GreaterOrEqual(frame, previous, $"t={t}");
                previous = frame;
            }
        }

        [Test]
        public void PastTheEndHoldsTheLastFrame()
        {
            Assert.AreEqual(Slash.Length - 1, At(10f));
        }

        [Test]
        public void ZeroWindupMovesSmearToTheStartOfActive()
        {
            var parts = new[] { AttackPart.Smear, AttackPart.Smear, AttackPart.Impact, AttackPart.Follow, AttackPart.Recovery };
            var weights = new[] { 1, 1, 1, 1, 1 };
            Assert.AreEqual(0, PhaseTimeline.FrameAt(parts, weights, 0f, 0f, 0.18f, 0.35f, AnimationDetail.Full));
            Assert.AreEqual(2, PhaseTimeline.FrameAt(parts, weights, 0.17f, 0f, 0.18f, 0.35f, AnimationDetail.Full));
        }

        [Test]
        public void KeyPosesShowOnePosePerPhase()
        {
            Assert.AreEqual(1, At(0.001f, detail: AnimationDetail.KeyPoses), "held anticipation pose, not the smear");
            Assert.AreEqual(1, At(0.049f, detail: AnimationDetail.KeyPoses));
            Assert.AreEqual(3, At(0.06f, detail: AnimationDetail.KeyPoses), "impact");
            Assert.AreEqual(4, At(0.14f, detail: AnimationDetail.KeyPoses), "first follow-through");
            Assert.AreEqual(4, At(0.3f, detail: AnimationDetail.KeyPoses));
        }

        [Test]
        public void LoopsWrapAndOneShotsHold()
        {
            Assert.AreEqual(1, PhaseTimeline.LoopFrame(4, 10f, 0.55f, true));
            Assert.AreEqual(2, PhaseTimeline.LoopFrame(4, 10f, 0.65f, true));
            Assert.AreEqual(3, PhaseTimeline.LoopFrame(4, 10f, 5f, false));
        }

        // ------------------------------------------------------------------ layers & swapping

        [Test]
        public void GarmentFrameFollowsTheBodyIndexWhateverIsWorn()
        {
            var a = Sheet("slash", Direction4.Side, 8, "vest");
            var b = Sheet("slash", Direction4.Side, 8, "plate");
            // The frame index comes from the timeline only, so swapping gear mid-swing keeps it.
            var index = At(0.06f);
            Assert.AreEqual($"vest{index}", a.Get("slash", Direction4.Side, index).name);
            Assert.AreEqual($"plate{index}", b.Get("slash", Direction4.Side, index).name);
        }

        [Test]
        public void EmptySlotOrMissingClipDrawsNothing()
        {
            var sheet = Sheet("slash", Direction4.Side, 8, "vest");
            Assert.IsNull(sheet.Get("thrust", Direction4.Side, 0), "garment without this clip");
            Assert.IsNull(sheet.Get("slash", Direction4.Side, 99), "index out of range");
            Assert.IsNull(new DollLayerSheet().Get("slash", Direction4.Side, 0), "nothing equipped");
        }

        [Test]
        public void MissingFacingFallsBackToDown()
        {
            var sheet = Sheet("idle", Direction4.Down, 2, "hat");
            Assert.AreEqual("hat1", sheet.Get("idle", Direction4.Up, 1).name);
        }

        // ------------------------------------------------------------------ validation

        [Test]
        public void ValidSetHasNoErrors()
        {
            var set = Set(Slash);
            Assert.IsEmpty(DollAnimationValidator.Validate(set, ("vest", Sheet("slash", Direction4.Side, 8, "vest"))));
        }

        [Test]
        public void LayerWithWrongFrameCountIsRejected()
        {
            var set = Set(Slash);
            var errors = DollAnimationValidator.Validate(set, ("vest", Sheet("slash", Direction4.Side, 7, "vest")));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("7 frames, body has 8", errors[0]);
        }

        [Test]
        public void AttackPartsOutOfOrderOrWithoutImpactAreRejected()
        {
            var backwards = Set(new[] { AttackPart.Windup, AttackPart.Impact, AttackPart.Smear, AttackPart.Recovery });
            Assert.IsTrue(DollAnimationValidator.Validate(backwards).Exists(e => e.Contains("must run in order")));

            var noImpact = Set(new[] { AttackPart.Windup, AttackPart.Smear, AttackPart.Recovery });
            Assert.IsTrue(DollAnimationValidator.Validate(noImpact).Exists(e => e.Contains("no Impact")));
        }

        // ------------------------------------------------------------------ anchors

        [Test]
        public void MirroringFlipsAnchorsAndAngles()
        {
            var right = AnchorMath.LocalPosition(new Vector2(8f, 20f), 16f, false);
            var left = AnchorMath.LocalPosition(new Vector2(8f, 20f), 16f, true);
            Assert.AreEqual(0.5f, right.x, 1e-5f);
            Assert.AreEqual(-0.5f, left.x, 1e-5f);
            Assert.AreEqual(1.25f, left.y, 1e-5f);

            var hand = new Vector2(0f, 0f);
            var upRight = new Vector2(1f, 1f);
            Assert.AreEqual(45f, AnchorMath.Angle(hand, upRight, false), 1e-3f);
            Assert.AreEqual(-45f, AnchorMath.Angle(hand, upRight, true), 1e-3f);
        }

        // ------------------------------------------------------------------ helpers

        private static DollLayerSheet Sheet(string clip, Direction4 direction, int count, string prefix)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                frames[i].name = prefix + i;
            }
            return new DollLayerSheet { clips = new[] { new DollLayerFrames { clip = clip, direction = direction, frames = frames } } };
        }

        private static DollAnimationSet Set(IReadOnlyList<AttackPart> parts)
        {
            var set = ScriptableObject.CreateInstance<DollAnimationSet>();
            var frames = new DollFrame[parts.Count];
            for (var i = 0; i < frames.Length; i++) frames[i] = new DollFrame { part = parts[i] };
            set.clips = new[] { new DollClip { name = "slash", direction = Direction4.Side, isAttack = true, loop = false, frames = frames } };
            set.body = Sheet("slash", Direction4.Side, parts.Count, "body");
            return set;
        }
    }
}

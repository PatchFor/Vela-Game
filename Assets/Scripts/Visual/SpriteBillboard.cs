using UnityEngine;
using Vela.Config;
using Vela.Gameplay;

namespace Vela.Visual
{
    /// Four-way facing relative to the camera. Left is Side mirrored.
    public enum Direction4
    {
        Down,
        Up,
        Side
    }

    public enum VisualState
    {
        Idle,
        Move,
        Attack,
        Hurt
    }

    /// 2.5D look: a flat sprite standing on a 3D floor, turned to face the locked camera,
    /// with a blob shadow and procedural bob/squash so single-frame art still feels alive.
    /// Put it on the character root (pivot at the feet); it builds its own children.
    public class SpriteBillboard : MonoBehaviour, Vela.Core.IHitPausable
    {
        [SerializeField] private CharacterVisual visual = new CharacterVisual();

        [Tooltip("0 = sprite stands straight up, 1 = sprite fully faces the camera.")]
        [Range(0f, 1f)] [SerializeField] private float tiltTowardCamera = 0.35f;

        private Transform spriteRoot;
        private SpriteRenderer spriteRenderer;
        private Transform shadow;
        private Material shadowMaterial;

        private VisualState state;
        private float stateTime;
        private float flashUntil;
        private Color flashColor = Color.white;
        private Color stateTint = Color.white;
        private Color baseTint = Color.white;
        private Color hurtTint = Color.white;
        private float hurtTintUntil;
        private float hurtTintDuration = 0.1f;
        private float trembleUntil;
        private float trembleAmount;
        private float trembleDuration = 0.1f;
        private bool blinking;
        private float blinkRate = 16f;
        private float scaleMultiplier = 1f;
        private bool facingLeft;
        private float fade = 1f;

        // Squash/stretch spring: punch it and it wobbles back to (1,1).
        private Vector2 squash = Vector2.one;
        private Vector2 squashVelocity;

        private float airHeight;
        private float pausedUntil;

        /// Hold the current pose (squash, animation frame) for a local hit-stop.
        public void HitPause(float seconds) => pausedUntil = Mathf.Max(pausedUntil, Time.unscaledTime + seconds);

        public SpriteRenderer Renderer => spriteRenderer;
        public CharacterVisual Visual => visual;
        public bool FacingLeft => facingLeft;
        public Transform SpriteRoot => spriteRoot;

        /// Facing relative to the camera (Down = toward the camera, Up = away).
        public Direction4 Direction { get; private set; } = Direction4.Down;

        /// Set by PaperDoll: returns the body sprite for a direction. When set, the sprite only
        /// mirrors in Side view, and Up/Down use their own art.
        public System.Func<Direction4, Sprite> DirectionalSprite { get; set; }

        /// Lifts the sprite (not the shadow) off the ground, e.g. during a jump.
        public void SetAirHeight(float height) => airHeight = Mathf.Max(0f, height);

        private void Awake()
        {
            EnsureBuilt();
        }

        public void Setup(CharacterVisual newVisual)
        {
            visual = newVisual ?? new CharacterVisual();
            EnsureBuilt();
            ApplySprite(visual.sprite);
            UpdateShadow();
        }

        public void SetState(VisualState next)
        {
            if (state == next) return;
            state = next;
            stateTime = 0f;
        }

        /// Faces the art left or right from a world-space direction, relative to the camera.
        public void SetFacing(Vector3 worldDirection)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var side = Vector3.Dot(worldDirection, cam.transform.right);
            var camForward = cam.transform.forward;
            camForward.y = 0f;
            var forward = Vector3.Dot(worldDirection, camForward.normalized);

            if (Mathf.Abs(side) > 0.15f) facingLeft = side < 0f;

            // Slight bias toward Side so diagonals read as profile, like most 4-way pixel games.
            if (Mathf.Abs(side) >= Mathf.Abs(forward) * 0.85f) Direction = Direction4.Side;
            else Direction = forward > 0f ? Direction4.Up : Direction4.Down;
        }

        public void Flash(Color color, float duration)
        {
            flashColor = color;
            flashUntil = Time.time + duration;
        }

        /// After a flash, fade from `tint` back to normal over `duration` (starts when the flash ends).
        public void HurtTint(Color tint, float duration)
        {
            hurtTint = tint;
            hurtTintDuration = Mathf.Max(0.01f, duration);
            hurtTintUntil = Mathf.Max(flashUntil, Time.time) + duration;
        }

        /// Sideways jitter in real time, so the victim visibly trembles during a freeze-frame.
        public void Tremble(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;
            trembleAmount = Mathf.Max(amount, Time.unscaledTime < trembleUntil ? trembleAmount : 0f);
            trembleDuration = duration;
            trembleUntil = Time.unscaledTime + duration;
        }

        /// Blink the sprite (i-frames after the player is hit).
        public void SetBlink(bool on, float rate)
        {
            blinking = on;
            blinkRate = rate;
        }

        /// Kick the squash spring: (1.3, 0.7) = squashed flat, (0.8, 1.25) = stretched tall.
        public void Punch(Vector2 scale)
        {
            squash = scale;
            squashVelocity = Vector2.zero;
        }

        /// Short-lived tint (attack windup pulse). Multiplied with the base tint.
        public void SetTint(Color tint) => stateTint = tint;

        /// Long-lived tint (boss phase color).
        public void SetBaseTint(Color tint) => baseTint = tint;

        public void SetScaleMultiplier(float scale) => scaleMultiplier = Mathf.Max(0.05f, scale);

        public void SetFade(float alpha) => fade = Mathf.Clamp01(alpha);

        private void EnsureBuilt()
        {
            if (spriteRoot != null) return;

            spriteRoot = transform.Find("Sprite");
            if (spriteRoot == null)
            {
                spriteRoot = new GameObject("Sprite").transform;
                spriteRoot.SetParent(transform, false);
            }

            spriteRenderer = spriteRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = spriteRoot.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;

            shadow = transform.Find("Shadow");
            if (shadow == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Shadow";
                DestroyCollider(quad);
                shadow = quad.transform;
                shadow.SetParent(transform, false);
            }

            var shadowRenderer = shadow.GetComponent<MeshRenderer>();
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
            shadowMaterial = new Material(VelaSettings.UnlitMaterial)
            {
                mainTexture = VelaSettings.Fx.softCircle,
                color = new Color(0f, 0f, 0f, 0.4f)
            };
            shadowRenderer.sharedMaterial = shadowMaterial;
            shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);

            ApplySprite(visual.sprite);
            UpdateShadow();
        }

        private static void DestroyCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        private void UpdateShadow()
        {
            if (shadow == null) return;
            var size = visual.shadowSize * scaleMultiplier;
            shadow.localPosition = new Vector3(0f, 0.03f, 0f);
            shadow.rotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.localScale = new Vector3(size, size * 0.6f, 1f);
        }

        private void ApplySprite(Sprite sprite)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == sprite) return;
            spriteRenderer.sprite = sprite;
        }

        private Sprite CurrentFrame()
        {
            var frames = state switch
            {
                VisualState.Move => visual.moveFrames,
                VisualState.Attack => visual.attackFrames,
                VisualState.Hurt => visual.hurtFrames,
                _ => visual.idleFrames
            };

            if (DirectionalSprite != null)
            {
                var directional = DirectionalSprite(Direction);
                if (directional != null) return directional;
            }

            if (frames == null || frames.Length == 0) frames = visual.idleFrames;
            if (frames == null || frames.Length == 0) return visual.sprite;

            var index = Mathf.FloorToInt(stateTime * Mathf.Max(0.01f, visual.framesPerSecond)) % frames.Length;
            return frames[index] != null ? frames[index] : visual.sprite;
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null) return;

            var paused = Time.unscaledTime < pausedUntil;
            var dt = paused ? 0f : Time.deltaTime;
            stateTime += dt;
            ApplySprite(CurrentFrame());

            // Squash spring back toward 1.
            const float stiffness = 320f;
            const float damping = 18f;
            var accel = (Vector2.one - squash) * stiffness - squashVelocity * damping;
            squashVelocity += accel * dt;
            squash += squashVelocity * dt;

            // Procedural motion for single-frame art.
            var bob = 0f;
            var lean = 0f;
            var breathe = 1f;
            switch (state)
            {
                case VisualState.Move:
                    bob = Mathf.Abs(Mathf.Sin(stateTime * 14f)) * visual.moveBob;
                    lean = visual.moveLean;
                    break;
                case VisualState.Idle:
                    breathe = 1f + Mathf.Sin(stateTime * 3f) * visual.idleBreath;
                    break;
            }

            var sprite = spriteRenderer.sprite;
            var baseScale = 1f;
            var feetOffset = 0f;
            if (sprite != null)
            {
                var bounds = sprite.bounds;
                baseScale = visual.worldHeight * scaleMultiplier / Mathf.Max(0.001f, bounds.size.y);
                feetOffset = -bounds.min.y * baseScale;
            }

            var cam = Camera.main;
            var camEuler = cam != null ? cam.transform.eulerAngles : Vector3.zero;
            var pitch = Mathf.DeltaAngle(0f, camEuler.x) * tiltTowardCamera;
            spriteRoot.rotation = Quaternion.Euler(pitch, camEuler.y, 0f);

            var leanSign = facingLeft ? 1f : -1f;
            spriteRoot.localRotation *= Quaternion.Euler(0f, 0f, lean * leanSign);

            var sx = baseScale * squash.x;
            var sy = baseScale * squash.y * breathe;
            spriteRoot.localScale = new Vector3(sx, sy, baseScale);
            spriteRoot.position = transform.position + spriteRoot.up * (feetOffset * squash.y * breathe)
                                  + Vector3.up * (bob + Hover() + airHeight) + TrembleOffset(cam);

            var mirror = DirectionalSprite == null || Direction == Direction4.Side;
            spriteRenderer.flipX = mirror && (visual.artFacesRight ? facingLeft : !facingLeft);

            var flashing = Time.time < flashUntil;
            var wanted = flashing ? VelaSettings.FlashMaterial : VelaSettings.UnlitMaterial;
            if (spriteRenderer.sharedMaterial != wanted) spriteRenderer.sharedMaterial = wanted;

            var color = visual.tint * baseTint * stateTint;
            if (flashing)
            {
                color = flashColor;
            }
            else if (Time.time < hurtTintUntil)
            {
                var t = 1f - (hurtTintUntil - Time.time) / hurtTintDuration;
                color = Color.Lerp(color * hurtTint, color, t * t);
            }

            color.a *= fade;
            if (blinking && Mathf.Repeat(Time.time * blinkRate, 1f) < 0.5f) color.a *= 0.25f;
            spriteRenderer.color = color;

            if (shadowMaterial != null)
            {
                if (shadowMaterial.mainTexture == null) shadowMaterial.mainTexture = VelaSettings.Fx.softCircle;
                shadowMaterial.color = new Color(0f, 0f, 0f, 0.4f * fade);
                UpdateShadow();
            }
        }

        private Vector3 TrembleOffset(Camera cam)
        {
            var now = Time.unscaledTime;
            if (now >= trembleUntil || cam == null) return Vector3.zero;

            var strength = (trembleUntil - now) / Mathf.Max(0.001f, trembleDuration);
            var side = Mathf.Sign(Mathf.Sin(now * 110f));
            return cam.transform.right * (side * trembleAmount * strength);
        }

        private float Hover()
        {
            if (visual.hoverHeight <= 0f) return 0f;
            return visual.hoverHeight + Mathf.Sin(Time.time * 5f + GetInstanceID()) * 0.12f;
        }

        private void OnDestroy()
        {
            if (shadowMaterial != null) Destroy(shadowMaterial);
        }
    }
}

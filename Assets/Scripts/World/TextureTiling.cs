using UnityEngine;

namespace Vela.World
{
    /// Tiles a shared material's texture by the object's size, so one brick or grass
    /// material works on every wall and floor without stretching. Uses a property block,
    /// so the material asset itself is untouched.
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class TextureTiling : MonoBehaviour
    {
        public enum Mode
        {
            /// Tiles by width (the longer of X/Z) and height. For walls, pillars, blocks.
            Wall,

            /// Tiles by X and Z. For floors, paths and planes.
            Floor
        }

        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private Mode mode = Mode.Wall;
        [Tooltip("World units covered by one texture repeat.")]
        [SerializeField] private float tileSize = 2f;
        [Tooltip("Mesh size at scale 1 (10 for Unity's Plane, 1 for Cube/Quad).")]
        [SerializeField] private float meshSize = 1f;

        public void Configure(Mode newMode, float newTileSize, float newMeshSize)
        {
            mode = newMode;
            tileSize = newTileSize;
            meshSize = newMeshSize;
            Apply();
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        private void Apply()
        {
            var target = GetComponent<Renderer>();
            if (target == null) return;

            var s = transform.lossyScale * meshSize;
            var tile = Mathf.Max(0.01f, tileSize);
            var repeat = mode == Mode.Floor
                ? new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z)) / tile
                : new Vector2(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)), Mathf.Abs(s.y)) / tile;

            var block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            var st = new Vector4(Mathf.Max(0.01f, repeat.x), Mathf.Max(0.01f, repeat.y), 0f, 0f);
            block.SetVector(MainTexSt, st);
            block.SetVector(BaseMapSt, st);
            target.SetPropertyBlock(block);
        }
    }
}

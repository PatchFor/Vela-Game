using UnityEngine;
using Vela.Gameplay;

namespace Vela.FX
{
    public enum TelegraphShape
    {
        Circle,
        Sector,
        Line
    }

    /// Ground warning for an incoming attack: a faint area plus a fill that grows with the
    /// windup. When the fill reaches the edge, the attack lands.
    public class Telegraph : MonoBehaviour
    {
        private MeshRenderer areaRenderer;
        private MeshRenderer fillRenderer;
        private MeshRenderer edgeRenderer;
        private MaterialPropertyBlock block;

        private TelegraphShape shape;
        private float size;
        private float widthOrArc;
        private Color color;

        /// `size` = radius (circle, sector) or length (line). `widthOrArc` = arc degrees
        /// (sector) or width (line).
        public static Telegraph Create(TelegraphShape shape, Vector3 position, Vector3 direction, float size,
            float widthOrArc, Color color)
        {
            var go = new GameObject($"Telegraph_{shape}");
            var telegraph = go.AddComponent<Telegraph>();
            telegraph.Build(shape, size, widthOrArc, color);
            telegraph.SetPose(position, direction);
            telegraph.SetProgress(0f);

            if (!VelaSettings.Feel.showTelegraphs) go.SetActive(false);
            return telegraph;
        }

        private void Build(TelegraphShape newShape, float newSize, float newWidthOrArc, Color newColor)
        {
            shape = newShape;
            size = Mathf.Max(0.05f, newSize);
            widthOrArc = newWidthOrArc;
            color = newColor;
            block = new MaterialPropertyBlock();

            var mesh = shape switch
            {
                TelegraphShape.Circle => ProceduralMeshes.Disc,
                TelegraphShape.Sector => ProceduralMeshes.Sector(widthOrArc),
                _ => ProceduralMeshes.Rect
            };

            areaRenderer = CreatePart("Area", mesh, 0.05f);
            fillRenderer = CreatePart("Fill", mesh, 0.06f);
            if (shape == TelegraphShape.Circle) edgeRenderer = CreatePart("Edge", ProceduralMeshes.Ring, 0.07f);

            var baseScale = shape == TelegraphShape.Line
                ? new Vector3(widthOrArc, 1f, size)
                : new Vector3(size, 1f, size);
            areaRenderer.transform.localScale = baseScale;
            if (edgeRenderer != null) edgeRenderer.transform.localScale = baseScale;
        }

        private MeshRenderer CreatePart(string partName, Mesh mesh, float height)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(0f, height, 0f);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = VelaSettings.UnlitMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public void SetPose(Vector3 position, Vector3 direction)
        {
            direction.y = 0f;
            var rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : transform.rotation;
            transform.SetPositionAndRotation(new Vector3(position.x, position.y, position.z), rotation);
        }

        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);

            fillRenderer.transform.localScale = shape == TelegraphShape.Line
                ? new Vector3(widthOrArc, 1f, size * t)
                : new Vector3(size * t, 1f, size * t);

            var area = color;
            area.a *= 0.3f;
            FxManager.SetColor(areaRenderer, block, area);

            var fill = color;
            fill.a *= Mathf.Lerp(0.35f, 0.75f, t);
            FxManager.SetColor(fillRenderer, block, fill);

            if (edgeRenderer != null)
            {
                var edge = color;
                edge.a = Mathf.Min(1f, color.a * 1.6f);
                FxManager.SetColor(edgeRenderer, block, edge);
            }
        }

        public void Release()
        {
            if (this != null) Destroy(gameObject);
        }
    }
}

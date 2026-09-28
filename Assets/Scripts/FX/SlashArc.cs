using System.Collections.Generic;
using UnityEngine;

namespace Vela.FX
{
    /// A crescent that sweeps across the attack arc and fades. The mesh is rebuilt each frame
    /// (a few dozen vertices), with a bright leading edge and a thin fading tail.
    public class SlashArc : MonoBehaviour
    {
        private const int Segments = 20;

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();

        private float radius;
        private float width;
        private float arc;
        private float duration;
        private float age;
        private bool reverse;
        private Color color;

        public void Play(float newRadius, float newWidth, float arcDegrees, Color newColor, float newDuration, bool reversed)
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "Slash" };
                mesh.MarkDynamic();
                gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.sharedMaterial = FxManager.SlashMaterial;
            }

            radius = newRadius;
            width = Mathf.Min(newWidth, newRadius * 0.95f);
            arc = Mathf.Clamp(arcDegrees, 10f, 360f);
            color = newColor;
            duration = Mathf.Max(0.02f, newDuration);
            reverse = reversed;
            age = 0f;
            gameObject.SetActive(true);
            Rebuild();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= duration)
            {
                gameObject.SetActive(false);
                return;
            }

            Rebuild();
        }

        private void Rebuild()
        {
            var t = Mathf.Clamp01(age / duration);
            var headT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t * 1.8f), 3f);
            var tailT = Mathf.Clamp01((t - 0.2f) / 0.8f);
            tailT *= tailT;

            var half = arc * 0.5f;
            var sign = reverse ? -1f : 1f;
            var alpha = 1f - t * t;

            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            for (var i = 0; i <= Segments; i++)
            {
                var s = (float)i / Segments;
                var along = Mathf.Lerp(tailT, headT, s);
                var angle = Mathf.Deg2Rad * Mathf.Lerp(-half, half, along) * sign;
                var dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

                // Thick at the head, thin at the tail.
                var thickness = width * Mathf.Lerp(0.15f, 1f, s);
                vertices.Add(dir * (radius - thickness));
                vertices.Add(dir * radius);

                var c = color;
                c.a *= alpha * Mathf.Lerp(0f, 1f, s);
                colors.Add(c * new Color(1f, 1f, 1f, 0.6f));
                colors.Add(c);

                if (i == Segments) continue;
                var b = i * 2;
                triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 3);
                triangles.Add(b); triangles.Add(b + 3); triangles.Add(b + 2);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}

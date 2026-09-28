using System.Collections.Generic;
using UnityEngine;

namespace Vela.FX
{
    /// Flat meshes lying on the XZ plane (unit size), used for telegraphs, rings and slashes.
    /// The FX materials are double-sided, so winding doesn't matter.
    public static class ProceduralMeshes
    {
        private static Mesh disc;
        private static Mesh ring;
        private static Mesh rect;
        private static readonly Dictionary<int, Mesh> Sectors = new Dictionary<int, Mesh>();

        /// Radius-1 disc.
        public static Mesh Disc => disc != null ? disc : disc = BuildSector(360f, 0f, 1f, 48, "Disc");

        /// Radius-1 ring outline, 12% thick.
        public static Mesh Ring => ring != null ? ring : ring = BuildSector(360f, 0.88f, 1f, 48, "Ring");

        /// Rectangle from z=0 to z=1, x from -0.5 to 0.5 (so it can grow forward).
        public static Mesh Rect
        {
            get
            {
                if (rect != null) return rect;

                rect = new Mesh { name = "Rect" };
                rect.vertices = new[]
                {
                    new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
                    new Vector3(0.5f, 0f, 1f), new Vector3(-0.5f, 0f, 1f)
                };
                rect.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                rect.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                rect.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                rect.RecalculateBounds();
                return rect;
            }
        }

        /// Radius-1 pie slice centered on +Z.
        public static Mesh Sector(float arcDegrees)
        {
            var key = Mathf.RoundToInt(Mathf.Clamp(arcDegrees, 1f, 360f));
            if (Sectors.TryGetValue(key, out var mesh) && mesh != null) return mesh;

            mesh = BuildSector(key, 0f, 1f, Mathf.Max(8, key / 6), $"Sector{key}");
            Sectors[key] = mesh;
            return mesh;
        }

        private static Mesh BuildSector(float arcDegrees, float inner, float outer, int segments, string name)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var half = arcDegrees * 0.5f;

            for (var i = 0; i <= segments; i++)
            {
                var t = (float)i / segments;
                var angle = Mathf.Deg2Rad * Mathf.Lerp(-half, half, t);
                var dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                vertices.Add(dir * inner);
                vertices.Add(dir * outer);
                uvs.Add(new Vector2(t, 0f));
                uvs.Add(new Vector2(t, 1f));
                colors.Add(Color.white);
                colors.Add(Color.white);

                if (i == segments) continue;

                var b = i * 2;
                triangles.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 });
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

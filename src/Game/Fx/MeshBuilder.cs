using System;
using System.Collections.Generic;
using Godot;

namespace OctoShoots.Game.Fx;

/// <summary>Collects triangles with normals and vertex colours and turns them into an ArrayMesh.</summary>
public sealed class MeshBuilder
{
    readonly List<Vector3> _positions = new();
    readonly List<Vector3> _normals = new();
    readonly List<Color> _colors = new();
    readonly List<int> _indices = new();

    public int Add(Vector3 position, Vector3 normal, Color color)
    {
        _positions.Add(position);
        _normals.Add(normal);
        _colors.Add(color);
        return _positions.Count - 1;
    }

    /// <summary>Adds a triangle, flipping it if needed so it faces the way its normals point (Godot: clockwise = front).</summary>
    public void Tri(int a, int b, int c)
    {
        Vector3 geometric = (_positions[b] - _positions[a]).Cross(_positions[c] - _positions[a]);
        Vector3 wanted = _normals[a] + _normals[b] + _normals[c];
        if (geometric.Dot(wanted) > 0f) (b, c) = (c, b);
        _indices.Add(a);
        _indices.Add(b);
        _indices.Add(c);
    }

    public void Quad(int a, int b, int c, int d)
    {
        Tri(a, b, c);
        Tri(a, c, d);
    }

    /// <summary>A tapered tube along a path. Colour per ring comes from <paramref name="color"/> (ring index → colour).</summary>
    public void Tube(IReadOnlyList<Vector3> path, Func<int, float> radius, int sides, Func<int, Color> color)
    {
        int rings = path.Count;
        var first = new int[rings][];
        for (int i = 0; i < rings; i++)
        {
            Vector3 tangent = (path[Math.Min(i + 1, rings - 1)] - path[Math.Max(i - 1, 0)]).Normalized();
            Vector3 n = tangent.Cross(Mathf.Abs(tangent.Y) > 0.95f ? Vector3.Right : Vector3.Up).Normalized();
            Vector3 b = tangent.Cross(n).Normalized();
            first[i] = new int[sides];
            for (int k = 0; k < sides; k++)
            {
                float a = k / (float)sides * Mathf.Tau;
                Vector3 outward = n * Mathf.Cos(a) + b * Mathf.Sin(a);
                first[i][k] = Add(path[i] + outward * radius(i), outward, color(i));
            }
        }
        for (int i = 0; i + 1 < rings; i++)
        for (int k = 0; k < sides; k++)
        {
            int k1 = (k + 1) % sides;
            Quad(first[i][k], first[i][k1], first[i + 1][k1], first[i + 1][k]);
        }
    }

    public void Sphere(Vector3 center, float radius, Color color, int segments = 8)
    {
        int rings = segments / 2 + 1;
        var ids = new int[rings + 1][];
        for (int r = 0; r <= rings; r++)
        {
            float v = r / (float)rings * Mathf.Pi;
            ids[r] = new int[segments];
            for (int s = 0; s < segments; s++)
            {
                float u = s / (float)segments * Mathf.Tau;
                var n = new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u));
                ids[r][s] = Add(center + n * radius, n, color);
            }
        }
        for (int r = 0; r < rings; r++)
        for (int s = 0; s < segments; s++)
            Quad(ids[r][s], ids[r][(s + 1) % segments], ids[r + 1][(s + 1) % segments], ids[r + 1][s]);
    }

    public ArrayMesh Build()
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = _positions.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = _normals.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = _colors.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = _indices.ToArray();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}

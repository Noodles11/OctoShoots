using System;
using System.Collections.Generic;
using Godot;

namespace OctoShoots.Game.Player;

/// <summary>
/// First-person tentacles (§3.1), procedural tubes rebuilt every frame and drawn on top of the world
/// so they never clip into rock.
/// The <b>throwing tentacle</b> is the only arm in view: it reaches in from the bottom right and
/// points at the crosshair like a gun, rolled so its suckers face into the screen. Each sucker holds
/// a bubble; throws take them from the tip end and new ones swell back from the base end.
/// The <b>inventory tentacle</b> appears only while reviewing: it rises along the left of the view
/// with item pearls on its suckers turned toward the camera.
/// </summary>
public partial class Viewmodel : Node3D
{
    const int Segments = 18;
    const int Sides = 8;

    /// <summary>Suckers drawn on the throwing tentacle (bubble capacity is capped to this).</summary>
    public const int BubbleSlots = 12;

    /// <summary>Pearls the inventory tentacle can hold; any more are not drawn.</summary>
    public const int SuckerSlots = 8;

    /// <summary>Render layer of the arms; the camera glow and shot lights skip it so the arms don't blow out.</summary>
    public const uint RenderLayer = 1u << 1;

    static readonly Color BaseColor = new(0.85f, 0.32f, 0.16f);
    static readonly Color TipColor = new(1f, 0.62f, 0.45f);

    ImmediateMesh _mesh = null!;
    StandardMaterial3D _material = null!;
    readonly MeshInstance3D[] _gunSuckers = new MeshInstance3D[BubbleSlots];
    readonly MeshInstance3D[] _bubbles = new MeshInstance3D[BubbleSlots];
    readonly MeshInstance3D[] _invSuckers = new MeshInstance3D[SuckerSlots];
    readonly MeshInstance3D[] _pearls = new MeshInstance3D[SuckerSlots];
    int _pearlCount;
    int _bubbleCount, _capacity = 8;
    float _regrow;

    float _time;
    float _throw, _throwVel;
    float _whip, _whipVel;
    float _hurt;
    float _swimPhase, _swim;
    bool _reviewing;
    float _review, _reviewVel;

    /// <summary>Highlighted pearl while reviewing.</summary>
    public int Selected { get; set; }

    public int PearlCount => _pearlCount;

    public override void _Ready()
    {
        _material = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            Roughness = 0.45f,
            RimEnabled = true,
            Rim = 0.7f,
            RimTint = 0.3f,
            EmissionEnabled = true,
            Emission = new Color(1f, 0.45f, 0.2f),
            EmissionEnergyMultiplier = 0.45f,
            NoDepthTest = true,
            RenderPriority = 10,
        };
        _mesh = new ImmediateMesh();
        AddChild(new MeshInstance3D { Mesh = _mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Layers = RenderLayer });

        var suckerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.72f, 0.62f),
            Roughness = 0.35f,
            RimEnabled = true,
            Rim = 0.5f,
            EmissionEnabled = true,
            Emission = new Color(1f, 0.55f, 0.4f),
            EmissionEnergyMultiplier = 0.25f,
            NoDepthTest = true,
            RenderPriority = 11,
        };
        var bubbleMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/bubble_overlay.gdshader"), RenderPriority = 12 };
        bubbleMaterial.SetShaderParameter("wobble", 0.4f);
        var suckerMesh = new CylinderMesh { TopRadius = 0.85f, BottomRadius = 1f, Height = 0.35f, RadialSegments = 12, Rings = 1 };
        var sphere = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 16, Rings = 8 };

        MeshInstance3D Part(Mesh mesh, Material? material, bool visible = true)
        {
            var m = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Layers = RenderLayer, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = visible };
            AddChild(m);
            return m;
        }

        for (int i = 0; i < BubbleSlots; i++)
        {
            _gunSuckers[i] = Part(suckerMesh, suckerMaterial);
            _bubbles[i] = Part(sphere, bubbleMaterial);
        }
        for (int i = 0; i < SuckerSlots; i++)
        {
            _invSuckers[i] = Part(suckerMesh, suckerMaterial, false);
            _pearls[i] = Part(sphere, null, false);
        }
    }

    /// <summary>A swim stroke ripples down the arm.</summary>
    public void OnStroke() => _swim = 1f;

    /// <summary>The dash whips the arm back.</summary>
    public void OnDash() => _whipVel += 14f;

    /// <summary>A throw: the tip lunges toward the crosshair.</summary>
    public void OnThrow() => _throwVel += 22f;

    public void OnHurt() => _hurt = 1f;

    public void SetReviewing(bool reviewing) => _reviewing = reviewing;

    /// <summary>How many bubbles sit on the suckers, how many suckers there are, and how far the next one has grown (0–1).</summary>
    public void SetBubbles(int bubbles, int capacity, float regrow)
    {
        _capacity = Math.Clamp(capacity, 1, BubbleSlots);
        _bubbleCount = Math.Clamp(bubbles, 0, _capacity);
        _regrow = Mathf.Clamp(regrow, 0f, 1f);
    }

    /// <summary>Pearl materials in pickup order; only the first <see cref="SuckerSlots"/> are drawn.</summary>
    public void SetPearls(IReadOnlyList<Material> pearls)
    {
        _pearlCount = Math.Min(pearls.Count, SuckerSlots);
        for (int i = 0; i < SuckerSlots; i++)
            if (i < _pearlCount) _pearls[i].MaterialOverride = pearls[i];
        Selected = Math.Clamp(Selected, 0, Math.Max(0, _pearlCount - 1));
    }

    /// <summary>World positions of the held pearls (for labels while reviewing).</summary>
    public Vector3 PearlGlobalPosition(int i) => _pearls[i].GlobalPosition;

    /// <summary>speed01: swim speed relative to base speed.</summary>
    public void Tick(float dt, float speed01)
    {
        _time += dt;
        _swimPhase += dt * (2f + speed01 * 6f);
        _swim = Mathf.MoveToward(_swim, Mathf.Min(speed01, 1f) * 0.6f, dt * 2f);
        _hurt = Mathf.MoveToward(_hurt, 0f, dt * 3f);
        Spring(ref _throw, ref _throwVel, 0f, 260f, 20f, dt);
        Spring(ref _whip, ref _whipVel, 0f, 70f, 11f, dt);
        Spring(ref _review, ref _reviewVel, _reviewing ? 1f : 0f, 120f, 16f, dt);
        Rebuild();
    }

    /// <summary>Damped spring, substepped at 240 Hz so stiff springs stay stable at any frame rate.</summary>
    static void Spring(ref float x, ref float v, float target, float stiffness, float damping, float dt)
    {
        const float maxStep = 1f / 240f;
        while (dt > 0f)
        {
            float h = MathF.Min(dt, maxStep);
            v += ((target - x) * stiffness - v * damping) * h;
            x += v * h;
            dt -= h;
        }
    }

    // ───────────────────────── poses ─────────────────────────

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    /// <summary>The throwing arm: in from the bottom right, its last stretch pointing at the crosshair.</summary>
    Vector3 GunPose(float s, float review)
    {
        Vector3 lunge = new Vector3(-0.015f, 0.015f, -0.09f) * _throw;
        Vector3 whip = new Vector3(0.04f, -0.08f, 0.14f) * _whip;
        Vector3 shake = new Vector3(Mathf.Sin(_time * 53f), Mathf.Sin(_time * 47f + 1f), 0f) * 0.012f * _hurt;
        Vector3 aside = new Vector3(0.06f, -0.1f, 0.05f) * review;

        var a = new Vector3(0.38f, -0.46f, -0.08f);
        var b = new Vector3(0.33f, -0.3f, -0.3f) + whip * 0.5f;
        var c = new Vector3(0.15f, -0.16f, -0.42f) + lunge * 0.6f + whip;
        var d = new Vector3(0.105f, -0.125f, -0.66f) + lunge + whip;
        Vector3 p = Bezier(a, b, c, d, s);

        // Gentle idle sway and a ripple running down the arm while swimming.
        float sway = Mathf.Sin(_time * 1.3f + s * 2f) * 0.006f * s;
        float ripple = Mathf.Sin(s * 7f - _swimPhase) * 0.012f * _swim * s;
        return p + new Vector3(sway, ripple + sway * 0.5f, 0f) + shake * s + aside;
    }

    /// <summary>The inventory arm held up along the left of the view, tip curling inward.</summary>
    Vector3 ReviewPose(float s)
    {
        float bow = Mathf.Sin(s * Mathf.Pi);
        float tip = Mathf.Max(0f, s - 0.82f) / 0.18f;
        float x = Mathf.Lerp(-0.31f, -0.25f, s) - 0.025f * bow + 0.05f * tip * tip + Mathf.Sin(_time * 1.1f + s * 3f) * 0.003f;
        float y = -0.42f + 0.62f * s - 0.03f * tip * tip;
        float z = -0.42f - 0.03f * bow;
        return new Vector3(x, y, z);
    }

    static float GunRadius(float s) => 0.042f * Mathf.Pow(1f - s, 0.7f) + 0.004f;

    static float InventoryRadius(float s) => 0.03f * Mathf.Pow(1f - s, 0.6f) + 0.0015f;

    // ───────────────────────── mesh ─────────────────────────

    void Rebuild()
    {
        float review = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(_review, 0f, 1f));
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles, _material);

        var gun = new Vector3[Segments + 1];
        for (int i = 0; i <= Segments; i++) gun[i] = GunPose(i / (float)Segments, review);
        Tube(gun, GunRadius);
        PlaceBubbles(gun);

        bool showInventory = review > 0.01f;
        if (showInventory)
        {
            // Rises from below the view into the review pose.
            var inv = new Vector3[Segments + 1];
            Vector3 below = new(0f, -0.65f * (1f - review), 0.1f * (1f - review));
            for (int i = 0; i <= Segments; i++) inv[i] = ReviewPose(i / (float)Segments) + below;
            Tube(inv, InventoryRadius);
            PlacePearls(inv, review);
        }
        for (int i = 0; i < SuckerSlots; i++)
        {
            _invSuckers[i].Visible = showInventory;
            _pearls[i].Visible = showInventory && i < _pearlCount;
        }
        _mesh.SurfaceEnd();
    }

    void Tube(Vector3[] points, Func<float, float> radiusAt)
    {
        var ring = new Vector3[Segments + 1][];
        var colors = new Color[Segments + 1];
        for (int i = 0; i <= Segments; i++)
        {
            float s = i / (float)Segments;
            float radius = radiusAt(s);
            Vector3 tangent = (points[Mathf.Min(i + 1, Segments)] - points[Mathf.Max(i - 1, 0)]).Normalized();
            Vector3 normal = tangent.Cross(Vector3.Back).Normalized();
            if (normal.LengthSquared() < 1e-4f) normal = Vector3.Up;
            Vector3 binormal = tangent.Cross(normal).Normalized();
            ring[i] = new Vector3[Sides];
            for (int k = 0; k < Sides; k++)
            {
                float a = k / (float)Sides * Mathf.Tau;
                ring[i][k] = points[i] + normal * Mathf.Cos(a) * radius + binormal * Mathf.Sin(a) * radius;
            }
            colors[i] = BaseColor.Lerp(TipColor, s * s);
        }
        for (int i = 0; i < Segments; i++)
        for (int k = 0; k < Sides; k++)
        {
            int k1 = (k + 1) % Sides;
            Quad(ring[i][k], ring[i][k1], ring[i + 1][k1], ring[i + 1][k], points[i], points[i + 1], colors[i], colors[i + 1]);
        }
    }

    /// <summary>Point and tangent at s along a polyline of Segments+1 points.</summary>
    static (Vector3 P, Vector3 T) Along(Vector3[] points, float s)
    {
        float f = Mathf.Clamp(s, 0f, 1f) * Segments;
        int i0 = Math.Min((int)f, Segments - 1);
        return (points[i0].Lerp(points[i0 + 1], f - i0), (points[i0 + 1] - points[i0]).Normalized());
    }

    static Transform3D Oriented(Vector3 at, Vector3 n, Vector3 tangent, float size)
    {
        Vector3 x = tangent.Cross(n).Normalized();
        return new Transform3D(new Basis(x, n, x.Cross(n)).Scaled(new Vector3(size, size, size)), at);
    }

    /// <summary>Suckers along the visible part of the throwing arm, rolled toward the screen centre and the eye.</summary>
    void PlaceBubbles(Vector3[] gun)
    {
        for (int i = 0; i < BubbleSlots; i++)
        {
            bool used = i < _capacity;
            _gunSuckers[i].Visible = used;
            _bubbles[i].Visible = false;
            if (!used) continue;

            // Base end (i = 0) to tip end; the tip-most bubbles are thrown first.
            float s = _capacity == 1 ? 0.8f : Mathf.Lerp(0.47f, 0.93f, i / (float)(_capacity - 1));
            var (p, tangent) = Along(gun, s);
            Vector3 toEye = (Vector3.Zero - p).Normalized();
            Vector3 n = (toEye + new Vector3(-0.9f, 0.5f, 0f)).Normalized();
            n = (n - tangent * n.Dot(tangent)).Normalized();

            float radius = GunRadius(s);
            float suckerSize = radius * 0.5f;
            Vector3 at = p + n * radius * 0.85f;
            _gunSuckers[i].Transform = Oriented(at, n, tangent, suckerSize);

            float grow = i < _bubbleCount ? 1f : i == _bubbleCount ? _regrow : 0f;
            if (grow <= 0.02f) continue;
            float bubbleSize = suckerSize * 1.05f * Mathf.Sqrt(grow);
            _bubbles[i].Visible = true;
            _bubbles[i].Transform = new Transform3D(Basis.Identity.Scaled(new Vector3(bubbleSize, bubbleSize, bubbleSize)), at + n * bubbleSize * 0.85f);
        }
    }

    /// <summary>Suckers on the inventory arm, each holding a pearl, turned toward the camera.</summary>
    void PlacePearls(Vector3[] inv, float review)
    {
        for (int i = 0; i < SuckerSlots; i++)
        {
            float s = 0.3f + 0.083f * i;
            var (p, tangent) = Along(inv, s);
            Vector3 n = new Vector3(0.4f, 0f, 1f).Normalized();
            n = (n - tangent * n.Dot(tangent)).Normalized();

            float radius = InventoryRadius(s);
            float suckerSize = radius * 0.55f;
            Vector3 at = p + n * radius * 0.85f;
            _invSuckers[i].Transform = Oriented(at, n, tangent, suckerSize);

            bool selected = review > 0.5f && i == Selected;
            float pearlSize = suckerSize * 1.1f * (selected ? 1.35f : 1f);
            _pearls[i].Transform = new Transform3D(Basis.Identity.Scaled(new Vector3(pearlSize, pearlSize, pearlSize)), at + n * pearlSize * 0.8f);
            if (_pearls[i].MaterialOverride is ShaderMaterial pearl)
                pearl.SetShaderParameter("highlight", selected ? 0.5f + 0.5f * Mathf.Sin(_time * 6f) : 0f);
        }
    }

    void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 ca, Vector3 cc, Color colA, Color colC)
    {
        Vertex(a, ca, colA);
        Vertex(c, cc, colC);
        Vertex(b, ca, colA);
        Vertex(a, ca, colA);
        Vertex(d, cc, colC);
        Vertex(c, cc, colC);
    }

    void Vertex(Vector3 p, Vector3 center, Color color)
    {
        _mesh.SurfaceSetNormal((p - center).Normalized());
        _mesh.SurfaceSetColor(color);
        _mesh.SurfaceAddVertex(p);
    }
}

using System.Collections.Generic;
using System.Linq;
using Godot;
using OctoShoots.Core.Items;
using OctoShoots.Core.Loot;
using OctoShoots.Core.Sim;
using OctoShoots.Game.Util;

namespace OctoShoots.Game.Fx;

/// <summary>
/// Pearl shells that open as Clementine approaches and show a floating description, wooden
/// treasure chests whose lids fly open, and the small pickups.
/// </summary>
public partial class LootViews : Node3D
{
    sealed class ShellView
    {
        public required Node3D Root;
        public required Node3D Lid;
        public required MeshInstance3D Pearl;
        public required OmniLight3D Light;
        public required Label3D Label;
        public float Open;
        public string? ShownItem;
    }

    sealed class ChestView
    {
        public required Node3D Root;
        public required Node3D Lid;
        public float Open;
    }

    readonly Dictionary<int, ShellView> _shells = new();
    readonly Dictionary<int, ChestView> _chests = new();
    readonly Dictionary<int, (Node3D Node, PickupKind Kind)> _pickups = new();
    ItemCatalog _catalog = null!;
    StandardMaterial3D _shellOuter = null!, _shellInner = null!, _shopShell = null!, _wood = null!, _brass = null!;
    float _time;

    public void Init(ItemCatalog catalog) => _catalog = catalog;

    public override void _Ready()
    {
        _shellOuter = new StandardMaterial3D { AlbedoColor = new Color(0.95f, 0.78f, 0.75f), Roughness = 0.55f, RimEnabled = true, Rim = 0.5f };
        _shellInner = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.92f, 0.95f), Roughness = 0.2f, Metallic = 0.2f, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        _shopShell = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.82f, 0.4f), Roughness = 0.35f, Metallic = 0.4f, RimEnabled = true };
        _wood = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.28f, 0.14f), Roughness = 0.8f };
        _brass = new StandardMaterial3D { AlbedoColor = new Color(0.85f, 0.65f, 0.25f), Metallic = 0.7f, Roughness = 0.3f };
    }

    public void Clear()
    {
        foreach (var v in _shells.Values) v.Root.QueueFree();
        foreach (var v in _chests.Values) v.Root.QueueFree();
        foreach (var v in _pickups.Values) v.Node.QueueFree();
        _shells.Clear();
        _chests.Clear();
        _pickups.Clear();
    }

    public void Sync(World world, float alpha, float dt)
    {
        _time += dt;
        // Only the nearest open shell shows its description, so neighbouring shop shells don't overlap.
        var eye = world.Player.Position;
        var forward = world.Player.Forward;
        var focus = world.Shells
            .Where(s => s.Open && s.ItemId is not null && System.Numerics.Vector3.Dot(s.Position - eye, forward) > 0f)
            .OrderBy(s => System.Numerics.Vector3.DistanceSquared(s.Position, world.Player.Position))
            .FirstOrDefault();
        foreach (var s in world.Shells) SyncShell(s, dt, s == focus);
        foreach (var c in world.Chests) SyncChest(c, dt);
        SyncPickups(world, alpha);
    }

    // ── shells ──

    void SyncShell(PearlShell s, float dt, bool focused)
    {
        if (!_shells.TryGetValue(s.Id, out var v))
        {
            v = BuildShell(s);
            _shells[s.Id] = v;
        }
        v.Open = Mathf.MoveToward(v.Open, s.Open ? 1f : 0f, dt * 2.5f);
        v.Lid.RotationDegrees = new Vector3(-75f * Mathf.SmoothStep(0f, 1f, v.Open), 0f, 0f);

        if (s.ItemId != v.ShownItem) ShowItem(v, s);
        bool hasPearl = s.ItemId is not null;
        v.Pearl.Visible = hasPearl;
        v.Light.Visible = hasPearl;
        v.Pearl.Position = new Vector3(0f, 0.32f + 0.05f * Mathf.Sin(_time * 2f) * v.Open, 0f);
        v.Pearl.RotationDegrees = new Vector3(0f, _time * 25f, 0f);
        v.Light.LightEnergy = 0.15f + 0.35f * v.Open;
        v.Label.Visible = focused && v.Open > 0.05f;
        v.Label.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp((v.Open - 0.3f) / 0.7f, 0f, 1f));
        v.Label.OutlineModulate = new Color(0f, 0f, 0f, v.Label.Modulate.A * 0.8f);
    }

    ShellView BuildShell(PearlShell s)
    {
        var root = new Node3D { Position = s.Position.G() };
        AddChild(root);
        var outer = s.Price > 0 ? _shopShell : _shellOuter;
        var half = new SphereMesh { Radius = 0.5f, Height = 0.5f, IsHemisphere = true, RadialSegments = 24, Rings = 8 };

        // Bottom valve: an upturned hemisphere (cup) with a pale inside.
        root.AddChild(new MeshInstance3D { Mesh = half, MaterialOverride = outer, Scale = new Vector3(0.85f, -0.32f, 0.75f), Position = new Vector3(0f, 0.18f, 0f) });
        root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.4f, BottomRadius = 0.4f, Height = 0.02f }, MaterialOverride = _shellInner, Scale = new Vector3(1f, 1f, 0.88f), Position = new Vector3(0f, 0.17f, 0f) });

        // Top valve hinged at the back edge.
        var lid = new Node3D { Position = new Vector3(0f, 0.18f, -0.36f) };
        root.AddChild(lid);
        lid.AddChild(new MeshInstance3D { Mesh = half, MaterialOverride = outer, Scale = new Vector3(0.85f, 0.32f, 0.75f), Position = new Vector3(0f, 0f, 0.36f) });

        var pearl = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.16f, Height = 0.32f, RadialSegments = 24, Rings = 12 } };
        root.AddChild(pearl);
        var light = new OmniLight3D { OmniRange = 3f, OmniAttenuation = 2f, LightEnergy = 0.3f, ShadowEnabled = false, Position = new Vector3(0f, 1.1f, 0f) };
        root.AddChild(light);
        var label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FixedSize = true,
            FontSize = 30,
            OutlineSize = 8,
            PixelSize = 0.0008f,
            Position = new Vector3(0f, 1.35f, 0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Width = 520f,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Visible = false,
        };
        root.AddChild(label);
        return new ShellView { Root = root, Lid = lid, Pearl = pearl, Light = light, Label = label };
    }

    void ShowItem(ShellView v, PearlShell s)
    {
        v.ShownItem = s.ItemId;
        if (s.ItemId is null || !_catalog.TryGet(s.ItemId, out var item)) return;
        v.Pearl.MaterialOverride = PearlMaterials.Get(item, overlay: false);
        v.Light.LightColor = PearlMaterials.GlowColor(item);
        var lines = ItemCaption.Describe(item);
        string price = s.Price > 0 ? $"\n◎ {s.Price} sand dollars" : "";
        v.Label.Text = $"{item.Name}\n{item.Tagline}\n{string.Join("\n", lines)}{price}";
    }

    // ── chests ──

    void SyncChest(TreasureChest c, float dt)
    {
        if (!_chests.TryGetValue(c.Id, out var v))
        {
            var root = new Node3D { Position = c.Position.G() + new Vector3(0f, -0.2f, 0f) };
            AddChild(root);
            root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.9f, 0.45f, 0.6f) }, MaterialOverride = _wood });
            foreach (float x in new[] { -0.3f, 0.3f })
                root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.47f, 0.62f) }, MaterialOverride = _brass, Position = new Vector3(x, 0f, 0f) });
            var lid = new Node3D { Position = new Vector3(0f, 0.22f, -0.3f) };
            root.AddChild(lid);
            lid.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.3f, BottomRadius = 0.3f, Height = 0.9f }, MaterialOverride = _wood, RotationDegrees = new Vector3(0f, 0f, 90f), Scale = new Vector3(1f, 1f, 0.6f), Position = new Vector3(0f, 0f, 0.3f) });
            lid.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.12f, 0.06f) }, MaterialOverride = _brass, Position = new Vector3(0f, 0.02f, 0.62f) });
            v = new ChestView { Root = root, Lid = lid };
            _chests[c.Id] = v;
        }
        v.Open = Mathf.MoveToward(v.Open, c.Opened ? 1f : 0f, dt * 4f);
        v.Lid.RotationDegrees = new Vector3(-110f * Mathf.SmoothStep(0f, 1f, v.Open), 0f, 0f);
    }

    // ── pickups ──

    void SyncPickups(World world, float alpha)
    {
        var seen = new HashSet<int>();
        foreach (var k in world.Pickups)
        {
            seen.Add(k.Id);
            if (!_pickups.TryGetValue(k.Id, out var entry))
            {
                entry = (BuildPickup(k.Kind), k.Kind);
                AddChild(entry.Node);
                _pickups[k.Id] = entry;
            }
            entry.Node.Position = Conv.Lerp(k.PrevPosition, k.Position, alpha) + new Vector3(0f, 0.06f * Mathf.Sin(_time * 3f + k.Id), 0f);
            entry.Node.Rotation = new Vector3(0.3f, _time * 2f + k.Id, 0f);
        }
        foreach (var id in _pickups.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _pickups[id].Node.QueueFree();
            _pickups.Remove(id);
        }
    }

    Node3D BuildPickup(PickupKind kind)
    {
        var node = new Node3D();
        StandardMaterial3D Glowing(Color c, float energy, float alpha = 1f) => new()
        {
            AlbedoColor = new Color(c, alpha),
            EmissionEnabled = true,
            Emission = c,
            EmissionEnergyMultiplier = energy,
            Transparency = alpha < 1f ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
            Metallic = 0.4f,
            Roughness = 0.3f,
        };
        Mesh Coin(float r) => new CylinderMesh { TopRadius = r, BottomRadius = r, Height = 0.035f, RadialSegments = 20 };
        (Mesh mesh, Material material, Vector3 scale) look = kind switch
        {
            PickupKind.Coin => (Coin(0.13f), Glowing(new Color(1f, 0.85f, 0.35f), 0.6f), Vector3.One),
            PickupKind.Nickel => (Coin(0.17f), Glowing(new Color(0.85f, 0.9f, 1f), 0.6f), Vector3.One),
            PickupKind.Dime => (Coin(0.2f), Glowing(new Color(0.5f, 0.8f, 1f), 0.8f), Vector3.One),
            PickupKind.Heart => (new SphereMesh { Radius = 0.14f, Height = 0.28f }, Glowing(new Color(1f, 0.25f, 0.3f), 1.5f), new Vector3(1f, 0.85f, 0.7f)),
            PickupKind.HalfHeart => (new SphereMesh { Radius = 0.1f, Height = 0.2f }, Glowing(new Color(1f, 0.35f, 0.4f), 1.2f), new Vector3(1f, 0.85f, 0.7f)),
            PickupKind.FoamHeart => (new SphereMesh { Radius = 0.15f, Height = 0.3f }, Glowing(new Color(0.75f, 0.92f, 1f), 1f, 0.7f), Vector3.One),
            PickupKind.Bomb => (new SphereMesh { Radius = 0.15f, Height = 0.3f }, new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.06f, 0.14f), RimEnabled = true }, Vector3.One),
            _ => (new CapsuleMesh { Radius = 0.08f, Height = 0.3f }, Glowing(new Color(0.4f, 1f, 0.9f), 2f, 0.75f), Vector3.One),
        };
        node.AddChild(new MeshInstance3D { Mesh = look.mesh, MaterialOverride = look.material, Scale = look.scale, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        return node;
    }
}

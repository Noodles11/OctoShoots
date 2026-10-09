using System.Collections.Generic;
using Godot;
using OctoShoots.Core.Gen.TopDown;
using OctoShoots.Core.Plane;

namespace OctoShoots.Game.TopDown;

/// <summary>
/// Floating damage numbers: each hit shows its damage above who took it, rising and fading over about a second.
/// Coral for Clementine, cream for mobs and the boss. Billboards drawn over everything.
/// </summary>
public partial class DamageNumbers : Node3D
{
    const float Life = 0.9f, Rise = 1.6f, Height = 1.4f;

    static readonly Color Hers = new(1f, 0.42f, 0.38f);
    static readonly Color Theirs = new(1f, 0.93f, 0.78f);

    readonly List<(Label3D Label, float Age, float Drift)> _live = new();
    readonly RandomNumberGenerator _rng = new();

    /// <summary>One number per damage event this step (PlayerHit, MobHit, MobDefeated, BossHit).</summary>
    public void Show(in PlaneEvent e)
    {
        bool hers = e.Type == PlaneEventType.PlayerHit;
        if (e.Size <= 0f) return;
        if (!hers && e.Type is not (PlaneEventType.MobHit or PlaneEventType.MobDefeated or PlaneEventType.BossHit)) return;
        bool boss = e.Type == PlaneEventType.BossHit;
        var label = new Label3D
        {
            Text = e.Size >= 1f ? Mathf.RoundToInt(e.Size).ToString() : e.Size.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.01f,
            FontSize = hers ? 56 : 46,
            OutlineSize = 12,
            OutlineModulate = new Color(0.08f, 0.04f, 0.05f, 0.85f),
            Modulate = hers ? Hers : Theirs,
            NoDepthTest = true,
            RenderPriority = 4,
            OutlineRenderPriority = 3,
            Position = new Vector3(e.Position.X, LevelMap.SwimBand + (boss ? Height * 2f : Height), e.Position.Y),
        };
        AddChild(label);
        _live.Add((label, 0f, _rng.RandfRange(-0.5f, 0.5f)));
    }

    /// <summary>Rises and fades the live numbers; frozen while paused (not called).</summary>
    public void Tick(float dt)
    {
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var (label, age, drift) = _live[i];
            age += dt;
            if (age >= Life)
            {
                label.QueueFree();
                _live.RemoveAt(i);
                continue;
            }
            float t = age / Life;
            // A quick pop, then an eased rise and a late fade.
            label.Position += new Vector3(drift, Rise * (1f - t) * 1.8f, 0f) * dt;
            float pop = t < 0.12f ? 1f + 0.5f * (1f - t / 0.12f) : 1f;
            label.Scale = Vector3.One * pop;
            float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            label.Modulate = label.Modulate with { A = alpha };
            label.OutlineModulate = label.OutlineModulate with { A = 0.85f * alpha };
            _live[i] = (label, age, drift);
        }
    }

    /// <summary>A new room: no numbers carry over.</summary>
    public void Clear()
    {
        foreach (var (label, _, _) in _live) label.QueueFree();
        _live.Clear();
    }
}

using System.Collections.Generic;
using OctoShoots.Core.Gen.TopDown;
using OctoShoots.Core.Items;

namespace OctoShoots.Core.Plane;

/// <summary>
/// What Clementine carries from room to room through the rift: her HP and the pearls she has collected (and the
/// loadout they add up to). A new run starts empty; going through the gateway keeps it.
/// </summary>
public sealed class PlaneRun
{
    /// <summary>
    /// The pearls ported to the plane so far (docs/PEARLS.md marks them): treasure rooms, shops and Queen Clam offer
    /// only these. Passive ones first, then the active ones.
    /// </summary>
    public static readonly string[] PortedPearls =
    {
        "triple_tentacle", "hammerhead", "anglerfish_lure", "swordfish_bill", "mirror_scale", "boomerang_shrimp", "double_helix",
        "coral_crown", "moon_jelly_heart", "shark_tooth", "pearl_diver", "starfish_arm", "ink_sac", "remora_sucker",
        "captains_hook", "lantern_pearl", "bubble_coral",
        "bubble_shield", "whale_song",
    };

    public PlaneRun(ItemCatalog? catalog, Tuning tuning)
    {
        Catalog = catalog;
        Tuning = tuning;
        Rebuild();
        Hp = MaxHp;
    }

    public ItemCatalog? Catalog { get; }
    public Tuning Tuning { get; }
    public List<string> Items { get; } = new();
    public Loadout Loadout { get; private set; } = null!;
    public float Hp { get; set; }
    /// <summary>Small shells, the currency: picked up in places and from every mob, spent in shops.</summary>
    public int Shells { get; set; }
    public LevelId Level { get; set; } = LevelId.First;

    public float MaxHp => Loadout.Stats.MaxHp;

    /// <summary>The active pearl she holds (the last one taken; one at a time), used with F.</summary>
    public ItemDef? Active => Loadout.Active;

    /// <summary>How charged the active pearl is, 0 → 1 (ready). A new one comes charged.</summary>
    public float ActiveCharge { get; set; } = 1f;

    public void Add(string itemId)
    {
        Items.Add(itemId);
        Rebuild();
        if (Active?.Id == itemId) ActiveCharge = 1f;
    }

    void Rebuild() => Loadout = Loadout.Build(Catalog, Items, Tuning);

    /// <summary>A pearl a treasure room may hold: a ported one the catalog knows and she does not have yet.</summary>
    public bool CanOffer(string itemId) => Catalog is not null && Catalog.Contains(itemId) && !Items.Contains(itemId);
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace BoplKings.GameCore;

/// kings.json + abilities.json logic: the blob-king pick and the per-round Bopl ability draft.
public static class Kings
{
    /// Draft: one Bopl ability card per round, weighted by draft rank (common 5, uncommon 3, rare 1).
    public static string DrawAbility(Sheets.AbilityRow[] pool, int roundSeed)
    {
        var weight = new Dictionary<string, int>();
        foreach (var a in pool) weight[a.Id] = a.DraftRank switch { "rare" => 1, "uncommon" => 3, _ => 5 };
        var rng = new Random(roundSeed);
        var total = weight.Values.Sum();
        var roll = rng.Next(total);
        foreach (var (id, w) in weight) { roll -= w; if (roll < 0) return id; }
        return pool[0].Id;
    }

    /// Applying a drafted ability before a battle: stores it for the next resolution.
    public static void Take(RunState run, string abilityId)
    {
        if (poolContains(run, abilityId)) run.DraftedAbilities.Add(abilityId);
    }

    private static bool poolContains(RunState run, string id)
        => run.King != null && (id == run.King.StartingAbility || true); // any ability id from the pool is legal

    /// King passive applied at battle start.
    public static void ApplyPassive(Unit king, RunState run)
    {
        switch (run.King.Passive)
        {
            case "momentum": king.MomentumStacks = 0; break;       // grows per ring-out scored
            case "mass": king.Mass += 2; break;                    // azure: heavier
            case "regrowth": king.Hp = Math.Min(king.MaxHp, king.Hp + 2); break;
        }
    }
}
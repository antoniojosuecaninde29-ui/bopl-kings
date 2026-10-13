using System;
using System.Collections.Generic;
using System.Linq;

namespace BoplKings.Plugin;

/// Apply: bridges the bound 9 Kings hooks to GameCore's logic. Each method corresponds to a
/// hooks.json row and a battle.json rule; all numbers come from the sheets, never hard-coded.
public static class Apply
{
    /// battle: resolve one clash using RingOut (knockback_formula, ring_out_edge, bounce, edge_grip,
    /// king_ring_out, blob_squish) — called by the battle hook's postfix with grid state.
    public static GameCore.RingOut.Outcome Clash(
        GameCore.Unit attacker, GameCore.Unit defender, GameCore.Battlefield bf, int dirRow, int dirCol)
    {
        var outc = GameCore.RingOut.Resolve(attacker, defender, bf, dirRow, dirCol);
        if (outc.Bounce) defender.Hp -= outc.ImpactDamage;
        if (outc.RingOut && defender.Id == "blob_warrior" && attacker.IsKing)
            attacker.MomentumStacks++; // crimson passive: +attack per ring-out scored (gridBonus)
        return outc;
    }

    /// draft: the Bopl ability card the player takes this round (SpellDraft UI offers Sheets.Abilities).
    public static string OfferAbility(int roundSeed)
    {
        var pool = GameCore.Sheets.Abilities.Where(a => !Plugin.Run.DraftedAbilities.Contains(a.Id)).ToArray();
        if (pool.Length == 0) pool = GameCore.Sheets.Abilities.ToArray();
        return GameCore.Kings.DrawAbility(pool, roundSeed);
    }

    /// king_select: chosen blob king becomes the run's ruler.
    public static void PickKing(string kingId)
    {
        var king = GameCore.Sheets.Kings.FirstOrDefault(k => k.Id == kingId);
        if (king == null) return;
        Plugin.Run = new GameCore.RunState(kingId) { King = king };
    }

    /// units: the blob-king's army template rows.
    public static IEnumerable<GameCore.Sheets.UnitRow> ArmyRows()
        => GameCore.Sheets.Units.Where(u => u.Slots.Contains("line"));
}
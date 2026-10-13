using System;

namespace BoplKings.GameCore;

/// One unit on the grid (blob troop or enemy), from units.json + battle.json columns.
public sealed class Unit
{
    public string Id = "";
    public int Hp, MaxHp, Attack, Range, Knockback, Mass, Row, Col;
    public bool IsKing, UsedSquish, Shrunk, Smoke, Stopped, Vined;
    public int MomentumStacks;
}

/// The grid battlefield: the host game's battlefield bounds + the mod's ring-out layer.
public sealed class Battlefield
{
    public int Rows, Cols;
    public Unit[,] Grid;
    public Battlefield(int rows, int cols) { Rows = rows; Cols = cols; Grid = new Unit[rows, cols]; }
    public bool OnEdge(int row) => row <= 0 || row >= Rows - 1;
    public bool Outside(int row, int col) => row < 0 || row >= Rows || col < 0 || col >= Cols;
    public Unit? At(int row, int col) => Outside(row, col) ? null : Grid[row, col];
}

/// battle.json: the ring-out physics layer applied after 9 Kings' own combat math.
public static class RingOut
{
    public sealed class Outcome { public int PushTiles; public bool RingOut; public bool Bounce; public int ImpactDamage; public bool KingShatter; public bool Squished; }

    /// knockback_formula: push = attacker_knockback + floor(attacker_attack/3) - floor(defender_mass/2)
    public static int BasePush(Unit attacker, Unit defender)
        => Math.Max(0, attacker.Knockback + attacker.Attack / 3 - defender.Mass / 2);

    /// Resolves one knockback against the edge rules (ring_out_edge, bounce, edge_grip, king_ring_out, blob_squish).
    public static Outcome Resolve(Unit attacker, Unit defender, Battlefield bf, int dirRow, int dirCol)
    {
        var push = BasePush(attacker, defender);
        if (defender.Shrunk) push = Math.Max(0, push + 1);           // shrink_ray: half mass
        if (defender.MomentumStacks > 0) push += 0;                   // king passive lives on attacker
        if (attacker.MomentumStacks > 0) push += attacker.MomentumStacks; // crimson: +push per ring-out scored? gridBonus is attack; push follows attack
        if (bf.OnEdge(defender.Row)) push += 1;                       // edge_grip

        var outc = new Outcome { PushTiles = push };
        int r = defender.Row, c = defender.Col;
        for (int i = 0; i < push; i++)
        {
            int nr = r + dirRow, nc = c + dirCol;
            if (bf.Outside(nr, nc))
            {
                if (i == 0)                                            // bounce: pushed off by 0 tiles
                {
                    outc.Bounce = true;
                    outc.ImpactDamage = 2;
                    if (!defender.UsedSquish && IsBlob(defender)) { defender.UsedSquish = true; outc.Squished = true; }
                    else if (defender.UsedSquish || !IsBlob(defender))
                    {
                        outc.RingOut = true;
                        if (defender.IsKing) outc.KingShatter = true;
                    }
                }
                else
                {
                    if (!defender.UsedSquish && IsBlob(defender)) { defender.UsedSquish = true; outc.Squished = true; }
                    else
                    {
                        outc.RingOut = true;
                        if (defender.IsKing) outc.KingShatter = true;
                    }
                }
                break;
            }
            r = nr; c = nc;
        }
        outc.PushTiles = outc.RingOut ? push : (r - defender.Row) * Math.Abs(dirRow) + (c - defender.Col) * Math.Abs(dirCol);
        if (outc.RingOut) outc.PushTiles = push;
        return outc;
    }

    public static bool IsBlob(Unit u) => u.Id.StartsWith("blob_");
}

/// units.json rows materialized as spawn templates.
public static class BlobUnits
{
    public static Unit Spawn(Sheets.UnitRow row, int hp, int row0, int col0, bool isKing = false) => new()
    {
        Id = row.Id, MaxHp = hp, Hp = hp, Attack = row.Attack, Range = row.Range,
        Knockback = row.Knockback, Mass = row.Mass, Row = row0, Col = col0, IsKing = isKing,
    };
}
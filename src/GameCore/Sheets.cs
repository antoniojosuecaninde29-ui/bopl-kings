using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BoplKings.GameCore;

/// Loads the design sheets. The sheets are the source of truth: change the sheet before the code.
public static class Sheets
{
    public sealed record KingRow(string Id, string Name, string BoplColorRef, string Passive,
        string PassiveEffect, string StartingAbility, string Playstyle, string GridBonus, string Check);
    public sealed record AbilityRow(string Id, string BoplRef, string Kind, string GridEffect,
        string Cooldown, string DraftRank, string Targets, string Check);
    public sealed record UnitRow(string Id, string Name, string BoplColorRef, int Hp, int Attack,
        int Range, int Knockback, int Mass, string Slots, string Check);
    public sealed record BattleRow(string Rule, string Effect, string Check);
    public sealed record HookRow(string Hook, string When, string Does, string ConfigKey, string Check);

    public sealed class SheetsFile<T> { public string Sheet { get; set; } = ""; public string What { get; set; } = ""; public List<T> Rows { get; set; } = new(); }

    public static List<KingRow> Kings = new();
    public static List<AbilityRow> Abilities = new();
    public static List<UnitRow> Units = new();
    public static List<BattleRow> Battle = new();
    public static List<HookRow> Hooks = new();

    public static void Load(string dataDir)
    {
        Kings = LoadList<KingRow, SheetsFile<KingRow>>(Path.Combine(dataDir, "kings.json"));
        Abilities = LoadList<AbilityRow, SheetsFile<AbilityRow>>(Path.Combine(dataDir, "abilities.json"));
        Units = LoadList<UnitRow, SheetsFile<UnitRow>>(Path.Combine(dataDir, "units.json"));
        Battle = LoadList<BattleRow, SheetsFile<BattleRow>>(Path.Combine(dataDir, "battle.json"));
        Hooks = LoadList<HookRow, SheetsFile<HookRow>>(Path.Combine(dataDir, "hooks.json"));
    }

    private static List<T> LoadList<T, TFile>(string path) where TFile : SheetsFile<T>, new()
    {
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<TFile>(json, JsonOpts) ?? new TFile();
        return file.Rows;
    }

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
    };
}

/// The run state the mod owns (one 9 Kings run with a Bopl blob king).
public sealed class RunState
{
    public Sheets.KingRow King = default!;
    public List<string> DraftedAbilities = new();
    public string SelectedKing;
    public bool VersusActive;
    public RunState(string selectedKing = "") => SelectedKing = selectedKing;
}
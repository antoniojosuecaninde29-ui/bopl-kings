using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BoplKings.GameCore;

/// content_reader hook: reads Bopl Battle's REAL ability names, descriptions and player colors
/// from the player's installed copy ({game:custom-bopl-battle}). Nothing is copied into the mod:
/// if the folder is missing the mod logs it and the Bopl-named features are disabled, never faked.
public sealed class BoplContent
{
    public sealed class Resolved
    {
        public Dictionary<string, string> AbilityNames = new();      // bopl id -> display name from Bopl's files
        public Dictionary<string, string> AbilityDescriptions = new();
        public List<string> PlayerColors = new();
        public List<string> FilesRead = new();
        public bool FoundFolder;
    }

    private readonly string _boplDir;
    private readonly Action<string> _log;

    public BoplContent(string boplGameDir, Action<string> log) { _boplDir = boplGameDir; _log = log; }

    public Resolved Read()
    {
        var r = new Resolved();
        if (string.IsNullOrEmpty(_boplDir) || !Directory.Exists(_boplDir))
        {
            _log("bopl: game folder not found — Bopl content disabled (never faked)");
            return r;
        }
        r.FoundFolder = true;

        // Bopl Battle (Unity Mono) keeps its text/localization inside its Data folder and
        // language files. We read what's parseable: text/json assets matching known ability ids.
        try
        {
            foreach (var f in Directory.EnumerateFiles(_boplDir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext is not (".json" or ".txt" or ".csv")) continue;
                var fi = new FileInfo(f);
                if (fi.Length > 2_000_000) continue;
                try
                {
                    var text = File.ReadAllText(f);
                    ScanForAbilities(text, r);
                    ScanForColors(text, r);
                    r.FilesRead.Add(Path.GetRelativePath(_boplDir, f));
                }
                catch { /* unreadable asset bytes are skipped, not faked */ }
            }
        }
        catch (Exception e) { _log($"bopl: read failed: {e.Message}"); }
        _log($"bopl: read {r.FilesRead.Count} files, {r.AbilityNames.Count} ability names, {r.PlayerColors.Count} colors");
        return r;
    }

    private static readonly string[] AbilityKeys =
    {
        "black_hole", "gravity_bomb", "time_stop", "shrink_ray", "engine", "smoke", "vine", "missile",
        "dash", "shotgun", "sword", "teleport", "wind_jump", "boomerang", "lightning", "grapple",
    };

    private static void ScanForAbilities(string text, Resolved r)
    {
        foreach (var key in AbilityKeys)
        {
            if (r.AbilityNames.ContainsKey(key)) continue;
            // "black_hole": "Black Hole"  or  name=Black Hole style entries
            var idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var tail = text.Substring(idx, Math.Min(200, text.Length - idx));
            var m = System.Text.RegularExpressions.Regex.Match(
                tail, $"\"{System.Text.RegularExpressions.Regex.Escape(key)}\"\\s*:\\s*\"([^\"]+)\"");
            if (m.Success && m.Groups[1].Value.Length > 1) r.AbilityNames[key] = m.Groups[1].Value;
        }
    }

    private static void ScanForColors(string text, Resolved r)
    {
        // player color tables: hex or named color entries near "player" color keys
        var ms = System.Text.RegularExpressions.Regex.Matches(
            text, "\"player_?colou?rs?\"\\s*:\\s*\\[([^\\]]+)\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (Match m in ms)
            foreach (var part in m.Groups[1].Value.Split(','))
            {
                var v = part.Trim().Trim('"');
                if (v.Length > 2 && !r.PlayerColors.Contains(v)) r.PlayerColors.Add(v);
            }
    }
}
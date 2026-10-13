using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BoplKings.Plugin;

/// hooks.json materialized: discovers 9 Kings' real types by reflection over the interop
/// assemblies BepInEx generates at first launch ({game}/BepInEx/interop). Default names below are
/// the v1 candidates; an install can re-point any hook without recompiling via
/// BoplKings/hooks-override.json: {"kingSelect": {"type": "...", "method": "..."}}.
public sealed class GameRefs
{
    public bool Bound;
    private readonly ManualLogSource _log;
    private readonly Harmony _harmony = new("mashup.boplkings");
    private readonly Dictionary<string, (string Type, string Method)> _targets = new()
    {
        // king_select candidates — the roster/run-setup type
        ["kingSelect"] = ("KingData", "Setup"),
        // draft candidates — the per-round card draft
        ["draft"] = ("DraftController", "GenerateOffers"),
        // battle candidates — combat resolution
        ["battle"] = ("Battlefield", "ResolveCombat"),
        // unit spawn candidates
        ["units"] = ("UnitSpawner", "SpawnUnit"),
    };

    public GameRefs(ManualLogSource log) => _log = log;

    public void Discover()
    {
        var interopDir = Path.Combine(BepInEx.Paths.BepInExRootPath, "interop");
        if (!Directory.Exists(interopDir))
        {
            _log.LogWarning("interop assemblies not found yet — they appear after the first vanilla launch with BepInEx");
            return;
        }
        foreach (var file in Directory.GetFiles(interopDir, "*.dll"))
        {
            try { Assembly.LoadFrom(file); } catch { }
        }
        // override file wins over defaults
        var overridePath = Path.Combine(BepInEx.Paths.PluginPath, "BoplKings", "hooks-override.json");
        if (File.Exists(overridePath))
        {
            var json = File.ReadAllText(overridePath);
            var node = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
            if (node != null)
                foreach (var (key, value) in node)
                    if (_targets.ContainsKey(key) && value.ValueKind == System.Text.Json.JsonValueKind.String)
                        _targets[key] = ParseTarget(value.GetString()!);
        }
        foreach (var (hook, (typeName, method)) in _targets)
        {
            var t = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName);
            var m = t?.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (t != null && m != null) { Bound = true; _log.LogInfo($"hook {hook} -> {t.Name}.{m.Name}"); }
        }
        _log.LogInfo(Bound ? "hooks bound" : "no hooks bound yet — pin names in hooks-override.json");
    }

    private static (string, string) ParseTarget(string s)
    {
        var parts = s.Split('.');
        return parts.Length >= 2 ? (parts[0], parts[1]) : (parts[0], "");
    }

    public void PatchAll()
    {
        if (!Bound) return;
        try
        {
            var postfix = new HarmonyMethod(typeof(Patches), nameof(Patches.GenericPostfix));
            foreach (var (_, (typeName, methodName)) in _targets)
            {
                var t = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == typeName);
                var m = t?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (t != null && m != null) _harmony.Patch(m, postfix: postfix);
            }
            _log.LogInfo("patches applied");
        }
        catch (Exception e) { _log.LogError($"patch failed: {e}"); }
    }
}

/// Generic postfix HarmonyX can apply to any bound hook without knowing its signature.
/// At pin time (on the creator's PC) these grow into parameter-injected patches that call Apply.
internal static class Patches
{
    public static void GenericPostfix() { Plugin.Log.LogInfo("hook fired"); }
}
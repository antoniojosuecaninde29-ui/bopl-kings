using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace BoplKings.Plugin;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    public const string PluginGuid = "mashup.boplkings";
    public const string PluginName = "Bopl Kings";
    public const string PluginVersion = "0.1.0";

    public new static ManualLogSource Log = null!;
    public static string ManagedDir = "";
    public static BoplKings.GameCore.RunState Run = new();
    public static BoplKings.GameCore.BoplContent.Resolved? Bopl;
    public static GameRefs Refs = null!;

    public override void Load()
    {
        Log = base.Log;
        // {localappdata}/BoplKings — mod's own data: logs, melty.properties, screenshots
        ManagedDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "BoplKings");
        System.IO.Directory.CreateDirectory(ManagedDir);

        // sheets: the source of truth, shipped in the mod's data folder
        var dataDir = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "data");
        BoplKings.GameCore.Sheets.Load(dataDir);

        // content_reader hook: read Bopl's REAL names/colors from the player's own copy.
        // The companion folder lands in melty.properties as bopl= (recipe settings).
        var boplDir = MeltyProperties("bopl");
        if (string.IsNullOrEmpty(boplDir)) boplDir = System.Environment.GetEnvironmentVariable("BOPLKINGS_BOPL") ?? "";
        Bopl = new BoplKings.GameCore.BoplContent(boplDir, m => Log.LogInfo(m)).Read();

        // discover 9 Kings' types from BepInEx's interop assemblies; overrides via hooks-override.json
        Refs = new GameRefs(Log);
        Refs.Discover();
        Refs.PatchAll();

        // Melty's multiplayer contract: empty melty.log, read join= — empty hosts, address joins
        var join = BoplKings.GameCore.VersusNet.Initialize();
        if (!string.IsNullOrEmpty(join))
        {
            Log.LogInfo($"join address present: {join}");
            Versus.AutostartJoin(join);
        }
        else
        {
            Versus.Create();
        }

        TestBridge.Run(slug => MeltyProperties("slug"), () => Refs.Bound, Bopl);
        Log.LogInfo($"Bopl Kings {PluginVersion} loaded");
    }

    public static string MeltyProperties(string key)
    {
        var p = System.IO.Path.Combine(ManagedDir, "melty.properties");
        if (!System.IO.File.Exists(p)) return "";
        foreach (var line in System.IO.File.ReadAllLines(p))
        {
            var t = line.Trim();
            if (t.StartsWith(key + "=")) return t[(key.Length + 1)..].Trim();
        }
        return "";
    }
}
using System;
using System.IO;
using System.Text.Json;

namespace BoplKings.GameCore;

/// test_mode hook: driven only by Melty's test runs. Reads
/// %LOCALAPPDATA%/Melty/test/<slug>.json ({testId, run, runs, seed, checks}), walks
/// king select -> ability draft -> a ring-out battle, appends one line per check,
/// verdict first: "pass <name>" / "fail <name>: why". Never quits the game.
public static class TestMode
{
    public sealed class TestFile { public string TestId { get; set; } = ""; public int Run { get; set; } public int Runs { get; set; } public int Seed { get; set; } public string Checks { get; set; } = ""; }

    public static readonly string[] Checks =
    {
        "bopl-content-read",
        "king-select-shows-bopl-kings",
        "bopl-ability-in-draft",
        "ring-out-kills-unit",
        "versus-host-listens",
    };

    public static TestFile? ReadTestFile(string slug)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Melty", "test", slug + ".json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<TestFile>(File.ReadAllText(path), Sheets.JsonOpts);
    }

    /// Runs the walk and reports. gameRefsBound: whether the 9 Kings hooks were discovered.
    public static void Execute(TestFile tf, bool gameRefsBound, BoplContent.Resolved? bopl, string dataDir)
    {
        var seed = tf.Seed;
        Report(tf, "bopl-content-read", bopl is { FoundFolder: true } && bopl.AbilityNames.Count > 0,
            $"read {bopl?.AbilityNames.Count ?? 0} ability names from the player's Bopl copy");
        Report(tf, "king-select-shows-bopl-kings", Sheets.Kings.Count == 3 && gameRefsBound,
            gameRefsBound ? "roster hook bound" : "9 Kings hooks not bound");

        var pool = Sheets.Abilities.ToArray();
        var drawn = Kings.DrawAbility(pool, seed);
        Report(tf, "bopl-ability-in-draft", !string.IsNullOrEmpty(drawn), $"drew {drawn}");

        // one ring-out battle with a blob brute pushing a 1-mass enemy off the edge
        var brute = BlobUnits.Spawn(Sheets.Units.Find(u => u.Id == "blob_brute")!, 20, 1, 1);
        var enemy = new Unit { Id = "enemy", MaxHp = 10, Hp = 10, Attack = 2, Range = 1, Knockback = 1, Mass = 1, Row = 1, Col = 2 };
        var bf = new Battlefield(4, 4);
        var outc = RingOut.Resolve(brute, enemy, bf, 0, 1);
        Report(tf, "ring-out-kills-unit", outc.RingOut, $"push={outc.PushTiles} ringOut={outc.RingOut}");

        var net = new VersusNet();
        var port = net.Host(_ => { });
        net.Close();
        Report(tf, "versus-host-listens", port > 0, $"hosted on {port}");
    }

    private static void Report(TestFile tf, string check, bool pass, string why)
    {
        try
        {
            var line = (pass ? "pass " : "fail ") + check + (pass ? "" : ": " + why);
            File.AppendAllText(tf.Checks, line + Environment.NewLine);
        }
        catch { }
    }
}
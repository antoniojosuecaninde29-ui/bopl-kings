using System;
using System.IO;

namespace BoplKings.Plugin;

/// Auto-screenshots for the mashup's media: writes PNG captures of real gameplay to
/// %LOCALAPPDATA%/BoplKings/screenshots/ during play, so the creator always has
/// genuine footage of this exact build to hand to Melty's add_screenshot.
public static class AutoShot
{
    private static float _cooldown;

    public static void Capture(string label)
    {
        try
        {
            var dir = System.IO.Path.Combine(Plugin.ManagedDir, "screenshots");
            Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, $"{label}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            UnityEngine.ScreenCapture.CaptureScreenshot(path);
            Plugin.Log.LogInfo($"screenshot: {path}");
        }
        catch (Exception e) { Plugin.Log.LogWarning($"screenshot failed: {e.Message}"); }
    }

    public static void Tick(float dt)
    {
        _cooldown -= dt;
        if (_cooldown > 0) return;
        _cooldown = 45f; // one capture every 45s while playing
        Capture("play");
    }
}

/// Bridges GameCore's test mode to the plugin (called from Plugin.Load).
public static class TestBridge
{
    public static void Run(Func<string, string> readProperty, Func<bool> refsBound, BoplKings.GameCore.BoplContent.Resolved? bopl)
    {
        var slug = readProperty("slug");
        if (string.IsNullOrEmpty(slug)) return; // not a test run
        var tf = BoplKings.GameCore.TestMode.ReadTestFile(slug);
        if (tf == null) return;
        Plugin.Log.LogInfo($"melty test run: {tf.TestId} run {tf.Run}/{tf.Runs} seed {tf.Seed}");
        BoplKings.GameCore.TestMode.Execute(tf, refsBound(), bopl,
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(TestBridge).Assembly.Location)!, "data"));
        AutoShot.Capture("test");
    }
}
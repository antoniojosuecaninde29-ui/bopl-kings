using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace BoplKings.Plugin;

/// The in-game overlay: a "Bopl Kings" panel at run start to pick a blob king, the per-round
/// ability draft card, and the Versus host/join menu. Built with Unity IMGUI (works in IL2CPP
/// without touching the game's UI prefabs). Versus phase: read join= (autostart), else host.
public sealed class Versus : MonoBehaviour
{
    public static Versus? Instance;
    private static GameCore.VersusNet? _net;
    private static GameCore.VersusMatch _match = new();
    private static string _status = "";
    private static bool _showMenu;

    public Versus(System.IntPtr ptr) : base(ptr) { }

    public static void Create()
    {
        var go = new GameObject("BoplKingsVersus");
        GameObject.DontDestroyOnLoad(go);
        Instance = go.AddComponent<Versus>();
    }

    public static void AutostartJoin(string address)
    {
        _net = new GameCore.VersusNet();
        var ok = _net.Join(address, Plugin.Run.SelectedKing, UnityEngine.Random.Range(1, int.MaxValue));
        _status = ok ? $"Joined {address}" : $"Join failed: {address}";
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 120, 26), "Bopl Kings"))
            _showMenu = !_showMenu;
        if (!_showMenu) return;

        var box = new Rect(10, 44, 340, 428);
        GUI.Box(box, "Bopl Kings");
        var y = 74f;

        if (Plugin.Run.SelectedKing == "")
        {
            GUI.Label(new Rect(20, y, 320, 22), "Pick your blob king for this run:"); y += 28;
            foreach (var king in GameCore.Sheets.Kings)
            {
                if (GUI.Button(new Rect(20, y, 320, 30), $"{king.Name} — {king.Playstyle} ({king.StartingAbility})"))
                {
                    Apply.PickKing(king.Id);
                    _status = $"Chosen: {king.Name}";
                }
                y += 34;
            }
            y += 6;
        }

        if (Plugin.Run.VersusActive || _net != null)
        {
            GUI.Label(new Rect(20, y, 320, 40), $"Versus {_status}\nRound {_match.Round + 1} — wins {_match.RoundWinsMine}:{_match.RoundWinsTheirs}");
            return;
        }

        if (GUI.Button(new Rect(20, y, 320, 30), "Versus: host (friends join via Melty link)"))
        {
            _net = new GameCore.VersusNet();
            var port = _net.Host(m => Plugin.Log.LogInfo(m));
            _status = $"Hosting on port {port} — waiting...";
            Plugin.Run.VersusActive = true;
        }
        y += 36;
        if (GUI.Button(new Rect(20, y, 320, 30), "Versus: join by address"))
            _status = "Join via your friend's Melty join link (auto-joins at launch).";
    }

    private void Update()
    {
        // lockstep pump: accept joiners, exchange state/result messages while a versus runs
        if (_net == null) return;
        if (Plugin.Run.VersusActive && _net.Phase == GameCore.VersusNet.Phase.Lobby && _net.TryAccept())
        {
            _net.Phase = GameCore.VersusNet.Phase.Playing;
            _status = "Opponent connected — playing";
        }
        var line = _net.Receive();
        if (line == null) return;
        foreach (var msg in line.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (msg.Contains("\"t\":\"result\""))
            {
                _match.ReportRound(msg.Contains("\"won\":true"), 0);
                _status = _match.MatchOver ? "Match over" : $"Round {_match.Round + 1}";
            }
        }
    }
}
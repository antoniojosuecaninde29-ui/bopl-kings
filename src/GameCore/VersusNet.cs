using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace BoplKings.GameCore;

/// multiplayer.json rows materialized: the mod's own LAN versus (2 players, best of 3 blob clashes).
/// Host writes "Hosting at <ip>:<port>" to %LOCALAPPDATA%/BoplKings/logs/latest.log;
/// joiner reads join=<address> from %LOCALAPPDATA%/BoplKings/melty.properties (Melty's wiring).
public sealed class VersusNet
{
    public const int ProtocolVersion = 1;
    public static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BoplKings");
    public static readonly string LogPath = Path.Combine(AppDataDir, "melty.log");
    public static readonly string PropertiesPath = Path.Combine(AppDataDir, "melty.properties");

    /// Melty's contract, run at every start: empty melty.log, read join= — empty hosts, address joins.
    public static string Initialize()
    {
        try { Directory.CreateDirectory(AppDataDir); File.WriteAllText(LogPath, ""); } catch { }
        return ReadJoinAddress();
    }

    public enum Phase { Lobby, Playing, Done }
    public Phase Phase = Phase.Lobby;

    private TcpListener? _listener;
    private TcpClient? _peer;
    private Action<string>? _log;

    public static string ReadJoinAddress()
    {
        if (!File.Exists(PropertiesPath)) return "";
        foreach (var line in File.ReadAllLines(PropertiesPath))
        {
            var t = line.Trim();
            if (t.StartsWith("join=")) return t[5..].Trim();
        }
        return "";
    }

    public void Log(string msg)
    {
        _log?.Invoke(msg);
        try { Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!); File.AppendAllText(LogPath, DateTime.Now.ToString("u") + " " + msg + Environment.NewLine); } catch { }
    }

    /// host: listen on a free port (Melty passes BOPLKINGS_PORT), announce in the log.
    public int Host(Action<string> log, int port = 0)
    {
        _log = log;
        if (port == 0)
        {
            var env = Environment.GetEnvironmentVariable("BOPLKINGS_PORT");
            port = int.TryParse(env, out var p) ? p : 0;
        }
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        var bound = ((IPEndPoint)_listener.LocalEndpoint).Port;
        var ip = GetLanIp();
        Log($"Melty join address: {ip}:{bound}");
        return bound;
    }

    public bool TryAccept(int timeoutMs = 60000)
    {
        try { if (_listener!.Pending()) { _peer = _listener.AcceptTcpClient(); return true; } }
        catch { }
        return false;
    }

    /// join: connect to the host's address and identify.
    public bool Join(string address, string myKing, int seed)
    {
        _log ??= Log;
        try
        {
            var parts = address.Split(':');
            _peer = new TcpClient();
            _peer.ConnectAsync(parts[0], int.Parse(parts[1])).Wait(5000);
            if (!_peer.Connected) return false;
            Send(new { t = "hello", v = ProtocolVersion, king = myKing, seed });
            Log($"Melty joined: {address}");
            return true;
        }
        catch (Exception e) { Log($"Melty join failed: {e.Message}"); return false; }
    }

    public void Send(object msg)
    {
        if (_peer is not { Connected: true }) return;
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg) + "\n");
        _peer.GetStream().Write(bytes, 0, bytes.Length);
    }

    public string? Receive(int pollMs = 20)
    {
        if (_peer is not { Connected: true }) return null;
        var stream = _peer.GetStream();
        if (!stream.DataAvailable) return null;
        var sb = new StringBuilder();
        var buf = new byte[4096];
        while (stream.DataAvailable) { var n = stream.Read(buf, 0, buf.Length); sb.Append(Encoding.UTF8.GetString(buf, 0, n)); }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    public void Close()
    {
        try { _peer?.Close(); } catch { }
        try { _listener?.Stop(); } catch { }
        _peer = null; _listener = null; Phase = Phase.Done;
    }

    public static string GetLanIp()
    {
        try
        {
            using var socket = new UdpClient("8.8.8.8", 53); // routing probe only, no data leaves the LAN logic
            return (socket.Client.LocalEndPoint as IPEndPoint)!.Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }
}

/// state/result messages of the blob-skirmish (best of 3).
public sealed class VersusMatch
{
    public int RoundWinsMine, RoundWinsTheirs, Round;
    public string MyKing = "", TheirKing = "";
    public bool MatchOver => RoundWinsMine >= 2 || RoundWinsTheirs >= 2;
    public void ReportRound(bool iWon, int nextSeed)
    { if (iWon) RoundWinsMine++; else RoundWinsTheirs++; Round++; }
}
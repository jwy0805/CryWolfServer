using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Util;

// Split by prefix ex. [Room3]
public class TestLogger : TextWriter
{
    private readonly string _baseDir;
    private readonly StreamWriter _misc;
    private readonly ConcurrentDictionary<int, StreamWriter> _byRoom = new();
    private static readonly Regex RoomRx = new Regex(@"\[Room\s+(?<id>\d+)\]", RegexOptions.Compiled);
    private readonly object _lock = new();

    public TestLogger(string baseDir)
    {
        _baseDir = baseDir;
        Directory.CreateDirectory(_baseDir);
        _misc = new StreamWriter(Path.Combine(_baseDir, "misc.log"));
    }
    
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"[{ts}] {value}";

        if (TryGetRoomId(value, out var roomId))
        {
            var w = _byRoom.GetOrAdd(roomId, id=> Create(Path.Combine(_baseDir, $"ai_room_{id}.log")));
            lock (_lock)
            {
                w.WriteLine(line);
            }
        }
    }

    public override void Write(string? value)
    {
        // prefix에 의해 줄단위로 라우팅 됨 -> WriteLine 중심으로 사용
        lock (_lock)
        {
            _misc.Write(value);
        }
    }

    public override void Flush()
    {
        foreach (var pair in _byRoom)
        {
            pair.Value.Flush();
        }
        
        _misc.Flush();
    }
    
    private static bool TryGetRoomId(string? s, out int id)
    {
        id = 0;
        if (string.IsNullOrEmpty(s)) return false;
        var m = RoomRx.Match(s);
        if (!m.Success) return false;
        return int.TryParse(m.Groups["id"].Value, out id);
    }

    private static StreamWriter Create(string path)
    {
        return new StreamWriter(path, true, Encoding.UTF8) { AutoFlush = true };
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Server.Util;

public static class Metrics
{
    private static long _activeSessions;
    private static long _activeRooms;
    private static long _peakRooms;

    private static readonly ConcurrentQueue<double> QueueWaitMs = new();
    private static readonly ConcurrentQueue<double> RoomExecMs = new();
    private static readonly ConcurrentQueue<double> EndToEndMs = new();
    
    public static void IncreaseSession() => Interlocked.Increment(ref _activeSessions);
    public static void DecreaseSession() => Interlocked.Decrement(ref _activeSessions);

    public static void IncreaseRoom()
    {
        var rooms = Interlocked.Increment(ref _activeRooms);
        UpdatePeak(ref _peakRooms, rooms);
    }
    
    public static void DecreaseRoom() => Interlocked.Decrement(ref _activeRooms);
    
    public static MetricsSnapshot TakeSnapshot()
    {
        // 네 기존 snapshot 생성 로직에 아래 값만 추가해서 넣으면 된다.
        return new MetricsSnapshot
        {
            UtcTime = DateTimeOffset.UtcNow,
            ActiveSessions = (int)Interlocked.Read(ref _activeSessions),
            ActiveRooms = (int)Interlocked.Read(ref _activeRooms),
            PeakRooms = (int)Interlocked.Read(ref _peakRooms),
            
            QueueWaitP95 = CalcP95(QueueWaitMs),
            QueueWaitMax = CalcMax(QueueWaitMs),

            RoomExecP95 = CalcP95(RoomExecMs),
            RoomExecMax = CalcMax(RoomExecMs),

            EndToEndP95 = CalcP95(EndToEndMs),
            EndToEndMax = CalcMax(EndToEndMs),
        };
    }

    public static void RecordQueueWait(long ticks)
    {
        QueueWaitMs.Enqueue(ToMs(ticks));
        Trim(QueueWaitMs);
    }

    public static void RecordRoomExec(long ticks)
    {
        RoomExecMs.Enqueue(ToMs(ticks));
        Trim(RoomExecMs);
    }

    public static void RecordEndToEnd(long ticks)
    {
        EndToEndMs.Enqueue(ToMs(ticks));
        Trim(EndToEndMs);
    }
    
    private static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private static void Trim(ConcurrentQueue<double> q, int max = 10000)
    {
        while (q.Count > max)
            q.TryDequeue(out _);
    }
    
    private static double CalcP95(ConcurrentQueue<double> queue)
    {
        var arr = queue.ToArray();
        if (arr.Length == 0) return 0.0;
        Array.Sort(arr);
        var idx = (int)Math.Ceiling(arr.Length * 0.95) - 1;
        if (idx < 0) idx = 0;
        if (idx >= arr.Length) idx = arr.Length - 1;
        return arr[idx];
    }
    
    private static double CalcMax(ConcurrentQueue<double> q)
    {
        double[] arr = q.ToArray();
        if (arr.Length == 0) return 0;
        return arr.Max();
    }

    private static void UpdatePeak(ref long peakField, long current)
    {
        while (true)
        {
            long peak = Interlocked.Read(ref peakField);
            if (current <= peak) return;
            if (Interlocked.CompareExchange(ref peakField, current, peak) == peak) return;
        }
    }
}

public class MetricsSnapshot
{
    public DateTimeOffset UtcTime { get; set;  }
    public int ActiveSessions { get; set;  }
    public int ActiveRooms { get; set; }
    public int PeakRooms { get; set; }
    
    public double QueueWaitP95 { get; set; }
    public double QueueWaitMax { get; set; }

    public double RoomExecP95 { get; set; }
    public double RoomExecMax { get; set; }

    public double EndToEndP95 { get; set; }
    public double EndToEndMax { get; set; }
}
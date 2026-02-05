using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Server.Util;

public static class Metrics
{
    private static long _activeSessions;
    private static long _activeRooms;
    private static long _peakRooms;
    private static long _exceptionsInterval;

    private static readonly ConcurrentQueue<double> _loopMs = new();
    private const int LoopWindowMax = 2000;
    private const int Interval = 60;
    
    public static void IncreaseSession() => Interlocked.Increment(ref _activeSessions);
    public static void DecreaseSession() => Interlocked.Decrement(ref _activeSessions);

    public static void IncreaseRoom()
    {
        var rooms = Interlocked.Increment(ref _activeRooms);
        UpdatePeak(ref _peakRooms, rooms);
    }
    
    public static void DecreaseRoom() => Interlocked.Decrement(ref _activeRooms);
    
    public static void IncreaseException() => Interlocked.Increment(ref _exceptionsInterval);
    
    public static void RecordLoopMs(double ms)
    {
        _loopMs.Enqueue(ms);
        while (_loopMs.Count > LoopWindowMax && _loopMs.TryDequeue(out _)) { }
    }

    public static Snapshot TakeSnapshotAndReset()
    {
        var sessions = (int)Interlocked.Read(ref _activeSessions);
        var rooms = (int)Interlocked.Read(ref _activeRooms);
        var peakRooms = (int)Interlocked.Read(ref _peakRooms);
        var exceptions = (int)Interlocked.Exchange(ref _exceptionsInterval, 0);
        var p95 = CalcP95(_loopMs);
        
        return new Snapshot(DateTimeOffset.UtcNow, sessions, rooms, peakRooms, exceptions, p95);
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

    private static void UpdatePeak(ref long peakField, long current)
    {
        while (true)
        {
            long peak = Interlocked.Read(ref peakField);
            if (current <= peak) return;
            if (Interlocked.CompareExchange(ref peakField, current, peak) == peak) return;
        }
    }

    public readonly record struct Snapshot(
        DateTimeOffset UtcTime, int ActiveSessions, int ActiveRooms, int PeakRooms, int ExceptionsInterval, double LoopP95Ms);
}
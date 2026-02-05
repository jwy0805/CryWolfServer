using Microsoft.Extensions.Logging;

namespace Server.Util;

public sealed class MetricsReporter : IDisposable
{
    private readonly DailyFileAppender _file;
    private readonly int _intervalMs;

    public MetricsReporter(string logDir, int intervalMs)
    {
        _file = new DailyFileAppender(logDir);
        _intervalMs = intervalMs;
    }

    public void Run()
    {
        while (true)
        {
            try
            {
                var s = Metrics.TakeSnapshotAndReset();
                var line = $"[METRIC] t={s.UtcTime:O} activeSessions={s.ActiveSessions} activeRooms={s.ActiveRooms} " +
                           $"peakRooms={s.PeakRooms} exceptions={s.ExceptionsInterval} loopP95={s.LoopP95Ms:0.0}ms";
                
                _file.AppendLine(DateTime.UtcNow, line);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[METRIC REPORTER ERROR] {e}");
            }
            
            Thread.Sleep(_intervalMs);
        }
    }
    
    public void Dispose()
    {
        _file.Dispose();
    }
}
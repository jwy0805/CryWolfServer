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
                var s = Metrics.TakeSnapshot();
                var line = $"[METRIC] t={s.UtcTime:O} activeSessions={s.ActiveSessions} activeRooms={s.ActiveRooms} " +
                           $"peakRooms={s.PeakRooms}" +
                           $"queueWaitP95={s.QueueWaitP95:F2}ms " +
                           $"roomExecP95={s.RoomExecP95:F2}ms " +
                           $"endToEndP95={s.EndToEndP95:F2}ms " +
                           $"queueWaitMax={s.QueueWaitMax:F2}ms " +
                           $"roomExecMax={s.RoomExecMax:F2}ms " +
                           $"endToEndMax={s.EndToEndMax:F2}ms";
                
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
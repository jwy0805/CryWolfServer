using System.Text;

namespace Server.Util;

public sealed class DailyFileAppender : IDisposable
{
    private readonly string _dir;
    private readonly object _lock = new();

    private DateTime _currentDateUtc;
    private StreamWriter? _writer;

    public DailyFileAppender(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(_dir);
        SwitchIfNeeded(DateTime.UtcNow.Date);
    }

    public void AppendLine(DateTime utcNow, string line)
    {
        lock (_lock)
        {
            SwitchIfNeeded(utcNow.Date);
            _writer?.WriteLine(line);
            _writer?.Flush();
        }
    }
    
    private void SwitchIfNeeded(DateTime dateUtc)
    {
        if (_writer != null && dateUtc == _currentDateUtc) return;
        
        _writer?.Dispose();
        _currentDateUtc = dateUtc;

        var path = Path.Combine(_dir, "socket-metric-{_currentDateUtc:yyyy-MM-dd}.log");
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read),
            Encoding.UTF8)
        {
            AutoFlush = true
        };
    }
    
    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
namespace Server.Game;

public class Scheduler
{
    private readonly Dictionary<Guid, CancellationTokenSource> _tasks = new();
    
    public async Task ScheduleEvent(long delayInSeconds, Action action)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(delayInSeconds));
        action();
    }
    
    public Guid ScheduleCancellableEvent(long delayInMilliSeconds, Action action)
    {
        var cts = new CancellationTokenSource();
        var taskId = Guid.NewGuid();
        _tasks[taskId] = cts;
        
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delayInMilliSeconds), cts.Token);
                if (cts.Token.IsCancellationRequested) return;
                action();
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
            finally
            {
                _tasks.Remove(taskId);
            }
        }, cts.Token);

        return taskId;
    }
    
    public void CancelEvent(Guid taskId)
    {
        if (_tasks.TryGetValue(taskId, out var cancelTokenSource) == false) return;
        cancelTokenSource.Cancel();
        _tasks.Remove(taskId);
    }
}
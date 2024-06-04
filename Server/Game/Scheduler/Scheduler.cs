namespace Server.Game;

public class Scheduler
{
    public async Task ScheduleEvent(long delayInSeconds, Action action)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(delayInSeconds));
        action();
    }
}
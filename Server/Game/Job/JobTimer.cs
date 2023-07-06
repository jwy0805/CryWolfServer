using ServerCore;

namespace Server.Game;

struct JobTimerElem : IComparable<JobTimerElem>
{
    public int execTick;
    public IJob job;

    public int CompareTo(JobTimerElem other)
    {
        return other.execTick - execTick;
    }
}

public class JobTimer
{
    private PriorityQueue<JobTimerElem> _priorityQueue = new();
    private readonly object _lock = new();

    public void Push(IJob job, int tickAfter = 0)
    {
        JobTimerElem jobElement;
        jobElement.execTick = Environment.TickCount + tickAfter;
        jobElement.job = job;

        lock (_lock)
        {
            _priorityQueue.Push(jobElement);
        }
    }

    public void Flush()
    {
        while (true)
        {
            int now = Environment.TickCount;
            JobTimerElem jobElement;

            lock (_lock)
            {
                if (_priorityQueue.Count == 0) break;
                jobElement = _priorityQueue.Peek();
                if (jobElement.execTick > now) break;

                _priorityQueue.Pop();
            }
            
            jobElement.job.Execute();
        }
    }
}
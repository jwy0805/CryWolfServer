using ServerCore;

namespace Server.Game;

internal struct JobTimerElem : IComparable<JobTimerElem>
{
    public int ExecTick;
    public IJob Job;

    public int CompareTo(JobTimerElem other)
    {
        return other.ExecTick - ExecTick;
    }
}

public class JobTimer
{
    private readonly PriorityQueue<JobTimerElem> _priorityQueue = new();
    private readonly object _lock = new();

    public void Push(IJob job, int tickAfter = 0)
    {
        JobTimerElem jobElement;
        jobElement.ExecTick = Environment.TickCount + tickAfter;
        jobElement.Job = job;

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
                if (jobElement.ExecTick > now) break;

                _priorityQueue.Pop();
            }
            
            jobElement.Job.Execute();
        }
    }
}
using System.Collections.Concurrent;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Server.Game;

public class RoomActorScheduler : IDisposable
{
    private readonly int _workerCount;
    private readonly ConcurrentQueue<GameRoom>[] _queues;
    private readonly AutoResetEvent[] _signals;
    private readonly Thread[] _threads;
    private volatile bool _stop;

    public RoomActorScheduler(int workerCount)
    {
        _workerCount = Math.Max(1, workerCount);
        _queues = new ConcurrentQueue<GameRoom>[_workerCount];
        _signals = new AutoResetEvent[_workerCount];
        _threads = new Thread[_workerCount];

        for (int i = 0; i < _workerCount; i++)
        {
            _queues[i] = new ConcurrentQueue<GameRoom>();
            _signals[i] = new AutoResetEvent(false);

            int index = i;
            _threads[i] = new Thread(() => WorkerLoop(index))
            {
                IsBackground = true,
                Name = $"RoomWorker-{index}"
            };
            _threads[i].Start();
        }
    }

    private int GetWorkerIndex(int roomId) => (roomId & 0x7fffffff) % _workerCount;
    
    // Request room execution -> 같은 룸이 워커 큐에 중복으로 들어가지 않도록 CAS로 보호
    public void Schedule(GameRoom room)
    {
        if (room == null) return;
        if (Interlocked.CompareExchange(ref room._scheduled, 1, 0) != 0) return;

        int workerIndex = GetWorkerIndex(room.RoomId);
        _queues[workerIndex].Enqueue(room);
        _signals[workerIndex].Set();
    }
    
    private void WorkerLoop(int idx)
    {
        var queue = _queues[idx];
        var signal = _signals[idx];

        while (!_stop)
        {
            if (!queue.TryDequeue(out var room))
            {
                signal.WaitOne(1);
                continue;
            }

            try
            {
                if (room.IsShuttingDown) continue;
                Interlocked.Exchange(ref room._tickPending, 0);
                room.Update();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[RoomWorker-{idx}] Room.Update error (RoomId={room.RoomId}): {e}");
            }
            finally
            {
                Interlocked.Exchange(ref room._scheduled, 0);
            }

            if (room.IsShuttingDown) continue;
            
            // 실행 중 드랍된 Schedule 요청 복구
            bool tickRequestedWhileRunning = 
                Interlocked.CompareExchange(ref room._tickPending, 1, 1) == 1;
            if (room.HasPendingJobs || tickRequestedWhileRunning)
            {
                Schedule(room);
            }
        }
    }
    
    public void Dispose()
    {
        _stop = true;
        for (int i = 0; i < _workerCount; i++)
        {
            _signals[i].Set();
        }

        for (int i = 0; i < _workerCount; i++)
        {
            _threads[i].Join();
        }

        for (int i = 0; i < _workerCount; i++)
        {
            _signals[i].Dispose();
        }
    }
}
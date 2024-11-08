namespace ServerCore;

public class PriorityQueue<T> where T : IComparable<T>
{
    // Max Heap
    private readonly List<T> _heap = new();
    public int Count => _heap.Count;
    
    public void Push(T data)
    {
        _heap.Add(data);

        int now = _heap.Count - 1;
        while (now > 0)
        {
            int next = (now - 1) / 2;
            if (_heap[now].CompareTo(_heap[next]) < 0) break;

            (_heap[now], _heap[next]) = (_heap[next], _heap[now]);
            now = next;
        }
    }

    public T Pop()
    {
        T ret = _heap[0];
        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        lastIndex--;

        int now = 0;
        while (true)
        {
            int left = 2 * now + 1;
            int right = 2 * now + 2;
            int next = now;

            if (left <= lastIndex && _heap[next].CompareTo(_heap[left]) < 0)
                next = left;
            if (right <= lastIndex && _heap[next].CompareTo(_heap[right]) < 0)
                next = right;
            if (next == now) break;
            
            (_heap[now], _heap[next]) = (_heap[next], _heap[now]);
            now = next;
        }

        return ret;
    }

    public T Peek()
    {
        return _heap.Count == 0 ? default(T) : _heap[0];
    }
}
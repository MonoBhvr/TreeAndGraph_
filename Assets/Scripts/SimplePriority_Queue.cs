using System;
using System.Collections.Generic;

public class SimplePriorityQueue<T>
{
    struct Node { public T item; public float priority; public Node(T i, float p) { item = i; priority = p; } }
    List<Node> heap = new List<Node>();
    Dictionary<T, int> positions = new Dictionary<T, int>();

    public int Count => heap.Count;

    public void Clear()
    {
        heap.Clear();
        positions.Clear();
    }

    public bool Contains(T item) => positions.ContainsKey(item);

    public void Enqueue(T item, float priority)
    {
        if (positions.TryGetValue(item, out int idx))
        {
            if (priority < heap[idx].priority)
            {
                heap[idx] = new Node(item, priority);
                HeapifyUp(idx);
            }
            return;
        }
        int i = heap.Count;
        heap.Add(new Node(item, priority));
        positions[item] = i;
        HeapifyUp(i);
    }

    public T Dequeue()
    {
        if (heap.Count == 0) throw new InvalidOperationException("Queue is empty");
        T ret = heap[0].item;
        RemoveAt(0);
        return ret;
    }

    public T Peek()
    {
        if (heap.Count == 0) throw new InvalidOperationException("Queue is empty");
        return heap[0].item;
    }

    void RemoveAt(int idx)
    {
        int last = heap.Count - 1;
        positions.Remove(heap[idx].item);
        if (idx == last)
        {
            heap.RemoveAt(last);
            return;
        }
        heap[idx] = heap[last];
        positions[heap[idx].item] = idx;
        heap.RemoveAt(last);
        if (!HeapifyDown(idx)) HeapifyUp(idx);
    }

    bool HeapifyDown(int i)
    {
        int count = heap.Count;
        int cur = i;
        while (true)
        {
            int left = cur * 2 + 1;
            int right = left + 1;
            int smallest = cur;
            if (left < count && heap[left].priority < heap[smallest].priority) smallest = left;
            if (right < count && heap[right].priority < heap[smallest].priority) smallest = right;
            if (smallest == cur) break;
            Swap(cur, smallest);
            cur = smallest;
        }
        return cur != i;
    }

    void HeapifyUp(int i)
    {
        int cur = i;
        while (cur > 0)
        {
            int parent = (cur - 1) / 2;
            if (heap[cur].priority >= heap[parent].priority) break;
            Swap(cur, parent);
            cur = parent;
        }
    }

    void Swap(int a, int b)
    {
        var tmp = heap[a];
        heap[a] = heap[b];
        heap[b] = tmp;
        positions[heap[a].item] = a;
        positions[heap[b].item] = b;
    }
}

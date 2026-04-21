using System;
using System.Collections.Generic;

namespace Registration.ApplicationCode.Other;

public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> _data = new List<T>();

    public void Enqueue(T item)
    {
        _data.Add(item);
        _data.Sort(); // Ascending; if you want descending, sort descending
    }

    public T Dequeue()
    {
        if (_data.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        T item = _data[0]; // Smallest item (highest priority if ascending)
        _data.RemoveAt(0);
        return item;
    }

    public T[] GetData()
    {
        return _data.ToArray();
    }

    public int Count => _data.Count;
}


// C# code to implement the approach
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataStructures : ScriptableObject
{
    public class PriorityQueue<T>
    {
        private readonly SortedDictionary<int, Queue<T>> _list;
        private int _count;

        public PriorityQueue()
        {
            _list = new SortedDictionary<int, Queue<T>>();
        }

        public void Push(T item, int priority)
        {
            if (!_list.ContainsKey(priority))
            {
                _list[priority] = new Queue<T>();
            }
            _list[priority].Enqueue(item);
            _count++;
        }

        public T Pop()
        {
            if (_count == 0) throw new InvalidOperationException("Queue is empty.");
            var firstKey = _list.First();
            T item = firstKey.Value.Dequeue();
            if (firstKey.Value.Count == 0) _list.Remove(firstKey.Key);
            _count--;
            return item;
        }

        public bool IsEmpty => _count == 0;
    }


}
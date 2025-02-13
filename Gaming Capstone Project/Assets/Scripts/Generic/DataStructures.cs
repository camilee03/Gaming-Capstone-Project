using System;
using System.Collections.Generic;
using System.Runtime;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.EventSystems.EventTrigger;

public class DataStructures : MonoBehaviour
{
    class PriorityQueue {
        SortedList<HeapKey, HeapKey> heap;
        int count;
        PriorityQueue()
        {
            this.heap = new SortedList<HeapKey, HeapKey>();
            this.count = 0;
        }

        void push(HeapKey item, int priority)
        {
            List<object> entry = new List<object> { priority, this.count, item };
            // push the entry item onto the heap
            this.count++;
        }
        HeapKey pop() 
        {
            //List<object> exit = pop from heap
            // return exit[2]

        }

        bool isEmpty()
        {
            return this.heap.Count == 0;
        }
        void update(HeapKey item, int priority)
        {
            // If item already in priority queue with higher priority, update its priority and rebuild the heap.
            // If item already in priority queue with equal or lower priority, do nothing.
            // If item not in priority queue, do the same thing as self.push.
            for (SortedList<HeapKey, HeapKey> list = this.heap; list.Count > 0;)
            {
                if (list[2] == item)
                {
                    if (list[0] < priority) { break; }

                    // delete this.heap[index]
                    // this.heap.append((priority, c, item))
                    // heapq.heapify(self.heap)
                    // break
                }
                else
                {
                    this.push(item, priority);
                }
            }
        }
    }

    class HeapKey : IComparable<HeapKey>
    {
        public HeapKey(Guid id, Int32 value)
        {
            Id = id;
            Value = value;
        }

        public Guid Id { get; private set; }
        public Int32 Value { get; private set; }

        public int CompareTo(HeapKey other)
        {
            if (_enableCompareCount)
            {
                ++_compareCount;
            }

            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            var result = Value.CompareTo(other.Value);

            return result == 0 ? Id.CompareTo(other.Id) : result;
        }
    }
}

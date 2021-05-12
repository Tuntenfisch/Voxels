using System;
using System.Collections.Generic;

namespace Generics
{
    public class SetQueue<T> where T : class
    {
        public int Count => m_items.Count;

        private readonly Queue<T> m_items;
        private readonly HashSet<T> m_lookup;

        public SetQueue()
        {
            m_items = new Queue<T>();
            m_lookup = new HashSet<T>();
        }

        public void Enqueue(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (m_lookup.Contains(item))
            {
                return;
            }

            m_items.Enqueue(item);
            m_lookup.Add(item);
        }

        public T Dequeue()
        {
            if (Count < 1)
            {
                throw new InvalidOperationException("Tried to dequeue an item while the underlying data structure is empty.");
            }

            T item = m_items.Dequeue();
            m_lookup.Remove(item);

            return item;
        }

        public bool TryDequeue(out T item)
        {
            if (Count == 0)
            {
                item = default;

                return false;
            }

            item = Dequeue();

            return true;
        }

        public void Clear()
        {
            m_items.Clear();
            m_lookup.Clear();
        }
    }
}
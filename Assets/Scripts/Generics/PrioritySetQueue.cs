using System.Collections.Generic;

namespace Generics
{
    public class PrioritySetQueue<T>
    {
        public int Count => m_items.Count;

        private readonly Queue<T> m_items;
        private readonly HashSet<T> m_lookup;

        public PrioritySetQueue()
        {
            m_items = new Queue<T>();
            m_lookup = new HashSet<T>();
        }

        public void Enqueue(T item)
        {
            if (m_lookup.Contains(item))
            {
                return;
            }

            m_items.Enqueue(item);
            m_lookup.Add(item);
        }

        public T Dequeue()
        {
            T item = m_items.Dequeue();
            m_lookup.Remove(item);

            return item;
        }

        public void Clear()
        {
            m_items.Clear();
            m_lookup.Clear();
        }
    }
}
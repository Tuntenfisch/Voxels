using System;
using System.Collections.Generic;

namespace Tuntenfisch.Generics.Pool
{
    public class ObjectPool<T> where T : IPoolable
    {
        private readonly Stack<T> m_available;
        private readonly Func<T> m_generator;

        public ObjectPool(Func<T> generator, int initialPopulation = 0)
        {
            m_available = new Stack<T>(initialPopulation);
            m_generator = generator;
            Populate(initialPopulation);
        }

        public T Acquire()
        {
            T obj;

            if (m_available.Count == 0)
            {
                Populate(1);
            }
            obj = m_available.Pop();
            obj.OnAcquire();

            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            obj.OnRelease();
            m_available.Push(obj);
        }

        public void Populate(int count)
        {
            for (int index = 0; index < count; index++)
            {
                m_available.Push(m_generator());
            }
        }
    }
}
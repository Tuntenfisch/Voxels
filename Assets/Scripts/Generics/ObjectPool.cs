using System;
using System.Collections.Generic;

namespace Generics
{
    public class ObjectPool<T>
    {
        private readonly List<T> m_objects;
        private readonly Func<T> m_objectGenerator;

        public ObjectPool(Func<T> objectGenerator, int initialCapacity = 10)
        {
            m_objects = new List<T>(initialCapacity);
            m_objectGenerator = objectGenerator;
        }

        public T Acquire()
        {
            T obj;

            if (m_objects.Count == 0)
            {
                obj = m_objectGenerator();
            }
            else
            {
                int index = m_objects.Count - 1;
                obj = m_objects[index];
                m_objects.RemoveAt(index);
            }

            return obj;
        }

        public void Release(T obj)
        {
            m_objects.Add(obj);
        }
    }
}
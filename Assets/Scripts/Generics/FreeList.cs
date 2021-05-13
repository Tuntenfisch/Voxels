using System.Collections.Generic;

namespace Tuntenfisch.Generics
{
    // Based on https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det/48384354#48384354.
    public class FreeList<T>
    {
        public int Count => m_items.Count;

        private readonly List<FreeItem> m_items;
        private int m_firstFreeIndex;

        public FreeList()
        {
            m_items = new List<FreeItem>();
            m_firstFreeIndex = -1;
        }

        public T this[int index]
        {
            get => m_items[index].Item;
            set => m_items[index] = FreeItem.Create(value, m_items[index].NextFreeIndex);
        }

        public int Insert(T item)
        {
            if (m_firstFreeIndex != -1)
            {
                int index = m_firstFreeIndex;
                m_firstFreeIndex = m_items[m_firstFreeIndex].NextFreeIndex;
                m_items[index] = FreeItem.Create(item);

                return index;

            }
            else
            {
                FreeItem freeItem = FreeItem.Create(item);
                m_items.Add(freeItem);

                return m_items.Count - 1;
            }
        }

        public void Erase(int index)
        {
            m_items[index] = new FreeItem
            {
                NextFreeIndex = m_firstFreeIndex
            };
            m_firstFreeIndex = index;
        }

        public void Clear()
        {
            m_items.Clear();
            m_firstFreeIndex = -1;
        }

        // There are no C style unions in C# and we cannot use field offsets of 0
        // to emulate a union because it doesn't work for generic types...
        private struct FreeItem
        {
            public T Item { get; set; }
            public int NextFreeIndex { get; set; }

            public static FreeItem Create(T item = default, int nextFreeIndex = -1)
            {
                return new FreeItem
                {
                    Item = item,
                    NextFreeIndex = nextFreeIndex
                };
            }
        }
    }
}
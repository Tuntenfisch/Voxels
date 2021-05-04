using Unity.Mathematics;

namespace Generics.Octree
{
    public struct OctreeNode
    {
        public bool IsLeaf => Count > -1;
        // Index indexes into the tree's children list if this node is not a leaf.
        // Index indexes into the tree's item node list if this node is a leaf.
        public int Index { get; set; }
        public int Count { get; set; }

        public static OctreeNode Create(int index = -1, int count = 0)
        {
            return new OctreeNode
            {
                Index = index,
                Count = count
            };
        }
    }

    internal struct OctreeItem<T>
    {
        public T Item { get; set; }
        public float3 Position { get; set; }
    }

    // Singly linked list of item nodes for our tree.
    internal struct OctreeItemNode
    {
        public int ItemIndex { get; set; }
        public int NextItemNodeIndex { get; set; }

        public static OctreeItemNode Create(int itemIndex = -1, int nextItemNodeIndex = -1)
        {
            return new OctreeItemNode
            {
                ItemIndex = itemIndex,
                NextItemNodeIndex = nextItemNodeIndex
            };
        }
    }
}
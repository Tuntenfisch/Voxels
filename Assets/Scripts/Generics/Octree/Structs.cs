using Unity.Mathematics;

namespace Tuntenfisch.Generics.Octree
{
    public struct OctreeNode
    {
        public bool IsLeaf => Count > -1;
        // Index indexes into the tree's children list if this node is not a leaf.
        // Index indexes into the tree's item node list if this node is a leaf.
        public int Index { get; set; }
        public int Count { get; set; }

        public OctreeNode(int index = -1, int count = 0)
        {
            Index = index;
            Count = count;
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

        public OctreeItemNode(int itemIndex = -1, int nextItemNodeIndex = -1)
        {
            ItemIndex = itemIndex;
            NextItemNodeIndex = nextItemNodeIndex;
        }
    }
}
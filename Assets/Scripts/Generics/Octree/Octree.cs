using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tuntenfisch.Generics.Octree
{
    // Based on https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det/48384354#48384354.
    public class Octree<T>
    {
        private Bounds m_bounds;

        private readonly List<OctreeNode> m_nodes;
        private readonly FreeList<OctreeItem<T>> m_items;
        private readonly FreeList<OctreeItemNode> m_itemNodes;

        private readonly int m_maxDepth;
        private readonly int m_maxElementsPerNode;
        private int m_firstFreeNodeIndex;

        // Stacks used for some methods internally to avoid dynamically allocating memory.
        private readonly Stack<int> m_internalStack0;
        private readonly Stack<Bounds> m_internalStack1;
        private OctreeFlags m_flags;

        public Octree(Bounds bounds, int maxDepth, int maxElementsPerNode)
        {
            if (maxDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, $"Paremeter {nameof(maxDepth)} must be positive.");
            }

            if (maxElementsPerNode < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxElementsPerNode), maxElementsPerNode, $"Parameter {nameof(maxElementsPerNode)} must be positive.");
            }

            m_bounds = bounds;

            m_nodes = new List<OctreeNode> { new OctreeNode() }; // Root node is always stored at index 0.
            m_items = new FreeList<OctreeItem<T>>();
            m_itemNodes = new FreeList<OctreeItemNode>();

            m_maxDepth = maxDepth;
            m_maxElementsPerNode = maxElementsPerNode;
            m_firstFreeNodeIndex = -1;

            m_internalStack0 = new Stack<int>();
            m_internalStack1 = new Stack<Bounds>();
        }

        public void Clear()
        {
            m_nodes.Clear();
            m_nodes.Add(new OctreeNode());
            m_items.Clear();
            m_itemNodes.Clear();
            m_firstFreeNodeIndex = -1;
        }

        public void Cleanup()
        {
            if (m_flags.HasFlag(OctreeFlags.Traversing))
            {
                throw new InvalidOperationException("Cannot call this method while octree is being traversed.");
            }

            if (m_nodes[0].IsLeaf)
            {
                return;
            }

            m_internalStack0.Clear();
            m_internalStack0.Push(0);

            while (m_internalStack0.Count > 0)
            {
                int nodeIndex = m_internalStack0.Pop();
                OctreeNode node = m_nodes[nodeIndex];
                int numberOfEmptyLeaves = 0;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    OctreeNode child = m_nodes[node.Index + childIndexOffset];

                    if (child.IsLeaf)
                    {
                        numberOfEmptyLeaves += child.Count == 0 ? 1 : 0;
                    }
                    else
                    {
                        m_internalStack0.Push(node.Index + childIndexOffset);
                    }
                }

                if (numberOfEmptyLeaves == 8)
                {
                    // We "abuse" the node's index field to implement our singly linked list. The same
                    // working principle as in FreeList<T> applies (difference being the stride is 8 instead of 1).
                    m_nodes[node.Index] = new OctreeNode(m_firstFreeNodeIndex, 0);
                    m_firstFreeNodeIndex = node.Index;
                    m_nodes[nodeIndex] = new OctreeNode();
                }
            }
        }

        public void Traverse(Action<OctreeNode, Bounds> onNodeTraverseAction)
        {
            if (onNodeTraverseAction == null)
            {
                throw new ArgumentNullException(nameof(onNodeTraverseAction));
            }

            if (m_flags.HasFlag(OctreeFlags.Traversing))
            {
                throw new InvalidOperationException("Already traversing octree.");
            }

            m_flags |= OctreeFlags.Traversing;

            m_internalStack0.Clear();
            m_internalStack1.Clear();
            m_internalStack0.Push(0);
            m_internalStack1.Push(m_bounds);

            while (m_internalStack0.Count > 0)
            {
                int nodeIndex = m_internalStack0.Pop();
                OctreeNode node = m_nodes[nodeIndex];
                Bounds nodeBounds = m_internalStack1.Pop();
                onNodeTraverseAction(node, nodeBounds);

                if (!node.IsLeaf)
                {
                    for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                    {
                        Bounds childBounds = GetChildBounds(nodeBounds, childIndexOffset);
                        m_internalStack0.Push(node.Index + childIndexOffset);
                        m_internalStack1.Push(childBounds);
                    }
                }
            }
            m_flags &= ~OctreeFlags.Traversing;
        }

        public void Remove(int itemIndex)
        {
            if (m_flags.HasFlag(OctreeFlags.Traversing))
            {
                throw new InvalidOperationException("Cannot call this method while octree is being traversed.");
            }

            OctreeItem<T> octreeItem = m_items[itemIndex];

            foreach (int leafIndex in FindLeaves(octreeItem.Position, 0, m_bounds))
            {
                OctreeNode leaf = m_nodes[leafIndex];
                int indexPtr = leaf.Index;
                int previousIndex = -1;

                while (indexPtr != -1 && m_itemNodes[indexPtr].ItemIndex != itemIndex)
                {
                    previousIndex = indexPtr;
                    indexPtr = m_itemNodes[indexPtr].NextItemNodeIndex;
                }

                if (indexPtr != -1)
                {
                    int nextIndex = m_itemNodes[indexPtr].NextItemNodeIndex;

                    if (previousIndex == -1)
                    {
                        // The item was located at the start of the node's singly linked list.
                        // Simply update the nodes index to the next index.
                        leaf.Index = nextIndex;
                    }
                    else
                    {
                        // The item was located somewhere in the middle or end of the node's singly linked list.
                        OctreeItemNode itemNode = m_itemNodes[previousIndex];
                        itemNode.NextItemNodeIndex = nextIndex;
                        m_itemNodes[previousIndex] = itemNode;
                    }
                    m_itemNodes.Erase(indexPtr);
                    leaf.Count--;
                }
                m_nodes[leafIndex] = leaf;
            }
            m_items.Erase(itemIndex);
        }

        public int Insert(T item, float3 position)
        {
            if (m_flags.HasFlag(OctreeFlags.Traversing))
            {
                throw new InvalidOperationException("Cannot call this method while octree is being traversed.");
            }

            if (!m_bounds.Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, $"Parameter {nameof(position)} is outside of octree bounds.");
            }

            // Store this item in our list of items.
            int itemIndex = m_items.Insert(new OctreeItem<T> { Item = item, Position = position });
            // Insert the item into our tree structure.
            OctreeNode root = m_nodes[0];
            Insert(itemIndex, ref root, m_bounds, 0);
            m_nodes[0] = root;

            return itemIndex;
        }

        private void Insert(int itemIndex, ref OctreeNode node, Bounds nodeBounds, int depth)
        {
            if (node.IsLeaf)
            {
                // If this is a leaf node, we either split the node if we don't have enough space...
                if (node.Count >= m_maxElementsPerNode && depth < m_maxDepth)
                {
                    Split(ref node, nodeBounds, depth);
                    Insert(itemIndex, ref node, nodeBounds, depth);
                }
                else
                {
                    // or we insert the item into the node itself.
                    InsertItemIntoNode(itemIndex, ref node);
                }
            }
            else
            {
                // If this is not a leaf node, we need to determine the appropriate child node to insert
                // the item into.
                OctreeItem<T> item = m_items[itemIndex];
                int childIndexOffset = GetChildIndexOffset(nodeBounds, item.Position);
                OctreeNode child = m_nodes[node.Index + childIndexOffset];
                Bounds childBounds = GetChildBounds(nodeBounds, childIndexOffset);
                Insert(itemIndex, ref child, childBounds, depth + 1);
                m_nodes[node.Index + childIndexOffset] = child;
            }
        }

        private int GetChildIndexOffset(Bounds parentBounds, float3 position)
        {
            int childIndexOffset = 0;

            childIndexOffset |= (position.x > parentBounds.center.x ? 1 : 0) << 0;
            childIndexOffset |= (position.y > parentBounds.center.y ? 1 : 0) << 1;
            childIndexOffset |= (position.z > parentBounds.center.z ? 1 : 0) << 2;

            return childIndexOffset;
        }

        private Bounds GetChildBounds(Bounds parentBounds, int childIndexOffset)
        {
            int x = (childIndexOffset >> 0) & 1;
            int y = (childIndexOffset >> 1) & 1;
            int z = (childIndexOffset >> 2) & 1;

            float3 center = (float3)parentBounds.min + 0.5f * (float3)parentBounds.extents + new float3(x * parentBounds.extents.x, y * parentBounds.extents.y, z * parentBounds.extents.z);

            return new Bounds(center, parentBounds.extents);
        }

        private void InsertItemIntoNode(int itemIndex, ref OctreeNode node)
        {
            Assert.IsTrue(node.IsLeaf);

            OctreeItemNode itemNode = new OctreeItemNode(itemIndex);
            int itemNodeIndex = m_itemNodes.Insert(itemNode);

            if (node.Count == 0)
            {
                node.Index = itemNodeIndex;
            }
            else
            {
                OctreeItemNode ptr = m_itemNodes[node.Index];

                while (ptr.NextItemNodeIndex != -1)
                {
                    ptr = m_itemNodes[ptr.NextItemNodeIndex];
                }
                ptr.NextItemNodeIndex = itemNodeIndex;
            }
            node.Count++;
        }

        private void Split(ref OctreeNode node, Bounds nodeBounds, int depth)
        {
            // Create new children.
            int firstChildIndex;

            if (m_firstFreeNodeIndex != -1)
            {
                firstChildIndex = m_firstFreeNodeIndex;
                m_firstFreeNodeIndex = m_nodes[m_firstFreeNodeIndex].Index;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    m_nodes[firstChildIndex + childIndexOffset] = new OctreeNode();
                }
            }
            else
            {
                firstChildIndex = m_nodes.Count;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    m_nodes.Add(new OctreeNode());
                }
            }

            // Reinsert existing item nodes into the appropriate children.
            int indexPtr = node.Index;
            OctreeItemNode ptr;

            while (indexPtr != -1)
            {
                // Get the next item node and earse it from the tree's item node
                // list because the corresponding item will be reinserted into
                // the appropriate child node.
                ptr = m_itemNodes[indexPtr];
                m_itemNodes.Erase(indexPtr);

                // Determine the appropriate child to insert the existing item
                // and insert it.
                OctreeItem<T> octreeItem = m_items[ptr.ItemIndex];
                int childIndexOffset = GetChildIndexOffset(nodeBounds, octreeItem.Position);
                OctreeNode child = m_nodes[firstChildIndex + childIndexOffset];
                Bounds childBounds = GetChildBounds(nodeBounds, childIndexOffset);
                Insert(ptr.ItemIndex, ref child, childBounds, depth + 1);
                m_nodes[firstChildIndex + childIndexOffset] = child;

                // Advance to the next item node.
                indexPtr = ptr.NextItemNodeIndex;
            }
            // Convert this node to an intermediate node.
            node.Index = firstChildIndex;
            node.Count = -1;
        }

        private IEnumerable<int> FindLeaves(float3 position, int nodeIndex, Bounds nodeBounds)
        {
            OctreeNode node = m_nodes[nodeIndex];

            if (node.IsLeaf)
            {
                yield return nodeIndex;
            }
            else
            {
                int childIndexOffset = GetChildIndexOffset(nodeBounds, position);
                Bounds childBounds = GetChildBounds(nodeBounds, childIndexOffset);

                foreach (int leafIndex in FindLeaves(position, node.Index + childIndexOffset, childBounds))
                {
                    yield return leafIndex;
                }
            }
        }

        [Flags]
        private enum OctreeFlags
        {
            Traversing = 1
        }
    }
}
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace World
{
    internal class LodTree
    {
        public Bounds Bounds => m_nodes[0].Bounds;
        public Node this[int index] { get => m_nodes[index]; }

        private readonly List<Node> m_nodes;
        private int m_firstFreeNodeIndex;
        private int m_maxDepth;

        private LodTreeFlags m_flags;

        // Stack used for some methods internally to avoid dynamically allocating memory.
        private readonly Stack<int> m_internalStack0;

        public LodTree()
        {
            m_nodes = new List<Node>();
            m_internalStack0 = new Stack<int>();
        }

        public void Initialize(Bounds bounds, int maxDepth)
        {
            if (m_flags.HasFlag(LodTreeFlags.Traversing))
            {
                throw new InvalidOperationException("Cannot call this method while traversing!");
            }

            if (maxDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "Max depth must be positive!");
            }

            m_nodes.Clear();
            m_nodes.Add(Node.Create(bounds, 0));
            m_firstFreeNodeIndex = -1;
            m_maxDepth = maxDepth;
        }

        public IEnumerable<Node> Traverse(bool onlyLeaves = false)
        {
            if (m_flags.HasFlag(LodTreeFlags.Traversing))
            {
                throw new InvalidOperationException("Already traversing!");
            }

            m_flags |= LodTreeFlags.Traversing;

            m_internalStack0.Clear();
            m_internalStack0.Push(0);

            while (m_internalStack0.Count > 0)
            {
                int nodeIndex = m_internalStack0.Pop();
                Node node = m_nodes[nodeIndex];

                if (!node.IsLeaf)
                {
                    for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                    {
                        m_internalStack0.Push(node.FirstChildIndex + childIndexOffset);
                    }
                }

                if (onlyLeaves && !node.IsLeaf)
                {
                    continue;
                }

                yield return node;
            }
            m_flags &= ~LodTreeFlags.Traversing;
        }

        public IEnumerable<Node> Query(Bounds bounds)
        {
            throw new NotImplementedException();
        }

        public void Update(float3 viewerPosition)
        {
            if (m_flags.HasFlag(LodTreeFlags.Traversing))
            {
                throw new InvalidOperationException("Cannot call this method while traversing!");
            }

            Node root = m_nodes[0];
            Update(ref root, 0, viewerPosition);
            m_nodes[0] = root;
        }

        private void Update(ref Node node, int nodeIndex, float3 viewerPosition)
        {
            Assert.IsFalse(m_flags.HasFlag(LodTreeFlags.Traversing));

            if (math.lengthsq((float3)node.Bounds.center - viewerPosition) <= node.Bounds.size.x * node.Bounds.size.x)
            {
                if (node.Depth < m_maxDepth)
                {
                    Split(ref node, nodeIndex);
                }

                if (!node.IsLeaf)
                {
                    for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                    {
                        int childIndex = node.FirstChildIndex + childIndexOffset;
                        Node child = m_nodes[childIndex];
                        Update(ref child, childIndex, viewerPosition);
                        m_nodes[childIndex] = child;
                    }
                }
            }
            else
            {
                MergeChildren(ref node);
            }
        }

        private void Split(ref Node parentNode, int parentNodeIndex)
        {
            Assert.IsFalse(m_flags.HasFlag(LodTreeFlags.Traversing));

            if (!parentNode.IsLeaf)
            {
                return;
            }

            // Create new children.
            int firstChildIndex;

            if (m_firstFreeNodeIndex != -1)
            {
                firstChildIndex = m_firstFreeNodeIndex;
                m_firstFreeNodeIndex = m_nodes[m_firstFreeNodeIndex].FirstChildIndex;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    m_nodes[firstChildIndex + childIndexOffset] = Node.Create(GetChildBounds(parentNode.Bounds, childIndexOffset), parentNode.Depth + 1, parentNodeIndex);
                }
            }
            else
            {
                firstChildIndex = m_nodes.Count;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    m_nodes.Add(Node.Create(GetChildBounds(parentNode.Bounds, childIndexOffset), parentNode.Depth + 1, parentNodeIndex));
                }
            }
            parentNode.FirstChildIndex = firstChildIndex;
        }

        private void MergeChildren(ref Node parentNode)
        {
            Assert.IsFalse(m_flags.HasFlag(LodTreeFlags.Traversing));

            if (parentNode.IsLeaf)
            {
                return;
            }

            for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
            {
                int childIndex = parentNode.FirstChildIndex + childIndexOffset;
                Node child = m_nodes[childIndex];
                MergeChildren(ref child);
                m_nodes[childIndex] = child;
            }

            Node firstChild = m_nodes[parentNode.FirstChildIndex];
            firstChild.FirstChildIndex = m_firstFreeNodeIndex;
            m_nodes[parentNode.FirstChildIndex] = firstChild;
            m_firstFreeNodeIndex = parentNode.FirstChildIndex;
            parentNode.FirstChildIndex = -1;
        }

        private Bounds GetChildBounds(Bounds parentBounds, int childIndexOffset)
        {
            int x = (childIndexOffset >> 0) & 1;
            int y = (childIndexOffset >> 1) & 1;
            int z = (childIndexOffset >> 2) & 1;

            float3 center = (float3)parentBounds.min + 0.5f * (float3)parentBounds.extents + new float3(x * parentBounds.extents.x, y * parentBounds.extents.y, z * parentBounds.extents.z);

            return new Bounds(center, parentBounds.extents);
        }

        public struct Node
        {
            public bool IsRoot => ParentIndex == -1;
            public bool IsLeaf => FirstChildIndex == -1;
            public Bounds Bounds { get; set; }
            public int Depth { get; set; }
            public int ParentIndex { get; set; }
            public int FirstChildIndex { get; set; }

            public static Node Create(Bounds bounds, int depth, int parentIndex = -1, int firstChildIndex = -1)
            {
                return new Node
                {
                    Bounds = bounds,
                    Depth = depth,
                    ParentIndex = parentIndex,
                    FirstChildIndex = firstChildIndex
                };
            }
        }

        [Flags]
        private enum LodTreeFlags
        {
            Traversing = 1
        }
    }
}
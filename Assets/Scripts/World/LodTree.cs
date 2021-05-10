using Generics.Pool;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace World
{
    internal class LodTree : IPoolable
    {
        public Bounds Bounds
        {
            get => m_bounds;

            set
            {
                if (m_bounds == value)
                {
                    return;
                }

                m_bounds = value;
                m_flags |= LodTreeFlags.isDirty;
            }
        }
        public int MaxDepth
        {
            get => m_maxDepth;

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxDepth), value, "Max depth must be positive!");
                }

                m_maxDepth = value;
                m_flags |= LodTreeFlags.isDirty;
            }
        }
        public float InflationFactor
        {
            get => m_inflationFactor;

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(InflationFactor), value, "Inflation factor must be at least 1!");
                }

                m_inflationFactor = value;
                m_flags |= LodTreeFlags.isDirty;
            }
        }
        public float3 DistanceFactor
        {
            get => m_distanceFactor;

            set
            {
                if (math.any(value < 0))
                {
                    throw new ArgumentOutOfRangeException(nameof(DistanceFactor), value, "LOD distance factor must be positive!");
                }

                m_distanceFactor = value;
                m_flags |= LodTreeFlags.isDirty;
            }
        }

        private Bounds m_bounds;
        private readonly List<Node> m_nodes;
        private int m_firstFreeNodeIndex;
        private int m_maxDepth;
        private float m_inflationFactor;
        private float3 m_distanceFactor;

        // Stacks used for some methods internally to avoid dynamically allocating memory.
        private readonly Stack<int> m_internalStack0;
        private readonly Stack<Bounds> m_internalStack1;

        private LodTreeFlags m_flags;

        void IPoolable.OnAcquire() { }

        void IPoolable.OnRelease() => Clear();

        public LodTree()
        {
            m_nodes = new List<Node>
            {
                Node.Create(0)
            };
            m_firstFreeNodeIndex = -1;
            m_internalStack0 = new Stack<int>();
            m_internalStack1 = new Stack<Bounds>();
        }

        public IEnumerable<(Node, Bounds)> Traverse(bool onlyLeaves = false)
        {
            if (m_flags.HasFlag(LodTreeFlags.isDirty))
            {
                m_flags &= ~LodTreeFlags.isDirty;
                Clear();
            }

            m_internalStack0.Clear();
            m_internalStack0.Push(0);
            m_internalStack1.Clear();
            m_internalStack1.Push(m_bounds);

            while (m_internalStack0.Count > 0)
            {
                int nodeIndex = m_internalStack0.Pop();
                Node node = m_nodes[nodeIndex];
                Bounds bounds = m_internalStack1.Pop();

                if (!node.IsLeaf)
                {
                    for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                    {
                        m_internalStack0.Push(node.FirstChildIndex + childIndexOffset);
                        m_internalStack1.Push(GetChildBounds(bounds.center, node.Depth, childIndexOffset));
                    }

                    if (onlyLeaves)
                    {
                        continue;
                    }
                }

                yield return (node, bounds);
            }
        }

        public void Clear()
        {
            foreach ((Node leaf, _) in Traverse(true))
            {
                if (leaf.Chunk != null)
                {
                    World.SharedChunkPool.Release(leaf.Chunk);
                }
            }
            m_nodes.Clear();
            m_nodes.Add(Node.Create(0));
            m_firstFreeNodeIndex = -1;
        }

        public void Update(float3 viewerPosition)
        {
            if (m_flags.HasFlag(LodTreeFlags.isDirty))
            {
                m_flags &= ~LodTreeFlags.isDirty;
                Clear();
            }

            // Update tree structure.
            Node root = m_nodes[0];
            Update(ref root, new Bounds(m_bounds.center, m_inflationFactor * m_bounds.size), viewerPosition);
            m_nodes[0] = root;
        }

        private void Update(ref Node node, Bounds bounds, float3 viewerPosition)
        {
            // Split criterium.
            if (math.lengthsq(m_distanceFactor * (viewerPosition - (float3)bounds.center)) <= math.lengthsq(bounds.extents))
            {
                // Only split if we aren't at maximum depth already.
                if (node.Depth < m_maxDepth)
                {
                    Split(ref node);

                    for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                    {
                        int childIndex = node.FirstChildIndex + childIndexOffset;
                        Node child = m_nodes[childIndex];
                        Bounds childBounds = GetInflatedChildBounds(bounds.center, node.Depth, childIndexOffset);
                        Update(ref child, childBounds, viewerPosition);
                        m_nodes[childIndex] = child;
                    }
                }
            }
            else if (!node.IsLeaf) // If we don't fullfil the split citerium we need to merge (assuming we are not a leaf already).
            {
                MergeChildren(ref node);
            }

            if (node.IsLeaf && node.Chunk == null)
            {
                float3 center = bounds.center;
                int lod = m_maxDepth - node.Depth;

                node.Chunk = World.SharedChunkPool.Acquire((chunk) =>
                {
                    chunk.transform.position = center;
                    chunk.Lod = lod;
                });
                World.VoxelVolume.GenerateVoxelVolume(node.Chunk);
                World.DualContouring.RequestMeshGeneration(node.Chunk);
            }
        }

        private void Split(ref Node node)
        {
            // If this nose is an intermediate node, it's already split. Do nothing.
            if (!node.IsLeaf)
            {
                return;
            }

            // Does this node have a chunk referenced? If yes, release it. It will
            // be replaced by the new children.
            if (node.Chunk != null)
            {
                World.SharedChunkPool.Release(node.Chunk);
                node.Chunk = null;
            }

            // Create the new children.
            if (m_firstFreeNodeIndex != -1)
            {
                node.FirstChildIndex = m_firstFreeNodeIndex;
                m_firstFreeNodeIndex = m_nodes[m_firstFreeNodeIndex].FirstChildIndex;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    m_nodes[node.FirstChildIndex + childIndexOffset] = Node.Create(node.Depth + 1);
                }
            }
            else
            {
                node.FirstChildIndex = m_nodes.Count;

                for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
                {
                    m_nodes.Add(Node.Create(node.Depth + 1));
                }
            }
        }

        private void MergeChildren(ref Node node)
        {
            if (node.IsLeaf)
            {
                World.SharedChunkPool.Release(node.Chunk);
                node.Chunk = null;

                return;
            }

            // It might be the case that this parent node's children have children of their own.
            // Recursively merge them...
            for (int childIndexOffset = 0; childIndexOffset < 8; childIndexOffset++)
            {
                int childIndex = node.FirstChildIndex + childIndexOffset;
                Node child = m_nodes[childIndex];
                MergeChildren(ref child);
                m_nodes[childIndex] = child;
            }

            Node firstChild = m_nodes[node.FirstChildIndex];
            firstChild.FirstChildIndex = m_firstFreeNodeIndex;
            m_nodes[node.FirstChildIndex] = firstChild;
            m_firstFreeNodeIndex = node.FirstChildIndex;
            node.FirstChildIndex = -1;
        }

        private Bounds GetChildBounds(float3 parentBoundsCenter, int parentDepth, int childIndexOffset)
        {
            int3 xyz = new int3(childIndexOffset >> 0, childIndexOffset >> 1, childIndexOffset >> 2) & 1;
            float3 parentBoundsExtent = m_bounds.extents / (1 << parentDepth);
            float3 parentBoundsMin = parentBoundsCenter - parentBoundsExtent;
            float3 center = parentBoundsMin + 0.5f * parentBoundsExtent + xyz * parentBoundsExtent;

            return new Bounds(center, parentBoundsExtent);
        }

        private Bounds GetInflatedChildBounds(float3 parentBoundsCenter, int parentDepth, int childIndexOffset)
        {
            Bounds childBounds = GetChildBounds(parentBoundsCenter, parentDepth, childIndexOffset);

            return new Bounds(childBounds.center, m_inflationFactor * childBounds.size);
        }

        public struct Node
        {
            public bool IsLeaf => FirstChildIndex == -1;
            public int Depth { get; set; }
            public int FirstChildIndex { get; set; }
            public Chunk Chunk { get; set; }

            public static Node Create(int depth, int firstChildIndex = -1, Chunk chunk = null)
            {
                return new Node
                {
                    Depth = depth,
                    FirstChildIndex = firstChildIndex,
                    Chunk = chunk
                };
            }
        }

        [Flags]
        private enum LodTreeFlags
        {
            isDirty = 1
        }
    }
}
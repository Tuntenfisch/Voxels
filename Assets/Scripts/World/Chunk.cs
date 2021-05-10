using Generics;
using Generics.Pool;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Voxels;
using Voxels.Config;

namespace World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    internal class Chunk : MonoBehaviour, IVoxelVolume, IPoolable
    {
        public int Lod { get; set; }

        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;

        private ComputeBuffer m_voxelVolumeBuffer;
        private JobHandle m_bakeJobHandle;
        private ChunkFlags m_flags;

        (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing) IVoxelVolume.GetArguments() => (m_voxelVolumeBuffer, transform.position, VoxelConfigs.VoxelVolumeConfig.GetVoxelSpacing(Lod));

        void IVoxelVolume.OnMeshGenerated(NativeArray<Vertex>? nullableVertices, NativeArray<int>? nullableTriangles)
        {
            if (!nullableVertices.HasValue || !nullableTriangles.HasValue)
            {
                m_mesh.Clear();
                m_meshCollider.sharedMesh = null;

                return;
            }

            NativeArray<Vertex> vertices = nullableVertices.Value;
            NativeArray<int> triangles = nullableTriangles.Value;

            m_mesh.SetVertexBufferParams(vertices.Length, Vertex.Attributes);
            m_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            m_mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            m_mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
            m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
            m_mesh.RecalculateBounds();

            m_bakeJobHandle = new BakeJob(m_mesh.GetInstanceID()).Schedule();
            m_flags |= ChunkFlags.BakingMesh;
        }

        void IPoolable.OnAcquire() => gameObject.SetActive(true);

        void IPoolable.OnRelease() => gameObject.SetActive(false);

        private void Awake()
        {
            InitializeMeshComponents();
        }

        private void Update()
        {
            if (m_flags.HasFlag(ChunkFlags.BakingMesh) && m_bakeJobHandle.IsCompleted)
            {
                m_flags &= ~ChunkFlags.BakingMesh;
                OnMeshBaked();
            }
        }

        private void OnDestroy() => ReleaseBuffers();

        public void CreateBuffers(int numberOfVoxels)
        {
            if (m_voxelVolumeBuffer?.count == numberOfVoxels)
            {
                return;
            }

            ReleaseBuffers();

            m_voxelVolumeBuffer = new ComputeBuffer(numberOfVoxels, 4 * sizeof(float));
        }

        public void ReleaseBuffers()
        {
            if (m_voxelVolumeBuffer == null)
            {
                return;
            }

            m_voxelVolumeBuffer.Release();
            m_voxelVolumeBuffer = null;
        }

        private void InitializeMeshComponents()
        {
            m_mesh = new Mesh();
            m_mesh.MarkDynamic();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshFilter.sharedMesh = m_mesh;
            m_meshCollider = GetComponent<MeshCollider>();
            m_meshCollider.sharedMesh = m_mesh;
        }

        private void OnMeshBaked()
        {
            m_bakeJobHandle.Complete();
            m_meshCollider.sharedMesh = m_mesh;
        }

        [Flags]
        private enum ChunkFlags
        {
            BakingMesh = 1
        }
    }
}
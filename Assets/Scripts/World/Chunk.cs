using Generics;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Voxel;

namespace World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour, IMeshGenerationRequester
    {
        private ComputeBuffer m_voxelVolumeBuffer;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;
        private Mesh m_mesh;
        private JobHandle m_bakeJobHandle;
        private ChunkFlags m_flags;

        private void Start()
        {
            VoxelVolume.Instance.Configuration.OnDirty += OnConfigurationDirty;
            InitializeMeshComponents();
            CreateBuffers();
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                OnTransformChanged();
            }

            if (m_flags.HasFlag(ChunkFlags.BakingMesh) && m_bakeJobHandle.IsCompleted)
            {
                OnMeshBaked();
            }
        }

        private void OnDestroy()
        {
            if (VoxelVolume.Instance != null)
            {
                VoxelVolume.Instance.Configuration.OnDirty -= OnConfigurationDirty;
            }
            ReleaseBuffers();
        }

        public (ComputeBuffer voxelVolumeBuffer, Vector3 worldPosition) GetMeshGenerationArguments() => (m_voxelVolumeBuffer, transform.position);

        public void OnMeshGenerated(NativeArray<Vertex>? nullableVertices, NativeArray<int>? nullableTriangles)
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

        private void OnConfigurationDirty()
        {
            if (m_voxelVolumeBuffer.count != VoxelVolume.Instance.Configuration.NumberOfVoxels)
            {
                ReleaseBuffers();
                CreateBuffers();
            }
        }

        private void CreateBuffers()
        {
            if (m_voxelVolumeBuffer != null)
            {
                return;
            }

            m_voxelVolumeBuffer = new ComputeBuffer(VoxelVolume.Instance.Configuration.NumberOfVoxels, 4 * sizeof(float));
        }

        private void ReleaseBuffers()
        {
            if (m_voxelVolumeBuffer == null)
            {
                return;
            }

            m_voxelVolumeBuffer.Release();
            m_voxelVolumeBuffer = null;
        }

        private void OnTransformChanged()
        {
            transform.hasChanged = false;

            VoxelVolume.Instance.GenerateVoxelVolume(m_voxelVolumeBuffer, transform.position);
            CubicalMarchingSquares.Instance.RequestMeshGeneration(this);
        }

        private void OnMeshBaked()
        {
            m_flags &= ~ChunkFlags.BakingMesh;

            m_bakeJobHandle.Complete();
            m_meshCollider.sharedMesh = m_mesh;
        }

        private void InitializeMeshComponents()
        {
            m_mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshFilter.sharedMesh = m_mesh;
            m_meshCollider = GetComponent<MeshCollider>();
            m_meshCollider.sharedMesh = m_mesh;
        }

        [Flags]
        private enum ChunkFlags
        {
            BakingMesh = 1
        }
    }
}
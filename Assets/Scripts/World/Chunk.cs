using System;
using System.Collections;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Voxels;
using Tuntenfisch.Voxels.Config;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tuntenfisch.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    internal class Chunk : MonoBehaviour, IPoolable
    {
        public int Lod { get; set; }
        private bool HasPendingRequest => !m_handle?.Canceled ?? false;

        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;
        private ComputeBuffer m_voxelVolumeBuffer;
        private DualContouring.RequestHandle m_handle;

        void IPoolable.OnAcquire()
        {
            gameObject.SetActive(true);
        }

        void IPoolable.OnRelease()
        {
            if (HasPendingRequest)
            {
                ClearPendingRequest();
            }
            m_meshFilter.sharedMesh = null;
            m_meshCollider.sharedMesh = null;
            m_handle = null;
            gameObject.SetActive(false);
        }

        private void Awake() => InitializeMeshComponents();

        public void CreateBuffers(int numberOfVoxels)
        {
            if (m_voxelVolumeBuffer?.count == numberOfVoxels)
            {
                return;
            }

            ReleaseBuffers();
            m_voxelVolumeBuffer = new ComputeBuffer(numberOfVoxels, sizeof(float) + sizeof(uint));
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

        public void Generate()
        {
            if (HasPendingRequest)
            {
                ClearPendingRequest();
            }
            float voxelSpacing = VoxelConfigs.VoxelVolumeConfig.GetVoxelSpacing(Lod);
            World.VoxelVolume.GenerateVoxelVolume(m_voxelVolumeBuffer, transform.position, voxelSpacing);
            m_handle = World.DualContouring.RequestMeshAsync(m_voxelVolumeBuffer, transform.position, voxelSpacing, OnMeshGenerated);
        }

        private void OnMeshGenerated(NativeArray<Vertex> vertices, NativeArray<int> triangles)
        {
            m_handle = null;

            if (!gameObject.activeSelf)
            {
                return;
            }

            if (vertices.Length == 0 || triangles.Length == 0)
            {
                m_meshFilter.sharedMesh = null;
                m_meshCollider.sharedMesh = null;

                return;
            }

            m_mesh.SetVertexBufferParams(vertices.Length, Vertex.Attributes);
            m_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            m_mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            m_mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
            m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
            m_mesh.RecalculateBounds();

            StartCoroutine(BakeMesh());
        }

        private IEnumerator BakeMesh()
        {
            JobHandle handle = new BakeJob(m_mesh.GetInstanceID()).Schedule();

            while (!handle.IsCompleted)
            {
                yield return null;
            }

            m_meshFilter.sharedMesh = m_mesh;
            m_meshCollider.sharedMesh = m_mesh;
        }

        private void ClearPendingRequest()
        {
            m_handle.Cancel();
            m_handle = null;
        }

        private void InitializeMeshComponents()
        {
            m_mesh = new Mesh();
            m_mesh.MarkDynamic();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshCollider = GetComponent<MeshCollider>();
        }
    }
}
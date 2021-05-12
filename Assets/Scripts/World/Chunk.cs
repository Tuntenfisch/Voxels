using Generics;
using Generics.Pool;
using System.Collections;
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
        public int Lod { set { m_lod = value; } }

        private int m_lod;
        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;
        private ComputeBuffer m_voxelVolumeBuffer;

        (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing) IVoxelVolume.GetArguments()
        {
            return (m_voxelVolumeBuffer, transform.position, VoxelConfigs.VoxelVolumeConfig.GetVoxelSpacing(m_lod));
        }

        void IVoxelVolume.OnMeshGenerated(NativeArray<Vertex>? nullableVertices, NativeArray<int>? nullableTriangles)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (!nullableVertices.HasValue || !nullableTriangles.HasValue)
            {
                m_meshFilter.sharedMesh = null;
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

            StartCoroutine(BakeMesh());
        }

        void IPoolable.OnAcquire() => gameObject.SetActive(true);

        void IPoolable.OnRelease()
        {
            m_meshFilter.sharedMesh = null;
            m_meshCollider.sharedMesh = null;
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
            m_voxelVolumeBuffer = new ComputeBuffer(numberOfVoxels, 1 * sizeof(float) + 1 * sizeof(uint));
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
            m_meshCollider = GetComponent<MeshCollider>();
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
    }
}
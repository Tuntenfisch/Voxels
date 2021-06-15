using System.Collections;
using System.Collections.Generic;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Generics.Request;
using Tuntenfisch.Voxels.DC;
using Tuntenfisch.Voxels.Volume;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Tuntenfisch.Voxels.CSG;

namespace Tuntenfisch.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour, IPoolable
    {
        public int Lod { get; set; }

        private bool HasPendingRequest => !m_requestHandle?.Canceled ?? false;

        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private MeshCollider m_meshCollider;

        private ComputeBuffer m_voxelVolumeBuffer;
        private RequestHandle m_requestHandle;
        private JobHandle m_bakeJobHandle;
        private List<GPUVoxelVolumeCSGOperation> m_voxelVolumeCSGOperations;


        private void Awake()
        {
            InitializeMeshComponents();
            m_voxelVolumeCSGOperations = new List<GPUVoxelVolumeCSGOperation>();
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, WorldManager.VoxelConfig.VoxelVolumeConfig.VoxelVolumeDimensions);
        }

        private void LateUpdate()
        {
            if (m_voxelVolumeCSGOperations.Count > 0 && !HasPendingRequest)
            {
                WorldManager.VoxelVolume.ApplyVoxelVolumeCSGOperations(m_voxelVolumeBuffer, transform.position, m_voxelVolumeCSGOperations);
                m_voxelVolumeCSGOperations.Clear();
                Remeshify();
            }
        }

        public void OnAcquire() => gameObject.SetActive(true);

        public void OnRelease()
        {
            if (HasPendingRequest)
            {
                m_requestHandle.Cancel();
                m_requestHandle = null;
            }
            m_meshFilter.sharedMesh = null;
            m_meshCollider.sharedMesh = null;
            m_requestHandle = null;
            m_voxelVolumeCSGOperations.Clear();
            gameObject.SetActive(false);
        }

        private void CreateBuffers()
        {
            if (m_voxelVolumeBuffer?.count != WorldManager.VoxelConfig.VoxelVolumeConfig.VoxelCount)
            {
                m_voxelVolumeBuffer?.Release();
                m_voxelVolumeBuffer = new ComputeBuffer(WorldManager.VoxelConfig.VoxelVolumeConfig.VoxelCount, sizeof(float) + sizeof(uint));
            }
        }

        private void ReleaseBuffers()
        {
            if (m_voxelVolumeBuffer != null)
            {
                m_voxelVolumeBuffer.Release();
                m_voxelVolumeBuffer = null;
            }
        }

        public void Regenerate()
        {
            CreateBuffers();

            WorldManager.VoxelVolume.GenerateVoxelVolume(m_voxelVolumeBuffer, transform.position);
        }

        public void Remeshify()
        {
            if (HasPendingRequest)
            {
                m_requestHandle.Cancel();
                m_requestHandle = null;
            }

            CreateBuffers();

            m_requestHandle = WorldManager.DualContouring.RequestMeshAsync(m_voxelVolumeBuffer, Lod, transform.position, OnMeshGenerated);
        }

        public void ApplyCSGPrimitiveOperation(GPUCSGOperator csgOperator, GPUCSGPrimitive csgPrimitive, Matrix4x4 worldToObjectMatrix)
        {
            m_voxelVolumeCSGOperations.Add(new GPUVoxelVolumeCSGOperation(csgOperator, csgPrimitive, worldToObjectMatrix));
        }

        private void OnMeshGenerated(int vertexCount, int triangleCount, NativeArray<GPUVertex> vertices, NativeArray<int> triangles)
        {
            m_requestHandle = null;

            if (vertexCount == 0 || triangleCount == 0)
            {
                m_meshFilter.sharedMesh = null;
                m_meshCollider.sharedMesh = null;

                return;
            }

            m_mesh.SetVertexBufferParams(vertexCount, GPUVertex.Attributes);
            m_mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
            m_mesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);
#if !UNITY_EDITOR
            m_mesh.SetIndexBufferData(triangles, 0, 0, triangleCount, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
#else
            m_mesh.SetIndexBufferData(triangles, 0, 0, triangleCount, MeshUpdateFlags.DontRecalculateBounds);
#endif
            m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount));
            m_mesh.RecalculateBounds();

            // Assign the mesh, in case it is null.
            if (m_meshFilter.sharedMesh == null)
            {
                m_meshFilter.sharedMesh = m_mesh;
            }
            StartCoroutine(BakeMeshCoroutine());
        }

        private IEnumerator BakeMeshCoroutine()
        {
            m_bakeJobHandle = new BakeJob(m_mesh.GetInstanceID()).Schedule();

            while (!m_bakeJobHandle.IsCompleted)
            {
                yield return null;
            }

            m_meshCollider.sharedMesh = m_mesh;
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
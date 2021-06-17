using System;
using System.Collections;
using System.Collections.Generic;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Generics.Request;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.DC;
using Tuntenfisch.Voxels.Volume;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

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
        private Coroutine m_processChunkFlagsCoroutine;
        private ChunkFlags m_flags;

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

        public void OnAcquire()
        {
            CreateBuffers();
            gameObject.SetActive(true);
        }

        public void OnRelease()
        {
            if (HasPendingRequest)
            {
                m_requestHandle.Cancel();
                m_requestHandle = null;
            }

            if (m_processChunkFlagsCoroutine != null)
            {
                StopCoroutine(m_processChunkFlagsCoroutine);
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

        public void RegenerateVoxelVolume() => ProcessChunkFlags(ChunkFlags.VoxelVolumeRegenerationRequested);

        public void RegenerateMesh() => ProcessChunkFlags(ChunkFlags.MeshRegenerationRequested);

        public void ApplyCSGPrimitiveOperation(GPUCSGOperator csgOperator, GPUCSGPrimitive csgPrimitive, Matrix4x4 worldToObjectMatrix)
        {
            m_voxelVolumeCSGOperations.Add(new GPUVoxelVolumeCSGOperation(csgOperator, csgPrimitive, worldToObjectMatrix));
            ProcessChunkFlags(ChunkFlags.CSGOperationPerformed | ChunkFlags.MeshRegenerationRequested);
        }

        private void ProcessChunkFlags(ChunkFlags flags)
        {
            m_flags |= flags;

            if (m_processChunkFlagsCoroutine == null)
            {
                m_processChunkFlagsCoroutine = StartCoroutine(ProcessChunkFlagsCoroutine());
            }
        }

        private IEnumerator ProcessChunkFlagsCoroutine()
        {
            while (m_flags != 0)
            {
                if (m_flags.HasFlag(ChunkFlags.VoxelVolumeRegenerationRequested))
                {
                    m_flags &= ~ChunkFlags.VoxelVolumeRegenerationRequested;
                    WorldManager.VoxelVolume.GenerateVoxelVolume(m_voxelVolumeBuffer, transform.position);
                }

                if (m_flags.HasFlag(ChunkFlags.CSGOperationPerformed))
                {
                    m_flags &= ~ChunkFlags.CSGOperationPerformed;
                    WorldManager.VoxelVolume.ApplyVoxelVolumeCSGOperations(m_voxelVolumeBuffer, transform.position, m_voxelVolumeCSGOperations);
                    m_voxelVolumeCSGOperations.Clear();
                }

                if
                (
                    m_flags.HasFlag(ChunkFlags.MeshRegenerationRequested) &&
                    !m_flags.HasFlag(ChunkFlags.MeshBakingRequested) &&
                    !m_flags.HasFlag(ChunkFlags.IsBakingMesh) &&
                    !HasPendingRequest
                )
                {
                    m_flags &= ~ChunkFlags.MeshRegenerationRequested;
                    m_requestHandle = WorldManager.DualContouring.RequestMeshAsync(m_voxelVolumeBuffer, Lod, transform.position, OnMeshGenerated);
                }

                if (m_flags.HasFlag(ChunkFlags.MeshBakingRequested))
                {
                    m_flags &= ~ChunkFlags.MeshBakingRequested;
                    m_bakeJobHandle = new BakeJob(m_mesh.GetInstanceID()).Schedule();
                    ProcessChunkFlags(ChunkFlags.IsBakingMesh);
                }

                if (m_flags.HasFlag(ChunkFlags.IsBakingMesh) && m_bakeJobHandle.IsCompleted)
                {
                    m_flags &= ~ChunkFlags.IsBakingMesh;
                    m_meshCollider.sharedMesh = m_mesh;
                }

                yield return null;
            }
            m_processChunkFlagsCoroutine = null;
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
            m_meshFilter.sharedMesh = m_mesh;
            ProcessChunkFlags(ChunkFlags.MeshBakingRequested);
        }

        private void InitializeMeshComponents()
        {
            m_mesh = new Mesh();
            m_mesh.MarkDynamic();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshCollider = GetComponent<MeshCollider>();
        }

        [Flags]
        private enum ChunkFlags
        {
            VoxelVolumeRegenerationRequested = 1,
            CSGOperationPerformed = 2,
            MeshRegenerationRequested = 4,
            MeshBakingRequested = 8,
            IsBakingMesh = 16
        }
    }
}
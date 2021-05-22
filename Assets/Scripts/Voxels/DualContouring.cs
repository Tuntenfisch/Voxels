using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Extensions;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Generics.Request;
using Tuntenfisch.Voxels.Config;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels
{
    public delegate void OnMeshGenerated(int vertexCount, int triangleCount, NativeArray<Vertex> vertices, NativeArray<int> triangles);

    [RequireComponent(typeof(VoxelConfigs))]
    public class DualContouring : MonoBehaviour
    {
        private event Action OnDestroyed;

        [Range(1, 16)]
        [SerializeField]
        private int m_numberOfWorkers = 4;

        private VoxelConfigs m_voxelConfigs;
        private Queue<(Request, OnMeshGenerated)> m_requests;
        private Stack<Worker> m_workers;
        private ObjectPool<Request> m_requestPool;

        private void Awake()
        {
            m_voxelConfigs = GetComponent<VoxelConfigs>();
#if UNITY_EDITOR
            m_voxelConfigs.DualContouringConfig.OnDirtied += ApplyDualContouringConfig;
            m_voxelConfigs.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
#endif
            m_requests = new Queue<(Request, OnMeshGenerated)>();
            m_workers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
            m_requestPool = new ObjectPool<Request>(() => { return new Request(); });

            ApplyDualContouringConfig();
            ApplyVoxelVolumeConfig();
        }

        private void Update()
        {
            while (m_requests.Count > 0 && m_workers.Count > 0)
            {
                (Request request, OnMeshGenerated callback) = m_requests.Dequeue();

                if (!request.Canceled)
                {
                    StartCoroutine(DispatchWorkerCoroutine(request, callback));
                }
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            m_voxelConfigs.DualContouringConfig.OnDirtied -= ApplyDualContouringConfig;
            m_voxelConfigs.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
#endif
            OnDestroyed?.Invoke();
        }

        private void OnValidate()
        {
            if (m_workers == null)
            {
                return;
            }

            foreach (Worker worker in m_workers)
            {
                worker.Dispose();
            }
            m_workers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
        }

        public RequestHandle RequestMeshAsync(ComputeBuffer voxelVolumeBuffer, int lod, float3 worldPosition, OnMeshGenerated callback)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            Request request = m_requestPool.Acquire((payload) =>
            {
                payload.VoxelVolumeBuffer = voxelVolumeBuffer;
                payload.Lod = lod;
                payload.WorldPosition = worldPosition;
            });
            m_requests.Enqueue((request, callback));

            return new RequestHandle(request);
        }

        private IEnumerator DispatchWorkerCoroutine(Request request, OnMeshGenerated callback)
        {
            Worker worker = m_workers.Pop();
            worker.GenerateMeshAsync(request.VoxelVolumeBuffer, request.WorldPosition, request.Lod);
            Worker.Status status;

            while ((status = worker.Process()) == Worker.Status.WaitingForGPUReadback && !request.Canceled)
            {
                yield return null;
            }

            switch (status)
            {
                case Worker.Status.GPUReadbackError:
                    Debug.LogError("GPU readback error detected.");
                    // If we encountered a GPU readback error, just try again.
                    m_requests.Enqueue((request, callback));
                    break;

                default:
                    // Only call the callback if the request isn't canceled.
                    if (!request.Canceled)
                    {
                        callback(worker.VertexCount, worker.TriangleCount, worker.Vertices, worker.Triangles);
                    }
                    break;
            }

            if (m_workers.Count < m_numberOfWorkers)
            {
                m_workers.Push(worker);
            }
            else
            {
                worker.Dispose();
            }
            m_requestPool.Release(request);
        }

        private void ApplyDualContouringConfig()
        {
            float cosOfSharpFeatureAngle = math.cos(math.radians(m_voxelConfigs.DualContouringConfig.SharpFeatureAngle));
            m_voxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.CosOfSharpFeatureAngle, cosOfSharpFeatureAngle);
            m_voxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.SchmitzParticleIterations, m_voxelConfigs.DualContouringConfig.SchmitzParticleIterations);
            m_voxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.SchmitzParticleStepSize, m_voxelConfigs.DualContouringConfig.SchmitzParticleStepSize);
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = m_voxelConfigs.VoxelVolumeConfig.NumberOfVoxels;
            m_voxelConfigs.DualContouringConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            m_voxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, m_voxelConfigs.VoxelVolumeConfig.VoxelSpacing);
        }

        private class Worker : IDisposable
        {
            public int VertexCount => m_counts[0];
            public int TriangleCount => 3 * m_counts[1];
            public NativeArray<Vertex> Vertices => m_vertices;
            public NativeArray<int> Triangles => m_triangles;

            private DualContouring m_parent;

            private AsyncComputeBuffer m_vertexBuffer;
            private AsyncComputeBuffer m_generatedVertexIndexLookupTable;
            private AsyncComputeBuffer m_triangleBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private NativeArray<Vertex> m_vertices;
            private NativeArray<int> m_triangles;
            private NativeArray<int> m_counts;

            public void Dispose()
            {
                ReleaseBuffers();
#if UNITY_EDITOR
                m_parent.m_voxelConfigs.VoxelVolumeConfig.OnDirtied -= CreateBuffers;
#endif
                m_parent.OnDestroyed -= Dispose;
                m_parent = null;
            }

            public Worker(DualContouring parent)
            {
                m_parent = parent;
#if UNITY_EDITOR
                m_parent.m_voxelConfigs.VoxelVolumeConfig.OnDirtied += CreateBuffers;
#endif
                m_parent.OnDestroyed += Dispose;
                CreateBuffers();
            }

            public Status Process()
            {
                if (m_countBuffer.IsDataAvailable())
                {
                    m_countBuffer.EndReadback();

                    if (m_countBuffer.HasError)
                    {
                        return Status.GPUReadbackError;
                    }

                    if (VertexCount == 0 || TriangleCount == 0)
                    {
                        return Status.Done;
                    }

                    // Retrieve vertices and triangles asynchronously.
                    m_vertexBuffer.StartReadbackNonAlloc(ref m_vertices, VertexCount);
                    m_triangleBuffer.StartReadbackNonAlloc(ref m_triangles, TriangleCount);
                }

                if (m_vertexBuffer.IsDataAvailable() && m_triangleBuffer.IsDataAvailable())
                {
                    m_vertexBuffer.EndReadback();
                    m_triangleBuffer.EndReadback();

                    if (m_vertexBuffer.HasError || m_triangleBuffer.HasError)
                    {
                        return Status.GPUReadbackError;
                    }

                    return Status.Done;
                }

                return Status.WaitingForGPUReadback;
            }

            public void GenerateMeshAsync(ComputeBuffer voxelVolumeBuffer, float3 voxelVolumeToWorldOffset, int lod)
            {
                SetupMeshGeneration(voxelVolumeBuffer, voxelVolumeToWorldOffset, m_parent.m_voxelConfigs.DualContouringConfig.GetCellStride(lod));

                m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(0, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(1, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(2, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells - 1);

                ComputeBuffer.CopyCount(m_vertexBuffer, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_triangleBuffer, m_countBuffer, sizeof(uint));
                m_countBuffer.StartReadbackNonAlloc(ref m_counts);
            }

            private void CreateBuffers()
            {
                if (m_vertexBuffer?.Count == m_parent.m_voxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices)
                {
                    return;
                }

                ReleaseBuffers();

                // Create CPU buffers.
                m_vertices = new NativeArray<Vertex>(m_parent.m_voxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices, Allocator.Persistent);
                m_triangles = new NativeArray<int>(3 * m_parent.m_voxelConfigs.VoxelVolumeConfig.MaxNumberOfTriangles, Allocator.Persistent);
                m_counts = new NativeArray<int>(2, Allocator.Persistent);

                // Create GPU buffers.
                m_vertexBuffer = new AsyncComputeBuffer(m_vertices.Length, Vertex.SizeInBytes, ComputeBufferType.Counter);
                m_generatedVertexIndexLookupTable = new AsyncComputeBuffer(m_parent.m_voxelConfigs.VoxelVolumeConfig.CellCount, sizeof(uint));
                m_triangleBuffer = new AsyncComputeBuffer(m_triangles.Length, sizeof(uint), ComputeBufferType.Append);
                m_countBuffer = new AsyncComputeBuffer(m_counts.Length, sizeof(uint), ComputeBufferType.Raw);
            }

            private void ReleaseBuffers()
            {
                if (m_vertexBuffer == null)
                {
                    return;
                }

                // Release CPU buffers.
                if (m_countBuffer.ReadbackInProgress)
                {
                    m_countBuffer.EndReadback();
                }
                m_counts.Dispose();

                if (m_vertexBuffer.ReadbackInProgress)
                {
                    m_vertexBuffer.EndReadback();
                }
                m_vertices.Dispose();

                if (m_triangleBuffer.ReadbackInProgress)
                {
                    m_triangleBuffer.EndReadback();
                }
                m_triangles.Dispose();

                // Release GPU buffers.
                m_vertexBuffer.Release();
                m_vertexBuffer = null;
                m_generatedVertexIndexLookupTable.Release();
                m_generatedVertexIndexLookupTable = null;
                m_triangleBuffer.Release();
                m_triangleBuffer = null;
                m_countBuffer.Release();
                m_countBuffer = null;
            }

            private void SetupMeshGeneration(ComputeBuffer voxelVolumeBuffer, float3 voxelVolumeToWorldOffset, int cellStride)
            {
                m_vertexBuffer.SetCounterValue(0);
                m_triangleBuffer.SetCounterValue(0);

                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)voxelVolumeToWorldOffset);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, cellStride);

                // Link buffer for kernel 0.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertices, m_vertexBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);

                // Link buffer for kernel 1.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);

                // Link buffer for kernel 2.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedVertices, m_vertexBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedTriangles, m_triangleBuffer);
            }

            public enum Status
            {
                WaitingForGPUReadback,
                GPUReadbackError,
                Done
            }
        }

        public class Request : IPoolable, IRequest
        {
            public bool Canceled => m_canceled;
            public ComputeBuffer VoxelVolumeBuffer { get; set; }
            public int Lod { get; set; }
            public float3 WorldPosition { get; set; }

            private bool m_canceled;

            void IPoolable.OnAcquire() { }

            void IPoolable.OnRelease()
            {
                VoxelVolumeBuffer = null;
                m_canceled = false;
            }

            public void Cancel() => m_canceled = true;
        }
    }
}
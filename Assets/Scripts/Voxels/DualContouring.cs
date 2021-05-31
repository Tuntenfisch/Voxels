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
                payload.VoxelVolumeToWorldSpaceOffset = worldPosition;
            });
            m_requests.Enqueue((request, callback));

            return new RequestHandle(request);
        }

        private IEnumerator DispatchWorkerCoroutine(Request request, OnMeshGenerated callback)
        {
            Worker worker = m_workers.Pop();
            worker.GenerateMeshAsync(request.VoxelVolumeBuffer, request.VoxelVolumeToWorldSpaceOffset, request.Lod);
            Worker.Status status;

            while ((status = worker.Process()) == Worker.Status.WaitingForGPUReadback)
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
                    // Only call the callback if the request hasn't been canceled.
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
            public NativeArray<Vertex> Vertices => m_generatedVertices;
            public NativeArray<int> Triangles => m_generatedTriangles;

            private DualContouring m_parent;

            private AsyncComputeBuffer m_generatedVerticesBuffer0;
            private AsyncComputeBuffer m_generatedVerticesBuffer1;
            private AsyncComputeBuffer m_generatedVerticesIndexLookupTable;
            private AsyncComputeBuffer m_generatedTrianglesBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private NativeArray<Vertex> m_generatedVertices;
            private NativeArray<int> m_generatedTriangles;
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
                    m_generatedVerticesBuffer0.StartReadbackNonAlloc(ref m_generatedVertices, VertexCount);
                    m_generatedTrianglesBuffer.StartReadbackNonAlloc(ref m_generatedTriangles, TriangleCount);
                }

                if (m_generatedVerticesBuffer0.IsDataAvailable() && m_generatedTrianglesBuffer.IsDataAvailable())
                {
                    m_generatedVerticesBuffer0.EndReadback();
                    m_generatedTrianglesBuffer.EndReadback();

                    if (m_generatedVerticesBuffer0.HasError || m_generatedTrianglesBuffer.HasError)
                    {
                        return Status.GPUReadbackError;
                    }

                    return Status.Done;
                }

                return Status.WaitingForGPUReadback;
            }

            public void GenerateMeshAsync(ComputeBuffer voxelVolumeBuffer, float3 voxelVolumeToWorldOffset, int lod)
            {
                m_generatedVerticesBuffer0.SetCounterValue(0);
                m_generatedTrianglesBuffer.SetCounterValue(0);

                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)voxelVolumeToWorldOffset);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, 1);

                // First we generate the inner cell vertices, i.e. all vertices which's cells do not reside on the surface of the voxel volume of this chunk.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVerticesIndexLookupTable, m_generatedVerticesIndexLookupTable);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(0, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells - 2);

                // Next we generate the desired level of detail for this chunk.
                // The safest way to go about level of detail is to first generate the mesh vertices at the highest lod (above dispatch call) and then merge those vertices
                // to create a lower lod. In order to leverage the GPU's parallelism we do this iteratively, similarly to how parallel reduction works.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVerticesIndexLookupTable, m_generatedVerticesIndexLookupTable);

                for (int cellStride = 2; cellStride <= (1 << lod); cellStride <<= 1)
                {
                    m_generatedVerticesBuffer1.SetCounterValue(0);

                    m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, cellStride);
                    // We need two buffers to merge the vertices. The first buffer acts as the source and the second as the destination.
                    m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                    m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertices1, m_generatedVerticesBuffer1);
                    m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(1, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells / cellStride);

                    // Swap the buffers, so during the next iteration the source buffer will be the current iteration's destination buffer.
                    (m_generatedVerticesBuffer0, m_generatedVerticesBuffer1) = (m_generatedVerticesBuffer1, m_generatedVerticesBuffer0);
                }

                // After the desired lod has been generated, we populate the outermost cells with vertices at the highest resolution. This will ensure that no
                // seams will be visible, resulting in a watertight mesh.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedVerticesIndexLookupTable, m_generatedVerticesIndexLookupTable);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(2, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells);

                // Finally, we triangulate the vertices to form the mesh.
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedVerticesIndexLookupTable, m_generatedVerticesIndexLookupTable);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedTriangles, m_generatedTrianglesBuffer);
                m_parent.m_voxelConfigs.DualContouringConfig.Compute.Dispatch(3, m_parent.m_voxelConfigs.VoxelVolumeConfig.NumberOfCells - 1);

                ComputeBuffer.CopyCount(m_generatedVerticesBuffer0, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_generatedTrianglesBuffer, m_countBuffer, sizeof(uint));
                m_countBuffer.StartReadbackNonAlloc(ref m_counts);
            }

            private void CreateBuffers()
            {
                if (m_generatedVerticesBuffer0?.Count == m_parent.m_voxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices)
                {
                    return;
                }

                ReleaseBuffers();

                // Create CPU buffers.
                m_generatedVertices = new NativeArray<Vertex>(m_parent.m_voxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices, Allocator.Persistent);
                m_generatedTriangles = new NativeArray<int>(3 * m_parent.m_voxelConfigs.VoxelVolumeConfig.MaxNumberOfTriangles, Allocator.Persistent);
                m_counts = new NativeArray<int>(2, Allocator.Persistent);

                // Create GPU buffers.
                m_generatedVerticesBuffer0 = new AsyncComputeBuffer(m_generatedVertices.Length, Vertex.SizeInBytes, ComputeBufferType.Counter);
                m_generatedVerticesBuffer1 = new AsyncComputeBuffer(m_generatedVertices.Length, Vertex.SizeInBytes, ComputeBufferType.Counter);
                m_generatedVerticesIndexLookupTable = new AsyncComputeBuffer(m_parent.m_voxelConfigs.VoxelVolumeConfig.CellCount, sizeof(uint));
                m_generatedTrianglesBuffer = new AsyncComputeBuffer(m_generatedTriangles.Length, sizeof(uint), ComputeBufferType.Append);
                m_countBuffer = new AsyncComputeBuffer(m_counts.Length, sizeof(uint), ComputeBufferType.Raw);
            }

            private void ReleaseBuffers()
            {
                if (m_generatedVerticesBuffer0 == null)
                {
                    return;
                }

                // We need to ensure none of the CPU buffers are currently in use before disposing them.
                if (m_generatedVerticesBuffer0.ReadbackInProgress)
                {
                    m_generatedVerticesBuffer0.EndReadback();
                }

                if (m_generatedVerticesBuffer1.ReadbackInProgress)
                {
                    m_generatedVerticesBuffer1.EndReadback();
                }

                if (m_generatedTrianglesBuffer.ReadbackInProgress)
                {
                    m_generatedTrianglesBuffer.EndReadback();
                }

                if (m_countBuffer.ReadbackInProgress)
                {
                    m_countBuffer.EndReadback();
                }

                // Dispose CPU buffers.
                m_generatedVertices.Dispose();
                m_generatedTriangles.Dispose();
                m_counts.Dispose();

                // Release GPU buffers.
                m_generatedVerticesBuffer0.Release();
                m_generatedVerticesBuffer0 = null;
                m_generatedVerticesBuffer1.Release();
                m_generatedVerticesBuffer1 = null;
                m_generatedVerticesIndexLookupTable.Release();
                m_generatedVerticesIndexLookupTable = null;
                m_generatedTrianglesBuffer.Release();
                m_generatedTrianglesBuffer = null;
                m_countBuffer.Release();
                m_countBuffer = null;
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
            public float3 VoxelVolumeToWorldSpaceOffset { get; set; }

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
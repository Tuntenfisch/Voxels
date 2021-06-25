using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Extensions;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Generics.Request;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.DC
{
    [RequireComponent(typeof(VoxelConfig))]
    public class DualContouring : MonoBehaviour
    {
        private event Action OnDestroyed;

        [Range(1, 8)]
        [SerializeField]
        private int m_numberOfWorkers = 4;

        private VoxelConfig m_voxelConfig;
        private Queue<(Request, OnMeshGenerated)> m_requests;
        private Stack<Worker> m_workers;
        private ObjectPool<Request> m_requestPool;

        private void Awake()
        {
            m_voxelConfig = GetComponent<VoxelConfig>();
            m_voxelConfig.DualContouringConfig.OnDirtied += ApplyDualContouringConfig;
            m_voxelConfig.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
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
            m_voxelConfig.DualContouringConfig.OnDirtied -= ApplyDualContouringConfig;
            m_voxelConfig.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
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

            Request request = m_requestPool.Acquire((request) =>
            {
                request.VoxelVolumeBuffer = voxelVolumeBuffer;
                request.Lod = lod;
                request.VoxelVolumeToWorldSpaceOffset = worldPosition;
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

                case Worker.Status.Retry:
                    m_requests.Enqueue((request, callback));
                    break;

                default:
                    // Only call the callback if the request hasn't been canceled.
                    if (!request.Canceled)
                    {
                        callback(worker.VertexCount, worker.TriangleCount, worker.Vertices, worker.Triangles);
                    }
                    m_requestPool.Release(request);
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
        }

        private void ApplyDualContouringConfig()
        {
            float cosOfHalfSharpFeatureAngle = math.cos(math.radians(0.5f * m_voxelConfig.DualContouringConfig.SharpFeatureAngle));
            m_voxelConfig.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.CosOfHalfSharpFeatureAngle, cosOfHalfSharpFeatureAngle);
            m_voxelConfig.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.SchmitzParticleIterations, m_voxelConfig.DualContouringConfig.SchmitzParticleIterations);
            m_voxelConfig.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.SchmitzParticleStepSize, m_voxelConfig.DualContouringConfig.SchmitzParticleStepSize);
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = m_voxelConfig.VoxelVolumeConfig.NumberOfVoxels;
            m_voxelConfig.DualContouringConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            m_voxelConfig.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, m_voxelConfig.VoxelVolumeConfig.VoxelSpacing);
        }

        private class Worker : IDisposable
        {
            public int VertexCount => m_counts[0];
            public int TriangleCount => 3 * m_counts[1];
            public NativeArray<GPUVertex> Vertices => m_generatedVertices;
            public NativeArray<int> Triangles => m_generatedTriangles;

            private DualContouring m_parent;

            private AsyncComputeBuffer m_cellVertexInfoLookupTableBuffer;
            private AsyncComputeBuffer m_generatedVerticesBuffer0;
            private AsyncComputeBuffer m_generatedVerticesBuffer1;
            private AsyncComputeBuffer m_generatedTrianglesBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private NativeArray<GPUVertex> m_generatedVertices;
            private NativeArray<int> m_generatedTriangles;
            private NativeArray<int> m_counts;

            public Worker(DualContouring parent)
            {
                m_parent = parent;
                m_parent.m_voxelConfig.VoxelVolumeConfig.OnDirtied += CreateBuffers;
                m_parent.OnDestroyed += Dispose;
                CreateBuffers();
            }

            public void Dispose()
            {
                ReleaseBuffers();
                m_parent.m_voxelConfig.VoxelVolumeConfig.OnDirtied -= CreateBuffers;
                m_parent.OnDestroyed -= Dispose;
                m_parent = null;
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

                    if (VertexCount > m_generatedVerticesBuffer0.Count)
                    {
                        // The mesh is larger than the buffer currently allocated. Enlarge buffers and try again.
                        int count = (int)math.round(1.5f * VertexCount);

                        m_generatedVertices.Dispose();
                        m_generatedVertices = new NativeArray<GPUVertex>(count, Allocator.Persistent);
                        m_generatedVerticesBuffer0.Release();
                        m_generatedVerticesBuffer0 = new AsyncComputeBuffer(count, GPUVertex.SizeInBytes, ComputeBufferType.Counter);
                        m_generatedVerticesBuffer1.Release();
                        m_generatedVerticesBuffer1 = new AsyncComputeBuffer(count, GPUVertex.SizeInBytes, ComputeBufferType.Counter);

                        return Status.Retry;
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

                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)voxelVolumeToWorldOffset);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, 1);

                // First we generate the inner cell vertices, i.e. all vertices which's cells do not reside on the surface of the voxel volume of this chunk.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(0, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells - 2);

                // Next we generate the desired level of detail for the previously generated vertices of this chunk.
                // The safest way to go about level of detail is to first generate the mesh vertices at the highest lod (above dispatch call) and then merge those vertices
                // to create a lower lod. In order to leverage the GPU's parallelism we do this iteratively, similarly to how parallel reduction works.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);

                for (int cellStride = 2; cellStride <= (1 << lod); cellStride <<= 1)
                {
                    m_generatedVerticesBuffer1.SetCounterValue(0);

                    m_parent.m_voxelConfig.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, cellStride);
                    // We need two buffers to merge the vertices. The first buffer acts as the source and the second as the destination.
                    m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                    m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertices1, m_generatedVerticesBuffer1);
                    m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(1, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells / cellStride);

                    // Swap the buffers, so during the next iteration the source buffer will be the previous iteration's destination buffer.
                    (m_generatedVerticesBuffer0, m_generatedVerticesBuffer1) = (m_generatedVerticesBuffer1, m_generatedVerticesBuffer0);
                }

                // After the desired lod has been generated, we populate the outermost cells with vertices at the highest level of detail. This will ensure that no
                // seams will be visible, resulting in a watertight mesh.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(2, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells);

                // Finally, we triangulate the vertices to form the mesh.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedTriangles, m_generatedTrianglesBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(3, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells - 1);

                ComputeBuffer.CopyCount(m_generatedVerticesBuffer0, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_generatedTrianglesBuffer, m_countBuffer, sizeof(uint));
                m_countBuffer.StartReadbackNonAlloc(ref m_counts);
            }

            private void CreateBuffers()
            {
                // Create CPU buffers.
                int generatedVertexCapacity = m_parent.m_voxelConfig.VoxelVolumeConfig.CellCount;

                if (!m_generatedVertices.IsCreated || m_generatedVertices.Length != generatedVertexCapacity)
                {
                    if (m_generatedVertices.IsCreated)
                    {
                        m_generatedVertices.Dispose();
                    }
                    m_generatedVertices = new NativeArray<GPUVertex>(generatedVertexCapacity, Allocator.Persistent);
                }

                int generatedTriangleCapacity = 3 * 6 * (int)math.round(math.pow(m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCellsAlongAxis - 1, 3));

                if (!m_generatedTriangles.IsCreated || m_generatedTriangles.Length != generatedTriangleCapacity)
                {
                    if (m_generatedTriangles.IsCreated)
                    {
                        m_generatedTriangles.Dispose();
                    }

                    m_generatedTriangles = new NativeArray<int>(generatedTriangleCapacity, Allocator.Persistent);
                }

                if (!m_counts.IsCreated || m_counts.Length != 2)
                {
                    if (m_counts.IsCreated)
                    {
                        m_counts.Dispose();
                    }
                    m_counts = new NativeArray<int>(2, Allocator.Persistent);
                }

                // Create GPU buffers.
                if (m_cellVertexInfoLookupTableBuffer?.Count != m_parent.m_voxelConfig.VoxelVolumeConfig.CellCount)
                {
                    m_cellVertexInfoLookupTableBuffer?.Release();
                    m_cellVertexInfoLookupTableBuffer = new AsyncComputeBuffer(m_parent.m_voxelConfig.VoxelVolumeConfig.CellCount, sizeof(uint));
                }

                if (m_generatedVerticesBuffer0?.Count != m_generatedVertices.Length)
                {
                    m_generatedVerticesBuffer0?.Release();
                    m_generatedVerticesBuffer0 = new AsyncComputeBuffer(m_generatedVertices.Length, GPUVertex.SizeInBytes, ComputeBufferType.Counter);
                }

                if (m_generatedVerticesBuffer1?.Count != m_generatedVertices.Length)
                {
                    m_generatedVerticesBuffer1?.Release();
                    m_generatedVerticesBuffer1 = new AsyncComputeBuffer(m_generatedVertices.Length, GPUVertex.SizeInBytes, ComputeBufferType.Counter);
                }

                if (m_generatedTrianglesBuffer?.Count != m_generatedTriangles.Length)
                {
                    m_generatedTrianglesBuffer?.Release();
                    m_generatedTrianglesBuffer = new AsyncComputeBuffer(m_generatedTriangles.Length, sizeof(uint), ComputeBufferType.Append);
                }

                if (m_countBuffer?.Count != m_counts.Length)
                {
                    m_countBuffer?.Release();
                    m_countBuffer = new AsyncComputeBuffer(m_counts.Length, sizeof(uint), ComputeBufferType.Raw);
                }
            }

            private void ReleaseBuffers()
            {
                // Dispose CPU buffers.
                if (m_generatedVertices.IsCreated)
                {
                    if (m_generatedVerticesBuffer0.ReadbackInProgress)
                    {
                        m_generatedVerticesBuffer0.EndReadback();
                    }
                    m_generatedVertices.Dispose();
                }

                if (m_generatedTriangles.IsCreated)
                {
                    if (m_generatedTrianglesBuffer.ReadbackInProgress)
                    {
                        m_generatedTrianglesBuffer.EndReadback();
                    }
                    m_generatedTriangles.Dispose();
                }

                if (m_counts.IsCreated)
                {
                    if (m_countBuffer.ReadbackInProgress)
                    {
                        m_countBuffer.EndReadback();
                    }
                    m_counts.Dispose();
                }

                // Release GPU buffers.
                if (m_cellVertexInfoLookupTableBuffer != null)
                {
                    m_cellVertexInfoLookupTableBuffer.Release();
                    m_cellVertexInfoLookupTableBuffer = null;
                }

                if (m_generatedVerticesBuffer0 != null)
                {
                    m_generatedVerticesBuffer0.Release();
                    m_generatedVerticesBuffer0 = null;
                }

                if (m_generatedVerticesBuffer1 != null)
                {
                    m_generatedVerticesBuffer1.Release();
                    m_generatedVerticesBuffer1 = null;
                }

                if (m_generatedTrianglesBuffer != null)
                {
                    m_generatedTrianglesBuffer.Release();
                    m_generatedTrianglesBuffer = null;
                }

                if (m_countBuffer != null)
                {
                    m_countBuffer.Release();
                    m_countBuffer = null;
                }
            }

            public enum Status
            {
                WaitingForGPUReadback,
                GPUReadbackError,
                Retry,
                Done
            }
        }

        private class Request : IPoolable, IRequest
        {
            public bool Canceled => m_canceled;
            public ComputeBuffer VoxelVolumeBuffer { get; set; }
            public int Lod { get; set; }
            public float3 VoxelVolumeToWorldSpaceOffset { get; set; }

            private bool m_canceled;

            public void OnAcquire() { }

            public void OnRelease()
            {
                VoxelVolumeBuffer = null;
                m_canceled = false;
            }

            public void Cancel() => m_canceled = true;
        }
    }
}
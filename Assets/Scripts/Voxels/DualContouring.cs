using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Extensions;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Voxels.Config;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels
{
    public delegate void OnMeshGenerated(NativeArray<Vertex> vertices, NativeArray<int> triangles);

    [RequireComponent(typeof(VoxelConfigs))]
    public class DualContouring : MonoBehaviour
    {
        private event Action OnDestroyed;

        [Range(1, 16)]
        [SerializeField]
        private int m_numberOfWorkers = 4;

        private Queue<(RequestHandle, Worker.Payload, OnMeshGenerated)> m_requests;
        private Stack<Worker> m_workers;
        private ObjectPool<Worker.Payload> m_payloadPool;

        private void Awake()
        {
#if UNITY_EDITOR
            VoxelConfigs.DualContouringConfig.OnDirtied += ApplyDualContouringConfig;
            VoxelConfigs.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
#endif
            m_requests = new Queue<(RequestHandle, Worker.Payload, OnMeshGenerated)>();
            m_workers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
            m_payloadPool = new ObjectPool<Worker.Payload>(() => { return new Worker.Payload(); });

            ApplyDualContouringConfig();
            ApplyVoxelVolumeConfig();
        }

        private void Update()
        {
            while (m_requests.Count > 0 && m_workers.Count > 0)
            {
                (RequestHandle handle, Worker.Payload payload, OnMeshGenerated callback) = m_requests.Dequeue();

                if (!handle.Canceled)
                {
                    StartCoroutine(DispatchWorkerCoroutine(handle, payload, callback));
                }
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            VoxelConfigs.DualContouringConfig.OnDirtied -= ApplyDualContouringConfig;
            VoxelConfigs.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
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

            RequestHandle handle = new RequestHandle();
            Worker.Payload payload = m_payloadPool.Acquire((payload) =>
            {
                payload.VoxelVolumeBuffer = voxelVolumeBuffer;
                payload.Lod = lod;
                payload.WorldPosition = worldPosition;
            });
            m_requests.Enqueue((handle, payload, callback));

            return handle;
        }

        private IEnumerator DispatchWorkerCoroutine(RequestHandle handle, Worker.Payload payload, OnMeshGenerated callback)
        {
            Worker worker = m_workers.Pop();
            worker.GenerateMeshAsync(payload);
            Worker.Status status;

            while ((status = worker.Process()) == Worker.Status.WaitingForGPUReadback && !handle.Canceled)
            {
                yield return null;
            }

            switch (status)
            {
                case Worker.Status.GPUReadbackError:
                    Debug.LogError("GPU readback error detected!");
                    break;

                default:
                    // Is the request still valid?
                    if (!handle.Canceled)
                    {
                        (NativeArray<Vertex> vertices, NativeArray<int> triangles) = worker.Data;
                        callback(vertices, triangles);
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
            m_payloadPool.Release(payload);
        }

        private void ApplyDualContouringConfig()
        {
            float cosOfSharpFeatureAngle = math.cos(math.radians(VoxelConfigs.DualContouringConfig.SharpFeatureAngle));
            VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.CosOfSharpFeatureAngle, cosOfSharpFeatureAngle);
            VoxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.SchmitzParticleIterations, VoxelConfigs.DualContouringConfig.SchmitzParticleIterations);
            VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.SchmitzParticleStepSize, VoxelConfigs.DualContouringConfig.SchmitzParticleStepSize);
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = VoxelConfigs.VoxelVolumeConfig.NumberOfVoxels;
            VoxelConfigs.DualContouringConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, VoxelConfigs.VoxelVolumeConfig.VoxelSpacing);
        }

        private class Worker : IDisposable
        {
            public (NativeArray<Vertex> vertices, NativeArray<int> triangles) Data => (m_vertices, m_triangles);

            private DualContouring m_parent;

            private AsyncComputeBuffer m_vertexBuffer;
            private AsyncComputeBuffer m_generatedVertexIndexLookupTable;
            private AsyncComputeBuffer m_triangleBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private NativeArray<Vertex> m_vertices;
            private NativeArray<int> m_triangles;

            public void Dispose()
            {
#if UNITY_EDITOR
                VoxelConfigs.VoxelVolumeConfig.OnDirtied -= CreateBuffers;
#endif
                ReleaseBuffers();
                m_parent.OnDestroyed -= Dispose;
                m_parent = null;
            }

            public Worker(DualContouring parent)
            {
#if UNITY_EDITOR
                VoxelConfigs.VoxelVolumeConfig.OnDirtied += CreateBuffers;
#endif
                m_parent = parent;
                m_parent.OnDestroyed += Dispose;
                CreateBuffers();
            }

            public Status Process()
            {
                if (m_countBuffer.IsDataAvailable())
                {
                    if (m_countBuffer.HasError)
                    {
                        return Status.GPUReadbackError;
                    }

                    NativeArray<int> counts = m_countBuffer.GetData<int>();

                    int vertexCount = counts[0];
                    int triangleCount = counts[1];

                    if (triangleCount == 0)
                    {
                        m_vertices = new NativeArray<Vertex>(0, Allocator.Temp);
                        m_triangles = new NativeArray<int>(0, Allocator.Temp);

                        return Status.Done;
                    }

                    // Retrieve vertices and triangles asynchronously.
                    m_vertexBuffer.RequestData(vertexCount);
                    m_triangleBuffer.RequestData(triangleCount);
                }

                if (m_vertexBuffer.IsDataAvailable() && m_triangleBuffer.IsDataAvailable())
                {
                    if (m_vertexBuffer.HasError || m_triangleBuffer.HasError)
                    {
                        return Status.GPUReadbackError;
                    }

                    m_vertices = m_vertexBuffer.GetData<Vertex>();
                    m_triangles = m_triangleBuffer.GetData<int>();

                    return Status.Done;
                }

                return Status.WaitingForGPUReadback;
            }

            public void GenerateMeshAsync(Payload payload)
            {
                ComputeBuffer voxelVolumeBuffer = payload.VoxelVolumeBuffer;
                float3 voxelVolumeToWorldOffset = payload.WorldPosition;
                int cellStride = VoxelConfigs.DualContouringConfig.GetCellStride(payload.Lod);

                SetupMeshGeneration(voxelVolumeBuffer, voxelVolumeToWorldOffset, cellStride);

                VoxelConfigs.DualContouringConfig.Compute.Dispatch(0, VoxelConfigs.VoxelVolumeConfig.NumberOfCells);
                VoxelConfigs.DualContouringConfig.Compute.Dispatch(1, VoxelConfigs.VoxelVolumeConfig.NumberOfCells - 1);

                ComputeBuffer.CopyCount(m_vertexBuffer, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_triangleBuffer, m_countBuffer, sizeof(int));
                m_countBuffer.RequestData();
            }

            private void CreateBuffers()
            {
                if (m_vertexBuffer?.Count == VoxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices)
                {
                    return;
                }

                ReleaseBuffers();
                m_vertexBuffer = new AsyncComputeBuffer(VoxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices, Vertex.SizeInBytes, ComputeBufferType.Counter);
                m_generatedVertexIndexLookupTable = new AsyncComputeBuffer(VoxelConfigs.VoxelVolumeConfig.CellCount, sizeof(int));
                m_triangleBuffer = new AsyncComputeBuffer(VoxelConfigs.VoxelVolumeConfig.MaxNumberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
                m_countBuffer = new AsyncComputeBuffer(2, sizeof(int), ComputeBufferType.Raw);
            }

            private void ReleaseBuffers()
            {
                if (m_vertexBuffer == null)
                {
                    return;
                }

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

                VoxelConfigs.DualContouringConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldOffset, (Vector3)voxelVolumeToWorldOffset);
                VoxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, cellStride);

                // Link buffer for kernel 0.
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertices, m_vertexBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);

                // Link buffer for kernel 1.
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertices, m_vertexBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.GeneratedTriangles, m_triangleBuffer);
            }

            public class Payload : IPoolable
            {
                public ComputeBuffer VoxelVolumeBuffer { get; set; }
                public int Lod { get; set; }
                public float3 WorldPosition { get; set; }

                void IPoolable.OnAcquire() { }

                void IPoolable.OnRelease() => VoxelVolumeBuffer = null;
            }

            public enum Status
            {
                WaitingForGPUReadback,
                GPUReadbackError,
                Done
            }
        }

        public class RequestHandle
        {
            public bool Canceled => m_canceled;

            private bool m_canceled;

            public void Cancel() => m_canceled = true;
        }
    }
}
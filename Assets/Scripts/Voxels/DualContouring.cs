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

        [Range(1, 8)]
        [SerializeField]
        private int m_numberOfWorkers = 2;

        private Stack<Worker> m_workers;
        private Queue<(RequestHandle, Worker.Payload, OnMeshGenerated)> m_requests;
        private ObjectPool<Worker.Payload> m_payloadPool;

        private void Awake()
        {
            m_workers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
            m_requests = new Queue<(RequestHandle, Worker.Payload, OnMeshGenerated)>();
            m_payloadPool = new ObjectPool<Worker.Payload>(() => { return new Worker.Payload(); });
        }

        private void Update()
        {
            while (m_requests.Count > 0 && m_workers.Count > 0)
            {
                (RequestHandle handle, Worker.Payload payload, OnMeshGenerated callback) = m_requests.Dequeue();

                if (!handle.Canceled)
                {
                    StartCoroutine(DispatchWorker(handle, payload, callback));
                }
            }
        }

        private void OnDestroy() => OnDestroyed?.Invoke();

        public RequestHandle RequestMeshAsync(ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing, OnMeshGenerated callback)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            RequestHandle handle = new RequestHandle();
            Worker.Payload payload = m_payloadPool.Acquire((payload) =>
            {
                payload.VoxelVolumeBuffer = voxelVolumeBuffer;
                payload.WorldPosition = worldPosition;
                payload.VoxelSpacing = voxelSpacing;
            });
            m_requests.Enqueue((handle, payload, callback));

            return handle;
        }

        private IEnumerator DispatchWorker(RequestHandle handle, Worker.Payload payload, OnMeshGenerated callback)
        {
            Worker worker = m_workers.Pop();
            worker.GenerateMeshAsync(payload);
            Worker.Status status;

            while ((status = worker.Process()) == Worker.Status.WaitingForGPUReadback)
            {
                yield return null;
            }

            switch (status)
            {
                case Worker.Status.GPUReadbackError:
                    Debug.Log("GPU readback error detected.");
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
            m_workers.Push(worker);
            m_payloadPool.Release(payload);
        }

        private class Worker
        {
            public (NativeArray<Vertex> vertices, NativeArray<int> triangles) Data => (m_vertices, m_triangles);

            private AsyncComputeBuffer m_vertexBuffer;
            private AsyncComputeBuffer m_generatedVertexIndexLookupTable;
            private AsyncComputeBuffer m_triangleBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private NativeArray<Vertex> m_vertices;
            private NativeArray<int> m_triangles;

            public Worker(DualContouring parent)
            {
#if UNITY_EDITOR
                VoxelConfigs.DualContouringConfig.OnDirtied += ApplyDualContouringConfig;
                VoxelConfigs.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
#endif
                parent.OnDestroyed += OnDestroy;
                ApplyDualContouringConfig();
                ApplyVoxelVolumeConfig();
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
                SetupMeshGeneration(payload.VoxelVolumeBuffer, payload.WorldPosition, payload.VoxelSpacing);

                VoxelConfigs.DualContouringConfig.Compute.Dispatch(0, VoxelConfigs.VoxelVolumeConfig.CellVolumeCount);
                VoxelConfigs.DualContouringConfig.Compute.Dispatch(1, VoxelConfigs.VoxelVolumeConfig.CellVolumeCount);

                ComputeBuffer.CopyCount(m_vertexBuffer, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_triangleBuffer, m_countBuffer, sizeof(int));
                m_countBuffer.RequestData();
            }

            private void CreateBuffers(int maxNumberOfVertices, int numberOfVoxels, int maxNumberOfTriangles)
            {
                if (m_vertexBuffer?.Count == maxNumberOfVertices)
                {
                    return;
                }

                ReleaseBuffers();
                m_vertexBuffer = new AsyncComputeBuffer(maxNumberOfVertices, Vertex.SizeInBytes, ComputeBufferType.Counter);
                m_generatedVertexIndexLookupTable = new AsyncComputeBuffer(numberOfVoxels, sizeof(int));
                m_triangleBuffer = new AsyncComputeBuffer(maxNumberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
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

            private void SetupMeshGeneration(ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing)
            {
                m_vertexBuffer.SetCounterValue(0);
                m_triangleBuffer.SetCounterValue(0);

                VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.s_voxelSpacing, voxelSpacing);
                VoxelConfigs.DualContouringConfig.Compute.SetVector(ComputeShaderProperties.s_voxelVolumeToWorldOffset, (Vector3)worldPosition);

                // Link buffer for kernel 0.
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.s_generatedVertices, m_vertexBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.s_generatedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);

                // Link buffer for kernel 1.
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.s_generatedVertices, m_vertexBuffer);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.s_generatedVertexIndicesLookupTable, m_generatedVertexIndexLookupTable);
                VoxelConfigs.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.s_generatedTriangles, m_triangleBuffer);
            }

            private void ApplyDualContouringConfig()
            {
                float cosOfSharpFeatureAngle = math.cos(math.radians(VoxelConfigs.DualContouringConfig.SharpFeatureAngle));
                VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.s_cosOfSharpFeatureAngle, cosOfSharpFeatureAngle);
                VoxelConfigs.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.s_schmitzParticleIterations, VoxelConfigs.DualContouringConfig.SchmitzParticleIterations);
                VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.s_schmitzParticleStepSize, VoxelConfigs.DualContouringConfig.SchmitzParticleStepSize);
            }

            private void ApplyVoxelVolumeConfig()
            {
                int3 voxelVolumeCount = VoxelConfigs.VoxelVolumeConfig.VoxelVolumeCount;
                VoxelConfigs.DualContouringConfig.Compute.SetInts(ComputeShaderProperties.s_voxelVolumeCount, voxelVolumeCount.x, voxelVolumeCount.y, voxelVolumeCount.z);
                CreateBuffers
                (
                    VoxelConfigs.VoxelVolumeConfig.MaxNumberOfVertices,
                    VoxelConfigs.VoxelVolumeConfig.NumberOfVoxels,
                    VoxelConfigs.VoxelVolumeConfig.MaxNumberOfTriangles
                );
            }

            private void OnDestroy()
            {
#if UNITY_EDITOR
                VoxelConfigs.DualContouringConfig.OnDirtied -= ApplyDualContouringConfig;
                VoxelConfigs.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
#endif
                ReleaseBuffers();
            }

            public class Payload : IPoolable
            {
                public ComputeBuffer VoxelVolumeBuffer { get; set; }
                public float3 WorldPosition { get; set; }
                public float VoxelSpacing { get; set; }

                void IPoolable.OnAcquire() { }

                void IPoolable.OnRelease() { }
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

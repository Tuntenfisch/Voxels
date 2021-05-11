using Extensions;
using Generics;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Config;

namespace Voxels
{
    [RequireComponent(typeof(VoxelConfigs))]
    public class DualContouring : MonoBehaviour
    {
        [Range(1, 4)]
        [SerializeField]
        private int m_numberOfWorkers = 2;

        private SetQueue<IVoxelVolume> m_requests;
        private Worker[] m_workers;

        private void Awake()
        {
            m_requests = new SetQueue<IVoxelVolume>();
            m_workers = new Worker[m_numberOfWorkers];

            for (int index = 0; index < m_workers.Length; index++)
            {
                Worker worker = new Worker();
                m_workers[index] = worker;
            }
        }

        private void Update()
        {
            foreach (Worker worker in m_workers)
            {
                if (!worker.IsWaitingForData && m_requests.TryDequeue(out IVoxelVolume requester))
                {
                    StartCoroutine(DispatchWorker(worker, requester));
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Worker worker in m_workers)
            {
                worker.Dispose();
            }
        }

        public void RequestMeshGeneration(IVoxelVolume requester)
        {
            if (requester == null)
            {
                throw new ArgumentNullException(nameof(requester));
            }

            m_requests.Enqueue(requester);
        }

        private IEnumerator DispatchWorker(Worker worker, IVoxelVolume requester)
        {
            worker.GenerateMeshAsync(requester);

            while (worker.IsWaitingForData)
            {
                worker.CheckIfDataReceived();

                yield return null;
            }
        }

        private class Worker : IDisposable
        {
            public bool IsWaitingForData => m_flags.HasFlag(WorkerFlags.Busy);

            private AsyncComputeBuffer m_vertexBuffer;
            private AsyncComputeBuffer m_generatedVertexIndexLookupTable;
            private AsyncComputeBuffer m_triangleBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private IVoxelVolume m_requester;
            private WorkerFlags m_flags;

            public Worker()
            {
#if UNITY_EDITOR
                VoxelConfigs.DualContouringConfig.OnDirty += ApplyDualContouringConfig;
                VoxelConfigs.VoxelVolumeConfig.OnDirty += ApplyVoxelVolumeConfig;
#endif
                ApplyDualContouringConfig();
                ApplyVoxelVolumeConfig();
            }

            public void Dispose()
            {
#if UNITY_EDITOR
                VoxelConfigs.DualContouringConfig.OnDirty -= ApplyDualContouringConfig;
                VoxelConfigs.VoxelVolumeConfig.OnDirty -= ApplyVoxelVolumeConfig;
#endif
                ReleaseBuffers();
            }

            public void CheckIfDataReceived()
            {
                if (m_countBuffer.IsDataAvailable())
                {
                    OnVertexAndTriangleCountRetrieved();
                }

                if (m_vertexBuffer.IsDataAvailable() && m_triangleBuffer.IsDataAvailable())
                {
                    OnMeshDataRetrieved();
                }
            }

            public void GenerateMeshAsync(IVoxelVolume requester)
            {
                m_requester = requester;

                (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing) = requester.GetArguments();

                if (voxelVolumeBuffer == null)
                {
                    throw new NullReferenceException("Voxel volume buffer can't be null!");
                }

                SetupMeshGeneration(voxelVolumeBuffer, worldPosition, voxelSpacing);

                VoxelConfigs.DualContouringConfig.Compute.Dispatch(0, VoxelConfigs.VoxelVolumeConfig.CellVolumeCount);
                VoxelConfigs.DualContouringConfig.Compute.Dispatch(1, VoxelConfigs.VoxelVolumeConfig.CellVolumeCount);

                ComputeBuffer.CopyCount(m_vertexBuffer, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_triangleBuffer, m_countBuffer, sizeof(int));
                m_countBuffer.RequestData();

                m_flags |= WorkerFlags.Busy;
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

            private void OnVertexAndTriangleCountRetrieved()
            {
                if (m_countBuffer.HasError)
                {
                    Debug.Log("GPU readback error detected.");

                    return;
                }

                NativeArray<int> counts = m_countBuffer.GetData<int>();

                int vertexCount = counts[0];
                int triangleCount = counts[1];

                if (triangleCount == 0)
                {
                    m_requester.OnMeshGenerated(null, null);
                    m_requester = null;
                    m_flags &= ~WorkerFlags.Busy;

                    return;
                }

                // Retrieve vertices and triangles asynchronously.
                m_vertexBuffer.RequestData(vertexCount);
                m_triangleBuffer.RequestData(triangleCount);
            }

            private void OnMeshDataRetrieved()
            {
                if (m_vertexBuffer.HasError || m_triangleBuffer.HasError)
                {
                    Debug.Log("GPU readback error detected.");

                    return;
                }

                NativeArray<Vertex> vertices = m_vertexBuffer.GetData<Vertex>();
                NativeArray<int> triangles = m_triangleBuffer.GetData<int>();

                m_requester.OnMeshGenerated(vertices, triangles);
                m_requester = null;
                m_flags &= ~WorkerFlags.Busy;
            }

            private void ApplyDualContouringConfig()
            {
                VoxelConfigs.DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.s_cosOfSharpFeatureAngle, math.cos(math.radians(VoxelConfigs.DualContouringConfig.SharpFeatureAngle)));
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

            [Flags]
            private enum WorkerFlags
            {
                Busy = 1
            }
        }
    }
}

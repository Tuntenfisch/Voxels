using Extensions;
using Generics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Config;

namespace Voxels
{
    [RequireComponent(typeof(VoxelConfigs))]
    public class DualContouring : MonoBehaviour
    {
        private event Action OnDestroyed;

        [Range(1, 8)]
        [SerializeField]
        private int m_numberOfWorkers = 2;

        private SetQueue<IVoxelVolume> m_requests;
        private Stack<Worker> m_workers;

        private void Awake()
        {
            m_requests = new SetQueue<IVoxelVolume>();
            m_workers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
        }

        private void Update()
        {
            if (m_requests.Count > 0 && m_workers.Count > 0)
            {
                StartCoroutine(DispatchWorker(m_workers.Pop(), m_requests.Dequeue()));
            }
        }

        private void OnDestroy() => OnDestroyed?.Invoke();

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

            m_workers.Push(worker);
        }

        private class Worker
        {
            public bool IsWaitingForData => m_requester != null;

            private AsyncComputeBuffer m_vertexBuffer;
            private AsyncComputeBuffer m_generatedVertexIndexLookupTable;
            private AsyncComputeBuffer m_triangleBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private IVoxelVolume m_requester;

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
        }
    }
}

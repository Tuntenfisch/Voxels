using Extensions;
using Generics;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Voxels
{
    public class CubicalMarchingSquares : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private Configuration m_configuration;
        [Range(1, 4)]
        [SerializeField]
        private int m_numberOfWorkers = 2;

        private SetQueue<IVoxelVolume> m_requests;
        private Worker[] m_workers;

        private void Awake()
        {
            Assert.IsNotNull(m_configuration);

            m_configuration.OnDirty += OnConfigurationDirty;
            m_requests = new SetQueue<IVoxelVolume>();
            m_workers = new Worker[m_numberOfWorkers];

            for (int index = 0; index < m_workers.Length; index++)
            {
                Worker worker = new Worker(m_configuration);
                m_workers[index] = worker;
            }
        }

        private void Update()
        {
            foreach (Worker worker in m_workers)
            {
                if (worker.IsWaitingForData)
                {
                    worker.CheckIfDataReceived();
                }
                else if (m_requests.TryDequeue(out IVoxelVolume requester))
                {
                    worker.GenerateMeshAsync(requester);
                }
            }
        }

        private void OnDestroy()
        {
            m_configuration.OnDirty -= OnConfigurationDirty;

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

        private void OnConfigurationDirty()
        {
            m_requests.Clear();
        }

        private class Worker : IDisposable
        {
            public bool IsWaitingForData => m_flags.HasFlag(WorkerFlags.Busy);

            private ComputeShader ComputeShader => m_configuration.CubicalMarchingSquaresCompute;

            private readonly Configuration m_configuration;

            private AsyncComputeBuffer m_vertexBuffer;
            private AsyncComputeBuffer m_flatFeatureVertexIndicesLookupBuffer;
            private AsyncComputeBuffer m_triangleBuffer;
            private AsyncComputeBuffer m_countBuffer;

            private IVoxelVolume m_requester;
            private WorkerFlags m_flags;

            public Worker(Configuration configuration)
            {
                m_configuration = configuration;
                m_configuration.OnDirty += OnConfigurationDirty;
                CreateBuffers();
            }

            public void Dispose()
            {
                m_configuration.OnDirty -= OnConfigurationDirty;
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

                ComputeShader.Dispatch(0, m_configuration.VoxelVolumeCount);
                ComputeShader.Dispatch(1, m_configuration.CellVolumeCount);

                ComputeBuffer.CopyCount(m_vertexBuffer, m_countBuffer, 0);
                ComputeBuffer.CopyCount(m_triangleBuffer, m_countBuffer, sizeof(int));

                m_countBuffer.RequestData();

                m_flags |= WorkerFlags.Busy;
            }

            private void CreateBuffers()
            {
                if (m_vertexBuffer != null)
                {
                    return;
                }

                int numberOfVertices = m_configuration.MaxNumberOfFlatFeatureVertices + m_configuration.MaxNumberOfSharpFeatureVertices;
                int numberOfVoxels = m_configuration.NumberOfVoxels;
                int numberOfTriangles = m_configuration.MaxNumberOfTriangles;

                m_vertexBuffer = new AsyncComputeBuffer(numberOfVertices, Vertex.SizeInBytes, ComputeBufferType.Counter);
                m_flatFeatureVertexIndicesLookupBuffer = new AsyncComputeBuffer(numberOfVoxels, 3 * sizeof(int));
                m_triangleBuffer = new AsyncComputeBuffer(numberOfTriangles, 3 * sizeof(int), ComputeBufferType.Append);
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
                m_flatFeatureVertexIndicesLookupBuffer.Release();
                m_flatFeatureVertexIndicesLookupBuffer = null;
                m_triangleBuffer.Release();
                m_triangleBuffer = null;
                m_countBuffer.Release();
                m_countBuffer = null;
            }

            private void SetupMeshGeneration(ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing)
            {
                m_vertexBuffer.SetCounterValue(0);
                m_triangleBuffer.SetCounterValue(0);

                ComputeShader.SetFloat(ComputeShaderProperties.s_voxelSpacing, voxelSpacing);
                ComputeShader.SetVector(ComputeShaderProperties.s_voxelVolumeToWorldOffset, (Vector3)worldPosition);

                // Link buffer for kernel 0.
                ComputeShader.SetBuffer(0, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
                ComputeShader.SetBuffer(0, ComputeShaderProperties.s_generatedVertices, m_vertexBuffer);
                ComputeShader.SetBuffer(0, ComputeShaderProperties.s_flatFeatureVertexIndicesLookupTable, m_flatFeatureVertexIndicesLookupBuffer);

                // Link buffer for kernel 1.
                ComputeShader.SetBuffer(1, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
                ComputeShader.SetBuffer(1, ComputeShaderProperties.s_generatedVertices, m_vertexBuffer);
                ComputeShader.SetBuffer(1, ComputeShaderProperties.s_flatFeatureVertexIndicesLookupTable, m_flatFeatureVertexIndicesLookupBuffer);
                ComputeShader.SetBuffer(1, ComputeShaderProperties.s_generatedTriangles, m_triangleBuffer);
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
                    Assert.IsNotNull(m_requester, "The requester for this worker is null. Did you forget to assign it?");

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
                Profiler.BeginSample($"{nameof(Worker)}.{nameof(OnMeshDataRetrieved)}");

                if (m_vertexBuffer.HasError || m_triangleBuffer.HasError)
                {
                    Debug.Log("GPU readback error detected.");

                    return;
                }

                NativeArray<Vertex> vertices = m_vertexBuffer.GetData<Vertex>();
                NativeArray<int> triangles = m_triangleBuffer.GetData<int>();

                Assert.IsNotNull(m_requester, "The requester for this worker is null. Did you forget to assign it?");

                m_requester.OnMeshGenerated(vertices, triangles);
                m_requester = null;

                m_flags &= ~WorkerFlags.Busy;

                Profiler.EndSample();
            }

            private void OnConfigurationDirty()
            {
                if (m_flatFeatureVertexIndicesLookupBuffer?.Count != m_configuration.NumberOfVoxels)
                {
                    ReleaseBuffers();
                    CreateBuffers();
                }
            }

            [Flags]
            private enum WorkerFlags
            {
                Busy = 1
            }
        }
    }
}

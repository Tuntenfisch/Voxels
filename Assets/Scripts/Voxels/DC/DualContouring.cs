using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Extensions;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.DC
{
    [RequireComponent(typeof(VoxelConfig))]
    public class DualContouring : MonoBehaviour
    {
        private event Action OnDestroyed;

        [Range(1, 16)]
        [SerializeField]
        private int m_numberOfWorkers = 4;
        [Min(0)]
        [SerializeField]
        private int m_initialTaskPoolPopulation = 0;
        [Range(1.0f, 2.0f)]
        [SerializeField]
        private float m_readbackInflationFactor = 1.25f;

        private VoxelConfig m_voxelConfig;
        private Queue<Worker.Task> m_tasks;
        private Stack<Worker> m_availableWorkers;
        private ObjectPool<Worker.Task> m_taskPool;

        private void Awake()
        {
            m_voxelConfig = GetComponent<VoxelConfig>();
            m_tasks = new Queue<Worker.Task>();
            m_availableWorkers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
            m_taskPool = new ObjectPool<Worker.Task>(() => { return new Worker.Task(); }, m_initialTaskPoolPopulation);
        }

        private void LateUpdate()
        {
            while (m_tasks.Count > 0 && m_availableWorkers.Count > 0)
            {
                DispatchWorker(m_tasks.Dequeue());
            }
        }

        private void OnDestroy() => OnDestroyed?.Invoke();

        private void OnValidate()
        {
            if (m_availableWorkers == null)
            {
                return;
            }

            foreach (Worker worker in m_availableWorkers)
            {
                worker.Dispose();
            }
            m_availableWorkers = new Stack<Worker>(Enumerable.Range(0, m_numberOfWorkers).Select(index => new Worker(this)));
        }

        public IRequest RequestMeshAsync
        (
            ComputeBuffer voxelVolumeBuffer,
            int currentLOD,
            int targetLOD,
            int currentVertexCount,
            int currentTriangleCount,
            float3 worldPosition,
            OnMeshGenerated callback
        )
        {
            Worker.Task task = m_taskPool.Acquire();
            task.VoxelVolumeBuffer = voxelVolumeBuffer ?? throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            task.CurrentLOD = currentLOD;
            task.TargetLOD = targetLOD;
            task.CurrentVertexCount = currentVertexCount;
            task.CurrentTriangleCount = currentTriangleCount;
            task.VoxelVolumeToWorldSpaceOffset = worldPosition;
            task.Callback = callback ?? throw new ArgumentNullException(nameof(callback));

            // If a worker is available, directly dispatch the task.
            if (m_availableWorkers.Count > 0)
            {
                DispatchWorker(task);
            }
            else
            {
                m_tasks.Enqueue(task);
            }

            return task;
        }

        private void DispatchWorker(Worker.Task task)
        {
            if (task.Canceled)
            {
                m_taskPool.Release(task);

                return;
            }

           DispatchWorkerUniTask(task).Forget();
        }

        private async UniTaskVoid DispatchWorkerUniTask(Worker.Task task)
        {
            Worker worker = m_availableWorkers.Pop();
            worker.GenerateMeshAsync(task);

            do
            {
                await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
            }
            while (worker.Process() == Worker.Status.WaitingForGPUReadback);

            // Only call the callback if the task hasn't been canceled.
            if (!task.Canceled)
            {
                task.Callback(worker.Vertices, worker.VertexCount, 0, worker.Triangles, worker.TriangleCount, 2);
            }
            m_taskPool.Release(task);

            if (m_availableWorkers.Count < m_numberOfWorkers)
            {
                m_availableWorkers.Push(worker);
            }
            else
            {
                worker.Dispose();
            }
        }

        private class Worker : IDisposable
        {
            public int VertexCount { get; private set; }
            public int TriangleCount { get; private set; }
            public NativeArray<GPUVertex> Vertices => m_generatedVertices;
            // In addition to the triangles, this native array also reads back the number of triangles and the number of vertices generated, i.e.
            // two additional integers.
            public NativeArray<int> Triangles => m_generatedTriangles;

            private DualContouring m_parent;

            private NativeArray<GPUVertex> m_generatedVertices;
            private NativeArray<int> m_generatedTriangles;

            private AsyncComputeBuffer m_cellVertexInfoLookupTableBuffer;
            private AsyncComputeBuffer m_generatedVerticesBuffer0;
            private AsyncComputeBuffer m_generatedVerticesBuffer1;
            private AsyncComputeBuffer m_generatedTrianglesBuffer;

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
                if (m_generatedVerticesBuffer0.IsDataAvailable() && m_generatedTrianglesBuffer.IsDataAvailable())
                {
                    int requestedVertexCount = m_generatedVerticesBuffer0.EndReadback();
                    int requestedTriangleCount = m_generatedTrianglesBuffer.EndReadback();

                    VertexCount = m_generatedTriangles[0];
                    TriangleCount = 3 * m_generatedTriangles[1];

                    if (requestedVertexCount < VertexCount || requestedTriangleCount < TriangleCount || m_generatedVerticesBuffer0.HasError || m_generatedTrianglesBuffer.HasError)
                    {
                        if (Debug.isDebugBuild && (m_generatedVerticesBuffer0.HasError || m_generatedTrianglesBuffer.HasError))
                        {
                            Debug.LogWarning("GPU readback error detected.");
                        }
                        // If we retrieved too few vertices/triangles, we need to start another readback to retrieve the correct count.
                        m_generatedVerticesBuffer0.StartReadbackNonAlloc(ref m_generatedVertices, VertexCount);
                        m_generatedTrianglesBuffer.StartReadbackNonAlloc(ref m_generatedTriangles, TriangleCount + 2);

                        return Status.WaitingForGPUReadback;
                    }

                    return Status.Done;
                }

                return Status.WaitingForGPUReadback;
            }

            public void GenerateMeshAsync(Task task)
            {
                m_cellVertexInfoLookupTableBuffer.SetCounterValue(0);
                m_generatedVerticesBuffer0.SetCounterValue(0);

                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)task.VoxelVolumeToWorldSpaceOffset);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetInt(ComputeShaderProperties.CellStride, 1);

                // First we generate the inner cell vertices, i.e. all vertices which's cells do not reside on the surface of the voxel volume of this chunk.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, task.VoxelVolumeBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(0, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(0, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells - 2);

                // Next we generate the desired level of detail for the previously generated vertices of this chunk.
                // The safest way to go about level of detail is to first generate the mesh vertices at the highest lod (above dispatch call) and then merge those vertices
                // to create a lower lod. In order to leverage the GPU's parallelism we do this iteratively, similarly to how parallel reduction works.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(1, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);

                for (int cellStride = 2; cellStride <= (1 << task.TargetLOD); cellStride <<= 1)
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
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.VoxelVolume, task.VoxelVolumeBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(2, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(2, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells);

                // Finally, we triangulate the vertices to form the mesh.
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.VoxelVolume, task.VoxelVolumeBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.CellVertexInfoLookupTable, m_cellVertexInfoLookupTableBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedVertices0, m_generatedVerticesBuffer0);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.SetBuffer(3, ComputeShaderProperties.GeneratedTriangles, m_generatedTrianglesBuffer);
                m_parent.m_voxelConfig.DualContouringConfig.Compute.Dispatch(3, m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCells - 1);

                // Normally, in order to retrieve the vertices/triangles generated, you would first read the counter values of
                // the respective compute buffers and then, in an additional readback, retrieve the vertices/triangles themselves.
                //
                // But this would require two readbacks and delay the updating of the mesh longer than acceptable.
                // Instead, we copy the counter values into the beginning of the triangles buffer and then, by estimating the
                // number of vertices/triangles we expect the compute shader to generate, retrieve the vertices/triangles.
                // 
                // Later on, once we receive the data from the readback, we can compare the actual number of vertices/triangles
                // to the number of vertices/triangles we initially read based on our estimate. Two possible scenarios can occur:
                //
                //     1. We correctly estimated the vertex/triangle counts and got all vertices/triangles during the first readback.
                //        Note: We don't really care if we retrieved more vertices/triangles than generated, as long as we don't
                //              retrieve unnecessarily many too often.
                //
                //     2. We retrieved too few vertices/triangles. Using the actual vertex/triangle counts we need to retrieve 
                //        the vertices/triangles again using the correct counts.
                //
                // So, best case equals one readback, worst case equals two readbacks, i.e. the worst case is as bad as the best case
                // before and the best case is twice as good.
                (int estimatedVertexCount, int estimatedTriangleCount) = EstimateVertexAndTriangleCounts(task);

                // Copy the number of vertices/triangles generated into the start of the triangles buffer.
                ComputeBuffer.CopyCount(m_generatedVerticesBuffer0, m_generatedTrianglesBuffer, 0);
                ComputeBuffer.CopyCount(m_cellVertexInfoLookupTableBuffer, m_generatedTrianglesBuffer, sizeof(uint));
                // Retrieve both the vertices and triangles buffer.
                m_generatedVerticesBuffer0.StartReadbackNonAlloc(ref m_generatedVertices, estimatedVertexCount);
                // We're adding 2 because the vertex and triangle counts are stored in the buffer as well.
                m_generatedTrianglesBuffer.StartReadbackNonAlloc(ref m_generatedTriangles, estimatedTriangleCount + 2);
            }

            private (int, int) EstimateVertexAndTriangleCounts(Task task)
            {
                float factor = m_parent.m_readbackInflationFactor * math.pow(2.0f, task.TargetLOD - task.CurrentLOD);

                int estimatedVertexCount = (int)math.round(factor * task.CurrentVertexCount);
                estimatedVertexCount = math.clamp(1, estimatedVertexCount, m_generatedVertices.Length);

                int estimatedTriangleCount = (int)math.round(factor * task.CurrentTriangleCount);
                estimatedTriangleCount = math.clamp(1, estimatedTriangleCount, m_generatedTriangles.Length - 2);

                return (estimatedVertexCount, estimatedTriangleCount);
            }

            private void CreateBuffers()
            {
                // Create CPU buffers.
                int maxNumberOfVertices = m_parent.m_voxelConfig.VoxelVolumeConfig.CellCount;
                int generatedVerticesCapacity = maxNumberOfVertices;

                if (!m_generatedVertices.IsCreated || m_generatedVertices.Length != generatedVerticesCapacity)
                {
                    if (m_generatedVertices.IsCreated)
                    {
                        m_generatedVertices.Dispose();
                    }
                    m_generatedVertices = new NativeArray<GPUVertex>(generatedVerticesCapacity, Allocator.Persistent);
                }

                int maxNumberOfTriangles = 3 * 6 * (int)math.round(math.pow(m_parent.m_voxelConfig.VoxelVolumeConfig.NumberOfCellsAlongAxis - 1, 3));
                // As mentioned, in addition to storing the triangles, this buffer will also store the number of vertices and triangles generated, i.e.
                // two additional integers.
                int generatedTrianglesCapacity = maxNumberOfTriangles + 2;

                if (!m_generatedTriangles.IsCreated || m_generatedTriangles.Length != generatedTrianglesCapacity)
                {
                    if (m_generatedTriangles.IsCreated)
                    {
                        m_generatedTriangles.Dispose();
                    }
                    m_generatedTriangles = new NativeArray<int>(generatedTrianglesCapacity, Allocator.Persistent);
                }

                // Create GPU buffers.
                if (m_cellVertexInfoLookupTableBuffer?.Count != m_parent.m_voxelConfig.VoxelVolumeConfig.CellCount)
                {
                    m_cellVertexInfoLookupTableBuffer?.Release();
                    // Since we cannot declare the triangles buffer of compute buffer type "counter", we use this buffer to keep track of the number of triangles generated.
                    m_cellVertexInfoLookupTableBuffer = new AsyncComputeBuffer(m_parent.m_voxelConfig.VoxelVolumeConfig.CellCount, sizeof(uint), ComputeBufferType.Counter);
                }

                if (m_generatedVerticesBuffer0?.Count != m_generatedVertices.Length)
                {
                    m_generatedVerticesBuffer0?.Release();
                    // The counter attached to this compute buffer stores the number of vertices generated by dual contouring.
                    m_generatedVerticesBuffer0 = new AsyncComputeBuffer(m_generatedVertices.Length, GPUVertex.SizeInBytes, ComputeBufferType.Counter);
                }

                if (m_generatedVerticesBuffer1?.Count != m_generatedVertices.Length)
                {
                    m_generatedVerticesBuffer1?.Release();
                    // The counter attached to this compute buffer stores the number of vertices generated by dual contouring.
                    m_generatedVerticesBuffer1 = new AsyncComputeBuffer(m_generatedVertices.Length, GPUVertex.SizeInBytes, ComputeBufferType.Counter);
                }

                if (m_generatedTrianglesBuffer?.Count != m_generatedTriangles.Length)
                {
                    m_generatedTrianglesBuffer?.Release();
                    // To copy the counter values into the triangles buffer it needs to be of type "raw".
                    m_generatedTrianglesBuffer = new AsyncComputeBuffer(m_generatedTriangles.Length, sizeof(uint), ComputeBufferType.Raw);
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
            }

            public enum Status
            {
                WaitingForGPUReadback,
                Done
            }

            public class Task : IPoolable, IRequest
            {
                public bool Canceled { get; private set; }
                public ComputeBuffer VoxelVolumeBuffer { get; set; }
                public int CurrentLOD { get; set; }
                public int TargetLOD { get; set; }
                public int CurrentVertexCount { get; set; }
                public int CurrentTriangleCount { get; set; }
                public float3 VoxelVolumeToWorldSpaceOffset { get; set; }
                public OnMeshGenerated Callback { get; set; }

                public void OnAcquire() { }

                public void OnRelease()
                {
                    VoxelVolumeBuffer = null;
                    Callback = null;
                    Canceled = false;
                }

                public void Cancel() => Canceled = true;
            }
        }
    }
}
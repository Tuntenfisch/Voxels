using System;
using System.Collections;
using System.Collections.Generic;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Voxels;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.DC;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tuntenfisch.World
{
    [RequireComponent(typeof(VoxelConfig), typeof(VoxelVolume), typeof(DualContouring))]
    [RequireComponent(typeof(CSGUtility))]
    public class WorldManager : SingletonComponent<WorldManager>
    {
        internal static VoxelConfig VoxelConfig => Instance.m_voxelConfig;
        internal static VoxelVolume VoxelVolume => Instance.m_voxelVolume;
        internal static DualContouring DualContouring => Instance.m_dualContouring;
        internal static ObjectPool<Chunk> SharedChunkPool => Instance.m_sharedChunkPool;

        private float ViewDistanceSquared => m_lodDistancesSquared[m_lodDistancesSquared.Length - 1];

        [SerializeField]
        private Transform m_viewer;
        [SerializeField]
        private float m_updateInterval = 20.0f;
        [SerializeField]
        private GameObject m_chunkPrefab;
        [SerializeField]
        private int m_maxNumberOfChunksProcessedEachFrame = 20;
        [SerializeField]
        private float[] m_lodDistances;

        private VoxelConfig m_voxelConfig;
        private VoxelVolume m_voxelVolume;
        private DualContouring m_dualContouring;
        private CSGUtility m_csgUtility;
        private ObjectPool<Chunk> m_sharedChunkPool;
        private Dictionary<int3, Chunk> m_chunks;
        private HashSet<int3> m_oldChunkCoordinates;
        private Queue<(int3, float3, float, int)> m_chunksToProcess;
        private HashSet<int3> m_processedChunkCoordinates;
        private float3 m_chunkDimensions;

        // We don't want to update the visible world every frame.
        // Sample applies to the level of details of the chunks.
        private float3 m_lastViewerPosition;
        private float m_updateIntervalSquared;
        private float[] m_lodDistancesSquared;

        private Coroutine m_updateWorldCoroutine;
        private Coroutine m_applySettingsCoroutine;

        private void Start()
        {
            Assert.IsFalse(m_chunkPrefab.activeSelf);

            m_voxelConfig = GetComponent<VoxelConfig>();
#if UNITY_EDITOR
            m_voxelConfig.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
            m_voxelConfig.DualContouringConfig.OnDirtied += ApplyDualContouringConfig;
            m_voxelConfig.NoiseGraph.OnDirtied += ApplyNoiseConfig;
#endif
            m_voxelVolume = GetComponent<VoxelVolume>();
            m_dualContouring = GetComponent<DualContouring>();
            m_csgUtility = GetComponent<CSGUtility>();

            m_sharedChunkPool = new ObjectPool<Chunk>(() => { return Instantiate(Instance.m_chunkPrefab, Instance.transform).GetComponent<Chunk>(); });
            m_chunks = new Dictionary<int3, Chunk>();
            m_oldChunkCoordinates = new HashSet<int3>();
            m_chunksToProcess = new Queue<(int3, float3, float, int)>();
            m_processedChunkCoordinates = new HashSet<int3>();
            m_chunkDimensions = CalculateChunkDimensions();

            m_lastViewerPosition = m_viewer.position;
            m_updateIntervalSquared = math.pow(m_updateInterval, 2.0f);
            m_lodDistancesSquared = CalculateLodDistancesSquared();

            if (!Application.isEditor)
            {
                UpdateWorld(m_viewer.position, m_maxNumberOfChunksProcessedEachFrame);
            }
        }

        private void Update()
        {
            if (math.lengthsq((float3)m_viewer.position - m_lastViewerPosition) >= m_updateIntervalSquared)
            {
                if (m_updateWorldCoroutine != null)
                {
                    StopCoroutine(m_updateWorldCoroutine);
                    m_updateWorldCoroutine = null;
                }
                m_lastViewerPosition = m_viewer.position;
                UpdateWorld(m_viewer.position, m_maxNumberOfChunksProcessedEachFrame);
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            m_voxelConfig.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
            m_voxelConfig.DualContouringConfig.OnDirtied -= ApplyDualContouringConfig;
            m_voxelConfig.NoiseGraph.OnDirtied -= ApplyNoiseConfig;
#endif
            SharedChunkPool.Apply((chunk, inUse) => { chunk.ReleaseBuffers(); });
        }

        private void OnValidate() => ApplySettings();

        public void DrawCSGPrimitiveHologram(CSGPrimitiveType primitiveType, Matrix4x4 objectToWorldMatrix) => m_csgUtility.DrawCSGPrimitiveHologram(primitiveType, objectToWorldMatrix);

        private void UpdateWorld(float3 viewerPosition, int maxNumberOfChunksProcessedEachFrame = -1)
        {
            if (m_updateWorldCoroutine == null)
            {
                m_updateWorldCoroutine = StartCoroutine(UpdateWorldCoroutine(viewerPosition, maxNumberOfChunksProcessedEachFrame));
            }
            else
            {
                throw new InvalidOperationException($"{nameof(UpdateWorldCoroutine)} is already running.");
            }
        }

        private IEnumerator UpdateWorldCoroutine(float3 viewerPosition, int maxNumberOfChunksProcessedEachFrame = -1)
        {
            yield return DestroyChunksOutsideViewDistanceCoroutine(viewerPosition, maxNumberOfChunksProcessedEachFrame);
            yield return CreateChunksWithinViewDistanceCoroutine(viewerPosition, maxNumberOfChunksProcessedEachFrame);

            m_updateWorldCoroutine = null;
        }

        private IEnumerator DestroyChunksOutsideViewDistanceCoroutine(float3 viewerPosition, int maxNumberOfChunksProcessedEachFrame)
        {
            int numberOfChunksProcessed = 0;

            m_oldChunkCoordinates.UnionWith(m_chunks.Keys);

            foreach (KeyValuePair<int3, Chunk> pair in m_chunks)
            {
                float viewerToChunkDistanceSquared = math.lengthsq((float3)pair.Value.transform.position - viewerPosition);

                if (viewerToChunkDistanceSquared <= ViewDistanceSquared)
                {
                    m_oldChunkCoordinates.Remove(pair.Key);
                }

                if (++numberOfChunksProcessed == maxNumberOfChunksProcessedEachFrame)
                {
                    numberOfChunksProcessed = 0;
                    yield return null;
                }
            }

            foreach (int3 chunkCoordinate in m_oldChunkCoordinates)
            {
                SharedChunkPool.Release(m_chunks[chunkCoordinate]);
                m_chunks.Remove(chunkCoordinate);
            }
            m_oldChunkCoordinates.Clear();
        }

        private IEnumerator CreateChunksWithinViewDistanceCoroutine(float3 viewerPosition, int maxNumberOfChunksProcessedEachFrame)
        {
            int numberOfChunksProcessed = 0;

            int3 chunkCoordinate = CalculateViewerChunkCoordinate();
            float3 chunkPosition = chunkCoordinate * m_chunkDimensions;
            float viewerToChunkDistanceSquared = math.lengthsq(chunkPosition - viewerPosition);
            int lod = CalculateChunkLod(viewerToChunkDistanceSquared);

            m_processedChunkCoordinates.Clear();
            m_chunksToProcess.Clear();
            m_chunksToProcess.Enqueue((chunkCoordinate, chunkPosition, viewerToChunkDistanceSquared, lod));

            while (m_chunksToProcess.Count > 0)
            {
                (chunkCoordinate, chunkPosition, viewerToChunkDistanceSquared, lod) = m_chunksToProcess.Dequeue();

                if (m_chunks.TryGetValue(chunkCoordinate, out Chunk chunk))
                {
                    if (chunk.Lod != lod)
                    {
                        chunk.Lod = lod;
                        chunk.Remeshify();
                    }
                }
                else
                {
                    // Create new chunk.
                    chunk = SharedChunkPool.Acquire((chunk) =>
                    {
                        chunk.gameObject.name = $"Chunk ({chunkCoordinate.x}, {chunkCoordinate.y}, {chunkCoordinate.z})";
                        chunk.transform.position = chunkPosition;
                        chunk.Lod = lod;
                        chunk.CreateBuffers();
                    });
                    chunk.Regenerate();
                    chunk.Remeshify();

                    m_chunks[chunkCoordinate] = chunk;
                }
                m_processedChunkCoordinates.Add(chunkCoordinate);

                EnqueueNeighbourChunk(chunkCoordinate + new int3(1, 0, 0), viewerPosition);
                EnqueueNeighbourChunk(chunkCoordinate - new int3(1, 0, 0), viewerPosition);
                EnqueueNeighbourChunk(chunkCoordinate + new int3(0, 0, 1), viewerPosition);
                EnqueueNeighbourChunk(chunkCoordinate - new int3(0, 0, 1), viewerPosition);

                if (++numberOfChunksProcessed == maxNumberOfChunksProcessedEachFrame)
                {
                    numberOfChunksProcessed = 0;
                    yield return null;
                }
            }
        }

        private void EnqueueNeighbourChunk(int3 neighbourChunkCoordinate, float3 viewerPosition)
        {
            if (!m_processedChunkCoordinates.Contains(neighbourChunkCoordinate))
            {
                float3 neighbourChunkPosition = neighbourChunkCoordinate * m_chunkDimensions;
                float viewerToNeighbourChunkDistanceSquared = math.lengthsq(neighbourChunkPosition - viewerPosition);

                if (viewerToNeighbourChunkDistanceSquared <= ViewDistanceSquared)
                {
                    m_chunksToProcess.Enqueue((neighbourChunkCoordinate, neighbourChunkPosition, viewerToNeighbourChunkDistanceSquared, CalculateChunkLod(viewerToNeighbourChunkDistanceSquared)));
                }
            }
        }

        private float3 CalculateChunkDimensions()
        {
            const int voxelOverlap = 1;
            float inflationFactor = 1.0f + (float)voxelOverlap / (VoxelConfig.VoxelVolumeConfig.NumberOfCellsAlongAxis - voxelOverlap);

            return VoxelConfig.VoxelVolumeConfig.VoxelVolumeDimensions / inflationFactor;
        }

        private int3 CalculateViewerChunkCoordinate() => (int3)math.round(m_viewer.position / m_chunkDimensions);

        private float[] CalculateLodDistancesSquared()
        {
            float[] lodDistancesSquared = new float[m_lodDistances.Length];

            for (int index = 0; index < lodDistancesSquared.Length; index++)
            {
                lodDistancesSquared[index] = math.pow(m_lodDistances[index], 2.0f);
            }

            return lodDistancesSquared;
        }

        private int CalculateChunkLod(float viewerToChunkDistanceSquared)
        {
            int lod = Array.BinarySearch(m_lodDistancesSquared, viewerToChunkDistanceSquared);

            if (lod < 0)
            {
                lod = ~lod;
            }

            if (lod == m_lodDistancesSquared.Length)
            {
                lod = 0;
            }

            return lod;
        }

        private void ApplySettings()
        {
            if (Application.isPlaying && gameObject.activeSelf && m_applySettingsCoroutine == null)
            {
                m_applySettingsCoroutine = StartCoroutine(ApplySettingsCoroutine());
            }
        }

        private IEnumerator ApplySettingsCoroutine()
        {
            do
            {
                yield return null;
            }
            while (m_updateWorldCoroutine != null);

            m_updateIntervalSquared = math.pow(m_updateInterval, 2.0f);
            m_lodDistancesSquared = CalculateLodDistancesSquared();
            m_chunkDimensions = CalculateChunkDimensions();

            foreach (Chunk chunk in m_chunks.Values)
            {
                SharedChunkPool.Release(chunk);
            }
            m_chunks.Clear();

            UpdateWorld(m_viewer.position, m_maxNumberOfChunksProcessedEachFrame);
            m_applySettingsCoroutine = null;
        }

        private void ApplyVoxelVolumeConfig() => ApplySettings();

        private void ApplyDualContouringConfig() => ApplySettings();

        private void ApplyNoiseConfig() => ApplySettings();
    }
}
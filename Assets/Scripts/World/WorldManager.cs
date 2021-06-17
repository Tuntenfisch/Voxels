﻿using System;
using System.Collections;
using System.Collections.Generic;
using Tuntenfisch.Generics;
using Tuntenfisch.Generics.Pool;
using Tuntenfisch.Voxels;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.DC;
using Tuntenfisch.Voxels.Volume;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tuntenfisch.World
{
    [RequireComponent(typeof(VoxelConfig), typeof(VoxelVolume), typeof(DualContouring))]
    [RequireComponent(typeof(CSGUtility))]
    public class WorldManager : SingletonComponent<WorldManager>
    {
        public static VoxelConfig VoxelConfig => Instance.m_voxelConfig;
        public static VoxelVolume VoxelVolume => Instance.m_voxelVolume;
        public static DualContouring DualContouring => Instance.m_dualContouring;

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
        private Queue<(int3, float3, int)> m_chunksToProcess;
        private HashSet<int3> m_processedChunkCoordinates;
        private float3 m_chunkDimensions;

        // We don't want to update the world every frame.
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
            m_voxelConfig.VoxelVolumeConfig.OnLateDirtied += ApplyVoxelVolumeConfig;
            m_voxelConfig.DualContouringConfig.OnLateDirtied += ApplyDualContouringConfig;
            m_voxelConfig.NoiseGraph.OnLateDirtied += ApplyNoiseGraph;
#endif
            m_voxelVolume = GetComponent<VoxelVolume>();
            m_dualContouring = GetComponent<DualContouring>();
            m_csgUtility = GetComponent<CSGUtility>();

            m_sharedChunkPool = new ObjectPool<Chunk>(() => { return Instantiate(Instance.m_chunkPrefab, Instance.transform).GetComponent<Chunk>(); });
            m_chunks = new Dictionary<int3, Chunk>();
            m_oldChunkCoordinates = new HashSet<int3>();
            m_chunksToProcess = new Queue<(int3, float3, int)>();
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
            m_voxelConfig.VoxelVolumeConfig.OnLateDirtied -= ApplyVoxelVolumeConfig;
            m_voxelConfig.DualContouringConfig.OnLateDirtied -= ApplyDualContouringConfig;
            m_voxelConfig.NoiseGraph.OnLateDirtied -= ApplyNoiseGraph;
#endif
        }

        private void OnValidate() => ApplySettings();

        public void DrawCSGPrimitiveHologram(CSGPrimitiveType primitiveType, float3 position, float3 scale, bool snapToGrid = false)
        {
            if (snapToGrid)
            {
                position = SnapToGrid(position);
                scale = SnapToGrid(scale);
            }
            m_csgUtility.DrawCSGPrimitiveHologram(primitiveType, Matrix4x4.TRS(position, quaternion.identity, scale));
        }

        public void ApplyCSGOperation(CSGOperatorIndex operatorIndex, CSGPrimitiveType csgPrimitiveType, float3 position, float3 scale, bool snapToGrid = false)
        {
            if (snapToGrid)
            {
                position = SnapToGrid(position);
                scale = SnapToGrid(scale);
            }
            GPUCSGOperator csgOperator = new GPUCSGOperator(operatorIndex, 0.0f);
            GPUCSGPrimitive csgPrimitive = new GPUCSGPrimitive(csgPrimitiveType);
            Matrix4x4 worldToObjectMatrix = Matrix4x4.TRS(position, quaternion.identity, scale).inverse;

            // Inflate the bounds a bit to ensure CSG operations near the boundary of chunks are processed by all nearby chunks.
            Bounds bounds = new Bounds(position, 2.0f * scale);
            int3 minChunkCoordinate = CalculateChunkCoordinate(bounds.min);
            int3 maxChunkCoordinate = CalculateChunkCoordinate(bounds.max);

            for (int3 chunkCoordinate = minChunkCoordinate; chunkCoordinate.z <= maxChunkCoordinate.z; chunkCoordinate.z++)
            {
                for (chunkCoordinate.y = minChunkCoordinate.y; chunkCoordinate.y <= maxChunkCoordinate.y; chunkCoordinate.y++)
                {
                    for (chunkCoordinate.x = minChunkCoordinate.x; chunkCoordinate.x <= maxChunkCoordinate.x; chunkCoordinate.x++)
                    {
                        if (m_chunks.TryGetValue(chunkCoordinate, out Chunk chunk))
                        {
                            chunk.ApplyCSGPrimitiveOperation(csgOperator, csgPrimitive, worldToObjectMatrix);
                        }
                    }
                }
            }
        }

        private float3 SnapToGrid(float3 number)
        {
            float voxelSpacing = m_voxelConfig.VoxelVolumeConfig.VoxelSpacing;
            number = math.round(number / voxelSpacing) * voxelSpacing;

            return number;
        }

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
                m_sharedChunkPool.Release(m_chunks[chunkCoordinate]);
                m_chunks.Remove(chunkCoordinate);
            }
            m_oldChunkCoordinates.Clear();
        }

        private IEnumerator CreateChunksWithinViewDistanceCoroutine(float3 viewerPosition, int maxNumberOfChunksProcessedEachFrame)
        {
            int numberOfChunksProcessed = 0;
            int3 chunkCoordinate = CalculateChunkCoordinate(viewerPosition);
            float3 chunkPosition = chunkCoordinate * m_chunkDimensions;
            float viewerToChunkDistanceSquared = math.lengthsq(chunkPosition - viewerPosition);
            int lod = CalculateChunkLod(viewerToChunkDistanceSquared);

            m_processedChunkCoordinates.Clear();
            m_chunksToProcess.Clear();
            m_chunksToProcess.Enqueue((chunkCoordinate, chunkPosition, lod));

            while (m_chunksToProcess.Count > 0)
            {
                (chunkCoordinate, chunkPosition, lod) = m_chunksToProcess.Dequeue();

                if (m_chunks.TryGetValue(chunkCoordinate, out Chunk chunk))
                {
                    if (chunk.Lod != lod)
                    {
                        chunk.Lod = lod;
                        chunk.RegenerateMesh();
                    }
                }
                else
                {
                    // Create new chunk.
                    chunk = m_sharedChunkPool.Acquire((chunk) =>
                    {
                        chunk.transform.position = chunkPosition;
                        chunk.Lod = lod;
                        chunk.RegenerateVoxelVolume();
                        chunk.RegenerateMesh();
                    });
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
                    m_chunksToProcess.Enqueue((neighbourChunkCoordinate, neighbourChunkPosition, CalculateChunkLod(viewerToNeighbourChunkDistanceSquared)));
                }
            }
        }

        private float3 CalculateChunkDimensions()
        {
            const int voxelOverlap = 1;
            float inflationFactor = 1.0f + (float)voxelOverlap / (VoxelConfig.VoxelVolumeConfig.NumberOfCellsAlongAxis - voxelOverlap);

            return VoxelConfig.VoxelVolumeConfig.VoxelVolumeDimensions / inflationFactor;
        }

        private int3 CalculateChunkCoordinate(float3 position) => (int3)math.round(position / m_chunkDimensions);

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
                m_sharedChunkPool.Release(chunk);
            }
            m_chunks.Clear();

            UpdateWorld(m_viewer.position, m_maxNumberOfChunksProcessedEachFrame);
            m_applySettingsCoroutine = null;
        }

        private void ApplyVoxelVolumeConfig() => ApplySettings();

        private void ApplyDualContouringConfig()
        {
            foreach (Chunk chunk in m_chunks.Values)
            {
                chunk.RegenerateMesh();
            }
        }

        private void ApplyNoiseGraph()
        {
            foreach (Chunk chunk in m_chunks.Values)
            {
                chunk.RegenerateVoxelVolume();
                chunk.RegenerateMesh();
            }
        }
    }
}
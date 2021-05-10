using Generics;
using Generics.Pool;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Voxels;
using Voxels.Config;

namespace World
{
    [RequireComponent(typeof(VoxelVolume), typeof(DualContouring))]
    public class World : SingletonComponent<World>
    {
        internal static VoxelVolume VoxelVolume => Instance.m_voxelVolume;
        internal static DualContouring DualContouring => Instance.m_dualContouring;
        internal static ObjectPool<Chunk> SharedChunkPool => Instance.m_sharedChunkPool;

        [Header("General")]
        [Tooltip("Specifies how much the viewer needs to move to cause an update of world.")]
        [Min(0.0f)]
        [SerializeField]
        private float m_worldUpdateInterval = 126.0f;
        [SerializeField]
        private GameObject m_chunkPrefab;
        [Tooltip("Overlapping cells of adjacent chunks hide LOD seams but increases inter-chunk dependency.")]
        [Range(0, 4)]
        [SerializeField]
        private int m_cellOverlap = 2;

        [Header("Level of Detail")]
        [Range(0, 10)]
        [SerializeField]
        private int m_numberOfLods = 4;
        [Tooltip("Determines how aggressive lodding along the given axis is.")]
        [SerializeField]
        private float3 m_lodDistanceFactor = 1.0f;
        [Tooltip("Specifies how much the viewer needs to move to cause an update of the level of detail.")]
        [Min(0.0f)]
        [SerializeField]
        private float m_lodUpdateInterval = 15.75f;
        [SerializeField]
        private Transform m_viewer;

        [Header("Editor")]
        [SerializeField]
        private Color m_gizmoColor = Color.black;
        [SerializeField]
        private bool m_showBounds = false;

        private VoxelVolume m_voxelVolume;
        private DualContouring m_dualContouring;
        private ObjectPool<Chunk> m_sharedChunkPool;
        private ObjectPool<LodTree> m_lodTreePool;
        private Dictionary<int3, LodTree> m_lodTrees;
        private HashSet<int3> m_oldLodTrees;
        private float m_lodTreeInflationFactor;
        private float3 m_lodTreeDimensions;
        private int m_numberOfLodTreesVisibleAlongAxis;

        // We don't want to update the visible world every frame.
        // Sample applies to the level of details of the chunks.
        private float3 m_lastViewerPositionWorld;
        private float3 m_lastViewerPositionLod;
        private float m_worldUpdateIntervalSquared;
        private float m_lodUpdateIntervalSquared;

        private WorldFlags m_flags;

        private void Awake()
        {
            Assert.IsFalse(m_chunkPrefab.activeSelf);

#if UNITY_EDITOR
            VoxelConfigs.VoxelVolumeConfig.OnDirty += ApplyVoxelVolumeConfig;
            VoxelConfigs.DualContouringConfig.OnDirty += ApplyDualContouringConfig;
            VoxelConfigs.NoiseConfig.OnDirty += ApplyNoiseConfig;
#endif

            m_voxelVolume = GetComponent<VoxelVolume>();
            m_dualContouring = GetComponent<DualContouring>();
            m_sharedChunkPool = new ObjectPool<Chunk>(() =>
            {
                Chunk chunk = Instantiate(Instance.m_chunkPrefab, Instance.transform).GetComponent<Chunk>();
                chunk.CreateBuffers(VoxelConfigs.VoxelVolumeConfig.NumberOfVoxels);

                return chunk;
            });
            m_lodTreePool = new ObjectPool<LodTree>(() => { return new LodTree(); });
            m_lodTrees = new Dictionary<int3, LodTree>();
            m_oldLodTrees = new HashSet<int3>();
            m_lodTreeInflationFactor = CalculateLodTreeInflationFactor();
            m_lodTreeDimensions = CalculateLodTreeDimensions();
            m_numberOfLodTreesVisibleAlongAxis = 1;

            m_lastViewerPositionWorld = m_viewer.position;
            m_lastViewerPositionLod = m_viewer.position;
            m_worldUpdateIntervalSquared = m_worldUpdateInterval * m_worldUpdateInterval;
            m_lodUpdateIntervalSquared = m_lodUpdateInterval * m_lodUpdateInterval;

            UpdateVisibleWorld();
            UpdateLodOfVisibleWorld();
        }

        private void Update()
        {
            if (math.lengthsq((float3)m_viewer.position - m_lastViewerPositionWorld) >= m_worldUpdateIntervalSquared)
            {
                m_lastViewerPositionWorld = m_viewer.position;
                UpdateVisibleWorld();
            }

            if (math.lengthsq((float3)m_viewer.position - m_lastViewerPositionLod) >= m_lodUpdateIntervalSquared)
            {
                m_lastViewerPositionLod = m_viewer.position;
                UpdateLodOfVisibleWorld();
            }

            if (m_flags.HasFlag(WorldFlags.SettingsUpdated))
            {
                m_flags &= ~WorldFlags.SettingsUpdated;
                OnSettingsUpdated();
            }
        }

        private void OnDrawGizmos()
        {
            if (m_lodTrees == null || !m_showBounds)
            {
                return;
            }

            Gizmos.color = m_gizmoColor;

            foreach (LodTree lodTree in m_lodTrees.Values)
            {
                foreach ((_, Bounds bounds) in lodTree.Traverse(true))
                {
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }

        private void OnValidate()
        {
            m_lodDistanceFactor = math.clamp(m_lodDistanceFactor, 0.5f, 1.5f);
            m_flags |= WorldFlags.SettingsUpdated;
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            VoxelConfigs.VoxelVolumeConfig.OnDirty -= ApplyVoxelVolumeConfig;
            VoxelConfigs.DualContouringConfig.OnDirty -= ApplyDualContouringConfig;
            VoxelConfigs.NoiseConfig.OnDirty -= ApplyNoiseConfig;
#endif
        }

        private float CalculateLodTreeInflationFactor() => 1.0f + (float)m_cellOverlap / VoxelConfigs.VoxelVolumeConfig.CellVolumeCount.x;

        private float3 CalculateLodTreeDimensions() => VoxelConfigs.VoxelVolumeConfig.GetCellVolumeDimensions(m_numberOfLods) / m_lodTreeInflationFactor;

        private int3 CalculateViewerLodTreeCoordinate() => (int3)math.round(m_viewer.position / m_lodTreeDimensions);

        private void UpdateVisibleWorld()
        {
            // Remember the currently active lod trees.
            m_oldLodTrees.Clear();
            m_oldLodTrees.UnionWith(m_lodTrees.Keys);

            int3 viewerLodTreeCoordinate = CalculateViewerLodTreeCoordinate();

            // Create new lod trees.
            for (int z = -m_numberOfLodTreesVisibleAlongAxis; z <= m_numberOfLodTreesVisibleAlongAxis; z++)
            {
                for (int y = -m_numberOfLodTreesVisibleAlongAxis; y <= m_numberOfLodTreesVisibleAlongAxis; y++)
                {
                    for (int x = -m_numberOfLodTreesVisibleAlongAxis; x <= m_numberOfLodTreesVisibleAlongAxis; x++)
                    {
                        int3 lodTreeCoordinate = viewerLodTreeCoordinate + new int3(x, y, z);

                        if (!m_lodTrees.ContainsKey(lodTreeCoordinate))
                        {
                            LodTree lodTree = m_lodTreePool.Acquire((lodTree) =>
                            {
                                lodTree.Bounds = new Bounds(lodTreeCoordinate * m_lodTreeDimensions, m_lodTreeDimensions);
                                lodTree.MaxDepth = m_numberOfLods;
                                lodTree.DistanceFactor = m_lodDistanceFactor;
                                lodTree.InflationFactor = m_lodTreeInflationFactor;
                            });
                            m_lodTrees[lodTreeCoordinate] = lodTree;
                        }
                        m_oldLodTrees.Remove(lodTreeCoordinate);
                    }
                }
            }

            // Remove old lod trees.
            foreach (int3 lodTreeCoordinate in m_oldLodTrees)
            {
                m_lodTreePool.Release(m_lodTrees[lodTreeCoordinate]);
                m_lodTrees.Remove(lodTreeCoordinate);
            }
        }

        private void UpdateLodOfVisibleWorld()
        {
            foreach (LodTree lodTree in m_lodTrees.Values)
            {
                lodTree.Update(m_viewer.position);
            }
        }

        private void OnSettingsUpdated()
        {
            m_worldUpdateIntervalSquared = m_worldUpdateInterval * m_worldUpdateInterval;
            m_lodUpdateIntervalSquared = m_lodUpdateInterval * m_lodUpdateInterval;

            m_lodTreeInflationFactor = CalculateLodTreeInflationFactor();
            m_lodTreeDimensions = CalculateLodTreeDimensions();

            foreach (LodTree lodTree in m_lodTrees.Values)
            {
                m_lodTreePool.Release(lodTree);
            }
            m_lodTrees.Clear();

            UpdateVisibleWorld();
            UpdateLodOfVisibleWorld();
        }

        private void ApplyVoxelVolumeConfig()
        {
            SharedChunkPool.Apply((chunk, inUse) =>
            {
                chunk.CreateBuffers(VoxelConfigs.VoxelVolumeConfig.NumberOfVoxels);

                if (!inUse)
                {
                    return;
                }

                VoxelVolume.GenerateVoxelVolume(chunk);
                DualContouring.RequestMeshGeneration(chunk);
            });

            m_flags |= WorldFlags.SettingsUpdated;
        }

        private void ApplyDualContouringConfig()
        {
            SharedChunkPool.Apply((chunk, inUse) =>
            {
                if (!inUse)
                {
                    return;
                }

                DualContouring.RequestMeshGeneration(chunk);
            });
        }

        private void ApplyNoiseConfig()
        {
            SharedChunkPool.Apply((chunk, inUse) =>
            {
                if (!inUse)
                {
                    return;
                }

                VoxelVolume.GenerateVoxelVolume(chunk);
                DualContouring.RequestMeshGeneration(chunk);
            });
        }

        [Flags]
        private enum WorldFlags
        {
            SettingsUpdated = 1
        }
    }
}
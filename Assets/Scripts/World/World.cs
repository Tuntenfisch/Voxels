using Generics;
using Generics.Pool;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Voxels;

namespace World
{
    [RequireComponent(typeof(VoxelVolume), typeof(CubicalMarchingSquares))]
    public class World : SingletonComponent<World>
    {
#if UNITY_EDITOR
        public static readonly string NumberOfLodTreesVisibleAlongAxisPropertyName = nameof(m_numberOfLodTreesVisibleAlongAxis);
        public static readonly string LodTreeDimensionsPropertyName = nameof(m_lodTreeDimensions);
#endif

        [Header("General")]
        [SerializeField]
        private Configuration m_configuration;
        [Min(0.0f)]
        [SerializeField]
        private float m_worldUpdateInterval = 126.0f;
        [SerializeField]
        private GameObject m_chunkPrefab;

        [Header("Level of Detail")]
        [Min(0.0f)]
        [SerializeField]
        private float m_lodUpdateInterval = 15.75f;
        [Range(0, 10)]
        [SerializeField]
        private int m_numberOfLods = 4;
        [SerializeField]
        private Transform m_viewer;

        [Header("Editor")]
        [SerializeField]
        private bool m_showBounds = false;

        private VoxelVolume m_voxelVolume;
        private CubicalMarchingSquares m_cubicalMarchingSquares;

        private ObjectPool<Chunk> m_chunkPool;
        private Dictionary<Bounds, Chunk> m_chunks;
        private HashSet<Bounds> m_oldChunks;

        private ObjectPool<LodTree> m_lodTreePool;
        private Dictionary<int3, LodTree> m_lodTrees;
        private HashSet<int3> m_oldLodTrees;
        [HideInInspector]
        [SerializeField]
        private float3 m_lodTreeDimensions;
        [HideInInspector]
        [SerializeField]
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
            Assert.IsNotNull(m_chunkPrefab);
            Assert.IsFalse(m_chunkPrefab.activeSelf);
            Assert.IsNotNull(m_viewer);

            m_configuration.OnDirty += OnValidate;
            m_voxelVolume = GetComponent<VoxelVolume>();
            m_cubicalMarchingSquares = GetComponent<CubicalMarchingSquares>();

            m_chunkPool = new ObjectPool<Chunk>(() =>
            {
                GameObject gameObject = Instantiate(m_chunkPrefab, transform);
                gameObject.SetActive(false);
                Chunk chunk = gameObject.GetComponent<Chunk>();
                chunk.CreateBuffers(m_configuration.NumberOfVoxels);

                return chunk;
            }, 100, 50);
            m_chunks = new Dictionary<Bounds, Chunk>();
            m_oldChunks = new HashSet<Bounds>();

            m_lodTreePool = new ObjectPool<LodTree>(() => { return new LodTree(); }, 20, 10);
            m_lodTrees = new Dictionary<int3, LodTree>();
            m_oldLodTrees = new HashSet<int3>();
            m_lodTreeDimensions = (1 << m_numberOfLods) * m_configuration.CellVolumeDimensions;
            m_numberOfLodTreesVisibleAlongAxis = 1;

            m_lastViewerPositionWorld = m_viewer.position;
            m_lastViewerPositionLod = m_viewer.position;
            m_worldUpdateIntervalSquared = m_worldUpdateInterval * m_worldUpdateInterval;
            m_lodUpdateIntervalSquared = m_lodUpdateInterval * m_lodUpdateInterval;

            ResetWorld();
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

        private void OnDestroy()
        {
            m_configuration.OnDirty -= OnValidate;
            m_chunkPool.Apply((chunk, inUse) => { chunk.ReleaseBuffers(); });
        }

        private void OnDrawGizmos()
        {
            if (m_lodTrees == null || !m_showBounds)
            {
                return;
            }

            foreach (LodTree lodTree in m_lodTrees.Values)
            {
                foreach (LodTree.Node leaf in lodTree.Traverse(true))
                {
                    Gizmos.DrawWireCube(leaf.Bounds.center, leaf.Bounds.size);
                }
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_lodTreeDimensions = (1 << m_numberOfLods) * m_configuration.CellVolumeDimensions;
            }
#endif
            m_flags |= WorldFlags.SettingsUpdated;
        }

        private void UpdateVisibleWorld()
        {
            m_oldLodTrees.Clear();
            m_oldLodTrees.UnionWith(m_lodTrees.Keys);

            int3 viewerLodTreeCoordinate = CalculateViewerLodTreeCoordinate();

            for (int z = -m_numberOfLodTreesVisibleAlongAxis; z <= m_numberOfLodTreesVisibleAlongAxis; z++)
            {
                for (int y = -m_numberOfLodTreesVisibleAlongAxis; y <= m_numberOfLodTreesVisibleAlongAxis; y++)
                {
                    for (int x = -m_numberOfLodTreesVisibleAlongAxis; x <= m_numberOfLodTreesVisibleAlongAxis; x++)
                    {
                        int3 lodTreeCoordinate = viewerLodTreeCoordinate + new int3(x, y, z);

                        if (!m_lodTrees.ContainsKey(lodTreeCoordinate))
                        {
                            LodTree lodTree = m_lodTreePool.Acquire();
                            lodTree.Initialize(new Bounds(lodTreeCoordinate * m_lodTreeDimensions, m_lodTreeDimensions), m_numberOfLods);
                            m_lodTrees[lodTreeCoordinate] = lodTree;
                        }
                        m_oldLodTrees.Remove(lodTreeCoordinate);
                    }
                }
            }

            foreach (int3 lodTreeCoordinate in m_oldLodTrees)
            {
                m_lodTreePool.Release(m_lodTrees[lodTreeCoordinate]);
                m_lodTrees.Remove(lodTreeCoordinate);
            }
        }

        private void UpdateLodOfVisibleWorld()
        {
            m_oldChunks.Clear();
            m_oldChunks.UnionWith(m_chunks.Keys);

            foreach (LodTree lodTree in m_lodTrees.Values)
            {
                lodTree.Update(m_viewer.position);

                foreach (LodTree.Node leaf in lodTree.Traverse(true))
                {
                    if (!m_chunks.ContainsKey(leaf.Bounds))
                    {
                        Chunk chunk = m_chunkPool.Acquire();
                        chunk.transform.position = leaf.Bounds.center;
                        chunk.VoxelSpacing = leaf.Bounds.size.x / m_configuration.NumberOfCellsAlongAxis;
                        m_voxelVolume.GenerateVoxelVolume(chunk);
                        m_cubicalMarchingSquares.RequestMeshGeneration(chunk);
                        chunk.gameObject.SetActive(true);
                        m_chunks[leaf.Bounds] = chunk;
                    }
                    m_oldChunks.Remove(leaf.Bounds);
                }
            }

            foreach (Bounds bounds in m_oldChunks)
            {
                Chunk chunk = m_chunks[bounds];
                chunk.gameObject.SetActive(false);
                m_chunkPool.Release(chunk);
                m_chunks.Remove(bounds);
            }
        }

        private int3 CalculateViewerLodTreeCoordinate() => (int3)math.round(m_viewer.position / m_lodTreeDimensions);

        private void OnSettingsUpdated()
        {
            m_worldUpdateIntervalSquared = m_worldUpdateInterval * m_worldUpdateInterval;
            m_lodUpdateIntervalSquared = m_lodUpdateInterval * m_lodUpdateInterval;

            m_chunkPool.Apply((chunk, inUse) =>
            {
                chunk.CreateBuffers(m_configuration.NumberOfVoxels);

                if (inUse)
                {
                    m_voxelVolume.GenerateVoxelVolume(chunk);
                    m_cubicalMarchingSquares.RequestMeshGeneration(chunk);
                }
            });

            float3 lodTreeDimensions = (1 << m_numberOfLods) * m_configuration.CellVolumeDimensions;

            if (math.any(m_lodTreeDimensions != lodTreeDimensions))
            {
                m_lodTreeDimensions = lodTreeDimensions;
                ResetWorld();
            }
        }

        private void ResetWorld()
        {
            foreach (LodTree lodTree in m_lodTrees.Values)
            {
                m_lodTreePool.Release(lodTree);
            }
            m_lodTrees.Clear();

            foreach (Chunk chunk in m_chunks.Values)
            {
                chunk.gameObject.SetActive(false);
                m_chunkPool.Release(chunk);
            }
            m_chunks.Clear();

            UpdateVisibleWorld();
            UpdateLodOfVisibleWorld();
        }

        [Flags]
        private enum WorldFlags
        {
            SettingsUpdated = 1
        }
    }
}
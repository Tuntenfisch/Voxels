using System;
using UnityEngine;

namespace CubicalMarchingSquares
{
    public class VoxelVolume : MonoBehaviour
    {
        public event Action OnVoxelVolumeChanged;

        [Header("Noise Parameters")]
        [SerializeField]
        private int m_seed;
        [SerializeField]
        private Vector3 m_offset;


        [Header("Height Map")]
        [Range(0.0f, 100.0f)]
        [SerializeField]
        private float m_height = 15.0f;
        [Range(0.0f, 100.0f)]
        [SerializeField]
        private float m_wavelength = 35.0f;

        [Header("FBM")]
        [Range(1, 16)]
        [SerializeField]
        private int m_numberOfOctaves = 4;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_persistence = 0.5f;
        [Range(1.0f, 4.0f)]
        [SerializeField]
        private float m_lacunarity = 2.0f;

        [Header("Shader")]
        [SerializeField]
        private ComputeShader m_computeShader;

        private static readonly int s_cellDimensionsID = Shader.PropertyToID("cellDimensions");
        private static readonly int s_cellSpacingID = Shader.PropertyToID("cellSpacing");
        private static readonly int s_cellVolumeToWorldSpaceOffsetID = Shader.PropertyToID("cellVolumeToWorldSpaceOffset");
        private static readonly int s_voxelDimensionsID = Shader.PropertyToID("voxelDimensions");
        private static readonly int s_wavelengthID = Shader.PropertyToID("wavelength");
        private static readonly int s_numberOfOctavesID = Shader.PropertyToID("numberOfOctaves");
        private static readonly int s_persistenceID = Shader.PropertyToID("persistence");
        private static readonly int s_lacunarityID = Shader.PropertyToID("lacunarity");
        private static readonly int s_heightID = Shader.PropertyToID("height");
        private static readonly int s_voxelVolumeID = Shader.PropertyToID("voxelVolume");
        private static readonly int s_octaveOffsetsID = Shader.PropertyToID("octaveOffsets");

        private Vector3Int m_numberOfThreads;
        private ComputeBuffer m_octaveOffsetsBuffer;
        private VoxelVolumeFlags m_flags;

        private void OnEnable()
        {
            m_computeShader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
            m_numberOfThreads = new Vector3Int((int)x, (int)y, (int)z);

            CreateBuffers();
            CalculateOctaveOffsets();
        }

        private void CreateBuffers()
        {
            m_octaveOffsetsBuffer = new ComputeBuffer(m_numberOfOctaves, 3 * sizeof(float));
        }

        private void ReleaseBuffers()
        {
            m_octaveOffsetsBuffer.Release();
            m_octaveOffsetsBuffer = null;
        }

        private void Update()
        {
            if (m_flags.HasFlag(VoxelVolumeFlags.SettingsUpdated))
            {
                OnSettingsUpdated();
            }
        }

        private void OnSettingsUpdated()
        {
            m_flags &= ~VoxelVolumeFlags.SettingsUpdated;

            if (m_octaveOffsetsBuffer.count != m_numberOfOctaves)
            {
                ReleaseBuffers();
                CreateBuffers();
            }

            CalculateOctaveOffsets();

            OnVoxelVolumeChanged();
        }

        private void CalculateOctaveOffsets()
        {
            System.Random random = new System.Random(m_seed);
            Vector3[] octaveOffsets = new Vector3[m_numberOfOctaves];

            for (int index = 0; index < m_numberOfOctaves; index++)
            {
                octaveOffsets[index] = new Vector3(random.Next(-100000, 100000), random.Next(-100000, 100000), random.Next(-100000, 100000)) + m_offset;
            }
            m_octaveOffsetsBuffer.SetData(octaveOffsets);
        }

        public void Generate
        (
            ComputeBuffer voxelVolumeBuffer,
            int numberOfVoxelsAlongAxis,
            int numberOfCellsAlongAxis,
            float cellSpacing,
            Vector3 localToWorldOffset
        )
        {
            m_computeShader.SetInts(s_cellDimensionsID, numberOfCellsAlongAxis, numberOfCellsAlongAxis, numberOfCellsAlongAxis);
            m_computeShader.SetFloat(s_cellSpacingID, cellSpacing);
            m_computeShader.SetVector(s_cellVolumeToWorldSpaceOffsetID, localToWorldOffset);
            m_computeShader.SetInts(s_voxelDimensionsID, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis);
            m_computeShader.SetFloat(s_wavelengthID, m_wavelength);
            m_computeShader.SetInt(s_numberOfOctavesID, m_numberOfOctaves);
            m_computeShader.SetFloat(s_persistenceID, m_persistence);
            m_computeShader.SetFloat(s_lacunarityID, m_lacunarity);
            m_computeShader.SetFloat(s_heightID, m_height);

            m_computeShader.SetBuffer(0, s_voxelVolumeID, voxelVolumeBuffer);
            m_computeShader.SetBuffer(0, s_octaveOffsetsID, m_octaveOffsetsBuffer);
            m_computeShader.Dispatch
            (
                0,
                Mathf.CeilToInt(numberOfVoxelsAlongAxis / (float)m_numberOfThreads.x),
                Mathf.CeilToInt(numberOfVoxelsAlongAxis / (float)m_numberOfThreads.y),
                Mathf.CeilToInt(numberOfVoxelsAlongAxis / (float)m_numberOfThreads.z)
            );
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        private void OnValidate()
        {
            m_flags |= VoxelVolumeFlags.SettingsUpdated;
        }

        private enum VoxelVolumeFlags
        {
            SettingsUpdated = 1
        }
    }
}
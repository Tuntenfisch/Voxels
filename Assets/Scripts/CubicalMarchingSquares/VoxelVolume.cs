using System;
using UnityEngine;
using UnityEditor;

namespace CubicalMarchingSquares
{
    [ExecuteInEditMode]
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

        private Vector3Int m_numberOfThreadsKernel0;
        private ComputeBuffer m_octaveOffsetsBuffer;
        private VoxelVolumeFlags m_flags;

        private void OnEnable()
        {
            SetupNumberOfThreadsPerKernel();
            CreateBuffers();
            CalculateOctaveOffsets();
        }

        private void SetupNumberOfThreadsPerKernel()
        {
            m_computeShader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
            m_numberOfThreadsKernel0 = new Vector3Int((int)x, (int)y, (int)z);
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
                octaveOffsets[index] = new Vector3(random.Next(-10000, 10000), random.Next(-10000, 10000), random.Next(-10000, 10000)) + m_offset;
            }
            m_octaveOffsetsBuffer.SetData(octaveOffsets);
        }

        private void SetupDispatch(ComputeBuffer voxelVolumeBuffer, int numberOfVoxelsAlongAxis, int numberOfCellsAlongAxis, float cellSpacing, Vector3 localToWorldOffset)
        {
            m_computeShader.SetInts(ComputeShaderProperties.s_cellDimensions, numberOfCellsAlongAxis, numberOfCellsAlongAxis, numberOfCellsAlongAxis);
            m_computeShader.SetFloat(ComputeShaderProperties.s_cellSpacing, cellSpacing);
            m_computeShader.SetVector(ComputeShaderProperties.s_cellVolumeToWorldSpaceOffset, localToWorldOffset);
            m_computeShader.SetInts(ComputeShaderProperties.s_voxelDimensions, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis, numberOfVoxelsAlongAxis);
            m_computeShader.SetFloat(ComputeShaderProperties.s_wavelength, m_wavelength);
            m_computeShader.SetInt(ComputeShaderProperties.s_numberOfOctaves, m_numberOfOctaves);
            m_computeShader.SetFloat(ComputeShaderProperties.s_persistence, m_persistence);
            m_computeShader.SetFloat(ComputeShaderProperties.s_lacunarity, m_lacunarity);
            m_computeShader.SetFloat(ComputeShaderProperties.s_height, m_height);

            // Link buffers for kernel 0.
            m_computeShader.SetBuffer(0, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
            m_computeShader.SetBuffer(0, ComputeShaderProperties.s_octaveOffsets, m_octaveOffsetsBuffer);
        }

        public void Generate(ComputeBuffer voxelVolumeBuffer, int numberOfVoxelsAlongAxis, int numberOfCellsAlongAxis, float cellSpacing, Vector3 localToWorldOffset)
        {
            SetupDispatch(voxelVolumeBuffer, numberOfVoxelsAlongAxis, numberOfCellsAlongAxis, cellSpacing, localToWorldOffset);

            Vector3Int numberOfWorkGroupsKernel0 = new Vector3Int
            (
                Mathf.CeilToInt(numberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel0.x),
                Mathf.CeilToInt(numberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel0.y),
                Mathf.CeilToInt(numberOfVoxelsAlongAxis / (float)m_numberOfThreadsKernel0.z)
            );

            m_computeShader.Dispatch(0, numberOfWorkGroupsKernel0.x, numberOfWorkGroupsKernel0.y, numberOfWorkGroupsKernel0.z);
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
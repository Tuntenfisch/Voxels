using Generics;
using UnityEngine;
using Utils;

namespace Voxel
{
    public class VoxelVolume : SingletonComponent<VoxelVolume>
    {
        public Configuration Configuration => m_configuration;

        private ComputeShader ComputeShader => Configuration.VoxelVolumeCompute;

        [SerializeField]
        private Configuration m_configuration;

        private ComputeBuffer m_octaveOffsetsBuffer;

        private void OnEnable()
        {
            Configuration.OnDirty += OnConfigurationDirty;
            CreateBuffers();
            CalculateOctaveOffsets();
        }

        private void OnDisable()
        {
            Configuration.OnDirty -= OnConfigurationDirty;
            ReleaseBuffers();
        }

        public void GenerateVoxelVolume(ComputeBuffer voxelVolumeBuffer, Vector3 worldPosition)
        {
            SetupVoxelVolumeGeneration(voxelVolumeBuffer, worldPosition);

            ComputeShader.Dispatch(0, Configuration.VoxelVolumeDimensions);
        }

        private void OnConfigurationDirty()
        {
            if (m_octaveOffsetsBuffer.count != Configuration.NumberOfOctaves)
            {
                ReleaseBuffers();
                CreateBuffers();
            }
            CalculateOctaveOffsets();
        }

        private void CreateBuffers()
        {
            if (m_octaveOffsetsBuffer != null)
            {
                return;
            }

            m_octaveOffsetsBuffer = new ComputeBuffer(Configuration.NumberOfOctaves, 3 * sizeof(float));
        }

        private void ReleaseBuffers()
        {
            if (m_octaveOffsetsBuffer == null)
            {
                return;
            }

            m_octaveOffsetsBuffer.Release();
            m_octaveOffsetsBuffer = null;
        }

        private void CalculateOctaveOffsets()
        {
            System.Random random = new System.Random(Configuration.Seed);
            Vector3[] octaveOffsets = new Vector3[Configuration.NumberOfOctaves];

            for (int index = 0; index < octaveOffsets.Length; index++)
            {
                octaveOffsets[index] = new Vector3(random.Next(-10000, 10000), random.Next(-10000, 10000), random.Next(-10000, 10000)) + Configuration.Offset;
            }
            m_octaveOffsetsBuffer.SetData(octaveOffsets);
            Configuration.VoxelVolumeCompute.SetBuffer(0, ComputeShaderProperties.s_octaveOffsets, m_octaveOffsetsBuffer);
        }

        private void SetupVoxelVolumeGeneration(ComputeBuffer voxelVolumeBuffer, Vector3 worldPosition)
        {
            ComputeShader.SetVector(ComputeShaderProperties.s_cellVolumeToWorldSpaceOffset, worldPosition);
            ComputeShader.SetBuffer(0, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
        }
    }
}
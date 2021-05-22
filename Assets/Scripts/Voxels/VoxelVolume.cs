using System;
using Tuntenfisch.Extensions;
using Tuntenfisch.Voxels.Config;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels
{
    [RequireComponent(typeof(VoxelConfigs))]
    public class VoxelVolume : MonoBehaviour
    {
        private VoxelConfigs m_voxelConfigs;

        private void Awake()
        {
            m_voxelConfigs = GetComponent<VoxelConfigs>();
#if UNITY_EDITOR
            m_voxelConfigs.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
            m_voxelConfigs.NoiseConfig.OnDirtied += ApplyNoiseConfig;
#endif
            ApplyVoxelVolumeConfig();
            ApplyNoiseConfig();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            m_voxelConfigs.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
            m_voxelConfigs.NoiseConfig.OnDirtied -= ApplyNoiseConfig;
#endif
        }

        public void GenerateVoxelVolume(ComputeBuffer voxelVolumeBuffer, float3 worldPosition)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            SetupVoxelVolumeGeneration(voxelVolumeBuffer, worldPosition);

            m_voxelConfigs.VoxelVolumeConfig.Compute.Dispatch(0, m_voxelConfigs.VoxelVolumeConfig.NumberOfVoxels);
        }

        private void SetupVoxelVolumeGeneration(ComputeBuffer voxelVolumeBuffer, float3 voxelVolumeToWorldOffset)
        {
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)voxelVolumeToWorldOffset);

            // Link buffer for kernel 0.
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = m_voxelConfigs.VoxelVolumeConfig.NumberOfVoxels;
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, m_voxelConfigs.VoxelVolumeConfig.VoxelSpacing);
        }

        private void ApplyNoiseConfig()
        {
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.Seed, m_voxelConfigs.NoiseConfig.Seed);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Wavelength, m_voxelConfigs.NoiseConfig.WaveLength);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.NumberOfOctaves, m_voxelConfigs.NoiseConfig.NumberOfOctaves);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Persistence, m_voxelConfigs.NoiseConfig.Persistence);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Lacunarity, m_voxelConfigs.NoiseConfig.Lacunarity);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Height, m_voxelConfigs.NoiseConfig.Height);
        }
    }
}
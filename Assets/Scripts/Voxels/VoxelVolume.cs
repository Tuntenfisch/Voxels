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
        private void Awake()
        {
#if UNITY_EDITOR
            VoxelConfigs.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
            VoxelConfigs.NoiseConfig.OnDirtied += ApplyNoiseConfig;
#endif
            ApplyVoxelVolumeConfig();
            ApplyNoiseConfig();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            VoxelConfigs.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
            VoxelConfigs.NoiseConfig.OnDirtied -= ApplyNoiseConfig;
#endif
        }

        public void GenerateVoxelVolume(ComputeBuffer voxelVolumeBuffer, float3 worldPosition)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            SetupVoxelVolumeGeneration(voxelVolumeBuffer, worldPosition);

            VoxelConfigs.VoxelVolumeConfig.Compute.Dispatch(0, VoxelConfigs.VoxelVolumeConfig.NumberOfVoxels);
        }

        private void SetupVoxelVolumeGeneration(ComputeBuffer voxelVolumeBuffer, float3 voxelVolumeToWorldOffset)
        {
            VoxelConfigs.VoxelVolumeConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldOffset, (Vector3)voxelVolumeToWorldOffset);

            // Link buffer for kernel 0.
            VoxelConfigs.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = VoxelConfigs.VoxelVolumeConfig.NumberOfVoxels;
            VoxelConfigs.VoxelVolumeConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, VoxelConfigs.VoxelVolumeConfig.VoxelSpacing);
        }

        private void ApplyNoiseConfig()
        {
            VoxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.Seed, VoxelConfigs.NoiseConfig.Seed);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Wavelength, VoxelConfigs.NoiseConfig.WaveLength);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.NumberOfOctaves, VoxelConfigs.NoiseConfig.NumberOfOctaves);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Persistence, VoxelConfigs.NoiseConfig.Persistence);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Lacunarity, VoxelConfigs.NoiseConfig.Lacunarity);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.Height, VoxelConfigs.NoiseConfig.Height);
        }
    }
}
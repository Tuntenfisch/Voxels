using Extensions;
using System;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Config;

namespace Voxels
{
    [RequireComponent(typeof(VoxelConfigs))]
    public class VoxelVolume : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            VoxelConfigs.VoxelVolumeConfig.OnDirty += ApplyVoxelVolumeConfig;
            VoxelConfigs.NoiseConfig.OnDirty += ApplyNoiseConfig;
#endif
            ApplyVoxelVolumeConfig();
            ApplyNoiseConfig();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            VoxelConfigs.VoxelVolumeConfig.OnDirty -= ApplyVoxelVolumeConfig;
            VoxelConfigs.NoiseConfig.OnDirty -= ApplyNoiseConfig;
#endif
        }

        public void GenerateVoxelVolume(IVoxelVolume requester)
        {
            (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing) = requester.GetArguments();

            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            SetupVoxelVolumeGeneration(voxelVolumeBuffer, worldPosition, voxelSpacing);

            VoxelConfigs.VoxelVolumeConfig.Compute.Dispatch(0, VoxelConfigs.VoxelVolumeConfig.VoxelVolumeCount);
        }

        private void SetupVoxelVolumeGeneration(ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing)
        {
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.s_voxelSpacing, voxelSpacing);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetVector(ComputeShaderProperties.s_voxelVolumeToWorldOffset, (Vector3)worldPosition);

            // Link buffer for kernel 0.
            VoxelConfigs.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
        }

        private void ApplyVoxelVolumeConfig()
        {
            VoxelConfigs.VoxelVolumeConfig.Compute.SetInts(ComputeShaderProperties.s_voxelVolumeCount, VoxelConfigs.VoxelVolumeConfig.VoxelVolumeCount.x, VoxelConfigs.VoxelVolumeConfig.VoxelVolumeCount.y, VoxelConfigs.VoxelVolumeConfig.VoxelVolumeCount.z);
        }

        private void ApplyNoiseConfig()
        {
            VoxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.s_seed, VoxelConfigs.NoiseConfig.Seed);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.s_wavelength, VoxelConfigs.NoiseConfig.WaveLength);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.s_numberOfOctaves, VoxelConfigs.NoiseConfig.NumberOfOctaves);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.s_persistence, VoxelConfigs.NoiseConfig.Persistence);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.s_lacunarity, VoxelConfigs.NoiseConfig.Lacunarity);
            VoxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.s_height, VoxelConfigs.NoiseConfig.Height);
        }
    }
}
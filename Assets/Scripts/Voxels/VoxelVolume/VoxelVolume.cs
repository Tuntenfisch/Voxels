using System;
using Tuntenfisch.Extensions;
using Tuntenfisch.Voxels.Noise;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.VoxelVolume
{
    [RequireComponent(typeof(VoxelConfigs))]
    public class VoxelVolume : MonoBehaviour
    {
        private VoxelConfigs m_voxelConfigs;
        private ComputeBuffer m_noiseLayersBuffer;

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
            ReleaseBuffers();
        }

        public void GenerateVoxelVolume(ComputeBuffer voxelVolumeBuffer, float3 worldPosition)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            m_voxelConfigs.VoxelVolumeConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)worldPosition);

            m_voxelConfigs.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
            m_voxelConfigs.VoxelVolumeConfig.Compute.Dispatch(0, m_voxelConfigs.VoxelVolumeConfig.NumberOfVoxels);
        }

        private void CreateBuffers()
        {
            if (m_noiseLayersBuffer?.count == m_voxelConfigs.NoiseConfig.NoiseLayers.Length)
            {
                return;
            }

            ReleaseBuffers();

            m_noiseLayersBuffer = new ComputeBuffer(m_voxelConfigs.NoiseConfig.NoiseLayers.Length, NoiseParameters.SizeInBytes);
        }

        private void ReleaseBuffers()
        {
            if (m_noiseLayersBuffer == null)
            {
                return;
            }

            m_noiseLayersBuffer.Release();
            m_noiseLayersBuffer = null;
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = m_voxelConfigs.VoxelVolumeConfig.NumberOfVoxels;
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, m_voxelConfigs.VoxelVolumeConfig.VoxelSpacing);
        }

        private void ApplyNoiseConfig()
        {
            CreateBuffers();

            m_noiseLayersBuffer.SetData(m_voxelConfigs.NoiseConfig.NoiseLayers);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.NumberOfNoiseLayers, m_voxelConfigs.NoiseConfig.NoiseLayers.Length);
            m_voxelConfigs.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.NoiseLayers, m_noiseLayersBuffer);
        }
    }
}
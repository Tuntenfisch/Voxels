using Extensions;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Voxels
{
    public class VoxelVolume : MonoBehaviour
    {
        private ComputeShader ComputeShader => m_configuration.VoxelVolumeCompute;

        [Header("General")]
        [SerializeField]
        private Configuration m_configuration;

        private void Awake()
        {
            Assert.IsNotNull(m_configuration);
        }

        public void GenerateVoxelVolume(IVoxelVolume requester)
        {
            (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing, _) = requester.GetArguments();

            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            SetupVoxelVolumeGeneration(voxelVolumeBuffer, worldPosition, voxelSpacing);

            ComputeShader.Dispatch(0, m_configuration.VoxelVolumeCount);
        }

        private void SetupVoxelVolumeGeneration(ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing)
        {
            ComputeShader.SetFloat(ComputeShaderProperties.s_voxelSpacing, voxelSpacing);
            ComputeShader.SetVector(ComputeShaderProperties.s_voxelVolumeToWorldOffset, (Vector3)worldPosition);
            ComputeShader.SetBuffer(0, ComputeShaderProperties.s_voxelVolume, voxelVolumeBuffer);
        }
    }
}
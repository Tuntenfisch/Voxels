using System;
using Tuntenfisch.Extensions;
using Tuntenfisch.Voxels.Noise.Nodes;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels
{
    [RequireComponent(typeof(VoxelConfig))]
    public class VoxelVolume : MonoBehaviour
    {
        private VoxelConfig m_voxelConfig;
        private ComputeBuffer m_noiseGraphNodesBuffer;

        private void Awake()
        {
            m_voxelConfig = GetComponent<VoxelConfig>();
#if UNITY_EDITOR
            m_voxelConfig.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
            m_voxelConfig.NoiseGraph.OnDirtied += ApplyNoiseGraph;
#endif
            ApplyVoxelVolumeConfig();
            ApplyNoiseGraph();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            m_voxelConfig.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
            m_voxelConfig.NoiseGraph.OnDirtied -= ApplyNoiseGraph;
#endif
            ReleaseBuffers();
        }

        public void GenerateVoxelVolume(ComputeBuffer voxelVolumeBuffer, float3 worldPosition)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            m_voxelConfig.VoxelVolumeConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)worldPosition);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);
            m_voxelConfig.VoxelVolumeConfig.Compute.Dispatch(0, m_voxelConfig.VoxelVolumeConfig.NumberOfVoxels);
        }

        private void CreateBuffers()
        {
            if (m_noiseGraphNodesBuffer == null || m_noiseGraphNodesBuffer.count < m_voxelConfig.NoiseGraph.Nodes.Count)
            {
                m_noiseGraphNodesBuffer?.Release();
                m_noiseGraphNodesBuffer = new ComputeBuffer(math.max(m_voxelConfig.NoiseGraph.Nodes.Count, 1), GPUNoiseGraphNode.SizeInBytes);
            }
        }

        private void ReleaseBuffers()
        {
            if (m_noiseGraphNodesBuffer != null)
            {
                m_noiseGraphNodesBuffer.Release();
                m_noiseGraphNodesBuffer = null;
            }
        }

        private void ApplyVoxelVolumeConfig()
        {
            int3 numberOfVoxels = m_voxelConfig.VoxelVolumeConfig.NumberOfVoxels;
            m_voxelConfig.VoxelVolumeConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, m_voxelConfig.VoxelVolumeConfig.VoxelSpacing);
        }

        private void ApplyNoiseGraph()
        {
            CreateBuffers();

            m_noiseGraphNodesBuffer.SetData(m_voxelConfig.NoiseGraph.Nodes);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.NumberOfNoiseGraphNoiseNodes, m_voxelConfig.NoiseGraph.Nodes.Count);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.NoiseGraphNodes, m_noiseGraphNodesBuffer);
        }
    }
}
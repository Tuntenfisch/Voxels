using System;
using System.Collections.Generic;
using Tuntenfisch.Extensions;
using Tuntenfisch.Voxels.Noise.Nodes;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Volume
{
    [RequireComponent(typeof(VoxelConfig))]
    public class VoxelVolume : MonoBehaviour
    {
        [SerializeField]
        private int m_voxelVolumeCSGOperationsBufferCapacity = 10;

        private VoxelConfig m_voxelConfig;
        private ComputeBuffer m_noiseGraphNodesBuffer;
        private ComputeBuffer m_voxelVolumeCSGOperationsBuffer;

        private void Awake()
        {
            m_voxelConfig = GetComponent<VoxelConfig>();
            m_voxelConfig.VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
            m_voxelConfig.NoiseGraph.OnDirtied += ApplyNoiseGraph;
            ApplyVoxelVolumeConfig();
            ApplyNoiseGraph();
        }

        private void OnDestroy()
        {
            m_voxelConfig.VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
            m_voxelConfig.NoiseGraph.OnDirtied -= ApplyNoiseGraph;
            ReleaseBuffers();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && gameObject.activeSelf && m_voxelConfig != null)
            {
                CreateBuffers();
            }
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

        public void ApplyVoxelVolumeCSGOperations(ComputeBuffer voxelVolumeBuffer, float3 worldPosition, List<GPUVoxelVolumeCSGOperation> voxelVolumeCSGOperations)
        {
            if (voxelVolumeBuffer == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeBuffer));
            }

            if (voxelVolumeCSGOperations == null)
            {
                throw new ArgumentNullException(nameof(voxelVolumeCSGOperations));
            }

            m_voxelConfig.VoxelVolumeConfig.Compute.SetVector(ComputeShaderProperties.VoxelVolumeToWorldSpaceOffset, (Vector3)worldPosition);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(1, ComputeShaderProperties.VoxelVolume, voxelVolumeBuffer);

            for (int index = 0; index < voxelVolumeCSGOperations.Count;)
            {
                int stride = math.min(m_voxelVolumeCSGOperationsBuffer.count, voxelVolumeCSGOperations.Count - index);

                m_voxelVolumeCSGOperationsBuffer.SetData(voxelVolumeCSGOperations, index, 0, stride);
                m_voxelConfig.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.NumberOfVoxelVolumeCSGOperations, stride);
                m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(1, ComputeShaderProperties.VoxelVolumeCSGOperations, m_voxelVolumeCSGOperationsBuffer);
                m_voxelConfig.VoxelVolumeConfig.Compute.Dispatch(1, m_voxelConfig.VoxelVolumeConfig.NumberOfVoxels);

                index += stride; 
            }
        }

        private void CreateBuffers()
        {
            if (m_noiseGraphNodesBuffer == null || m_noiseGraphNodesBuffer.count != m_voxelConfig.NoiseGraph.Nodes.Count)
            {
                m_noiseGraphNodesBuffer?.Release();
                m_noiseGraphNodesBuffer = new ComputeBuffer(math.max(m_voxelConfig.NoiseGraph.Nodes.Count, 1), GPUNoiseGraphNode.SizeInBytes);
            }

            if (m_voxelVolumeCSGOperationsBuffer == null || m_voxelVolumeCSGOperationsBuffer.count != m_voxelVolumeCSGOperationsBufferCapacity)
            {
                m_voxelVolumeCSGOperationsBuffer?.Release();
                m_voxelVolumeCSGOperationsBuffer = new ComputeBuffer(m_voxelVolumeCSGOperationsBufferCapacity, GPUVoxelVolumeCSGOperation.SizeInBytes);
            }
        }

        private void ReleaseBuffers()
        {
            if (m_noiseGraphNodesBuffer != null)
            {
                m_noiseGraphNodesBuffer.Release();
                m_noiseGraphNodesBuffer = null;
            }

            if (m_voxelVolumeCSGOperationsBuffer != null)
            {
                m_voxelVolumeCSGOperationsBuffer.Release();
                m_voxelVolumeCSGOperationsBuffer = null;
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
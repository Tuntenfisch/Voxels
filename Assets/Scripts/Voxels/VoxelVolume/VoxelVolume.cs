using System;
using Tuntenfisch.Extensions;
using Tuntenfisch.Voxels.Noise;
using Tuntenfisch.Voxels.Noise.Nodes;
using Unity.Mathematics;
using UnityEngine;
using Tuntenfisch.Voxels.CSG;

namespace Tuntenfisch.Voxels.VoxelVolume
{
    [RequireComponent(typeof(VoxelConfig))]
    public class VoxelVolume : MonoBehaviour
    {
        private VoxelConfig m_voxelConfig;
        private ComputeBuffer m_noiseGraphNodesBuffer;
        private ComputeBuffer m_noiseGraphNoiseParametersBuffer;
        private ComputeBuffer m_noiseGraphCSGOperatorsBuffer;
        private ComputeBuffer m_noiseGraphCSGPrimitivesBuffer;

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

            if (m_noiseGraphNoiseParametersBuffer == null || m_noiseGraphNoiseParametersBuffer.count < m_voxelConfig.NoiseGraph.NoiseParameters.Count)
            {
                m_noiseGraphNoiseParametersBuffer?.Release();
                m_noiseGraphNoiseParametersBuffer = new ComputeBuffer(math.max(m_voxelConfig.NoiseGraph.NoiseParameters.Count, 1), GPUNoiseParameters.SizeInBytes);
            }

            if (m_noiseGraphCSGOperatorsBuffer == null || m_noiseGraphCSGOperatorsBuffer.count < m_voxelConfig.NoiseGraph.CSGOperators.Count)
            {
                m_noiseGraphCSGOperatorsBuffer?.Release();
                m_noiseGraphCSGOperatorsBuffer = new ComputeBuffer(math.max(m_voxelConfig.NoiseGraph.CSGOperators.Count, 1), GPUCSGOperator.SizeInBytes);
            }

            if (m_noiseGraphCSGPrimitivesBuffer == null || m_noiseGraphCSGPrimitivesBuffer.count < m_voxelConfig.NoiseGraph.CSGPrimitives.Count)
            {
                m_noiseGraphCSGPrimitivesBuffer?.Release();
                m_noiseGraphCSGPrimitivesBuffer = new ComputeBuffer(math.max(m_voxelConfig.NoiseGraph.CSGPrimitives.Count, 1), GPUCSGPrimitive.SizeInBytes);
            }
        }

        private void ReleaseBuffers()
        {
            if (m_noiseGraphNodesBuffer != null)
            {
                m_noiseGraphNodesBuffer.Release();
                m_noiseGraphNodesBuffer = null;
            }

            if (m_noiseGraphNoiseParametersBuffer != null)
            {
                m_noiseGraphNoiseParametersBuffer.Release();
                m_noiseGraphNoiseParametersBuffer = null;
            }

            if (m_noiseGraphCSGOperatorsBuffer != null)
            {
                m_noiseGraphCSGOperatorsBuffer.Release();
                m_noiseGraphCSGOperatorsBuffer = null;
            }

            if (m_noiseGraphCSGPrimitivesBuffer != null)
            {
                m_noiseGraphCSGPrimitivesBuffer.Release();
                m_noiseGraphCSGPrimitivesBuffer = null;
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
            m_noiseGraphNoiseParametersBuffer.SetData(m_voxelConfig.NoiseGraph.NoiseParameters);
            m_noiseGraphCSGOperatorsBuffer.SetData(m_voxelConfig.NoiseGraph.CSGOperators);
            m_noiseGraphCSGPrimitivesBuffer.SetData(m_voxelConfig.NoiseGraph.CSGPrimitives);

            m_voxelConfig.VoxelVolumeConfig.Compute.SetInt(ComputeShaderProperties.NumberOfNoiseGraphNoiseNodes, m_voxelConfig.NoiseGraph.Nodes.Count);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.NoiseGraphNodes, m_noiseGraphNodesBuffer);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.NoiseGraphNoiseParameters, m_noiseGraphNoiseParametersBuffer);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.NoiseGraphCSGOperators, m_noiseGraphCSGOperatorsBuffer);
            m_voxelConfig.VoxelVolumeConfig.Compute.SetBuffer(0, ComputeShaderProperties.NoiseGraphCSGPrimitives, m_noiseGraphCSGPrimitivesBuffer);
        }
    }
}
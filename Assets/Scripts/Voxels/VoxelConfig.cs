using Tuntenfisch.Voxels.DC;
using Tuntenfisch.Voxels.Materials;
using Tuntenfisch.Voxels.Procedural;
using Tuntenfisch.Voxels.Volume;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tuntenfisch.Voxels
{
    public class VoxelConfig : MonoBehaviour
    {
        public VoxelVolumeConfig VoxelVolumeConfig => m_voxelVolumeConfig;
        public MaterialConfig MaterialConfig => m_materialConfig;
        public DualContouringConfig DualContouringConfig => m_dualContouringConfig;
        public GenerationGraph GenerationGraph => m_generationGraph;

        [SerializeField]
        private VoxelVolumeConfig m_voxelVolumeConfig;
        [SerializeField]
        private MaterialConfig m_materialConfig;
        [SerializeField]
        private DualContouringConfig m_dualContouringConfig;
        [SerializeField]
        private GenerationGraph m_generationGraph;

        private void Awake()
        {
            Assert.IsNotNull(m_voxelVolumeConfig);
            Assert.IsNotNull(m_materialConfig);
            Assert.IsNotNull(m_dualContouringConfig);
            Assert.IsNotNull(m_generationGraph);

            VoxelVolumeConfig.OnDirtied += ApplyVoxelVolumeConfig;
            DualContouringConfig.OnDirtied += ApplyDualContouringConfig;

            ApplyVoxelVolumeConfig();
            ApplyDualContouringConfig();
        }

        private void OnDestroy()
        {
            VoxelVolumeConfig.OnDirtied -= ApplyVoxelVolumeConfig;
            DualContouringConfig.OnDirtied -= ApplyDualContouringConfig;
        }

        private void ApplyVoxelVolumeConfig()
        {
            VoxelVolumeConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, VoxelVolumeConfig.NumberOfVoxels.x, VoxelVolumeConfig.NumberOfVoxels.y, VoxelVolumeConfig.NumberOfVoxels.z);
            VoxelVolumeConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, VoxelVolumeConfig.VoxelSpacing);

            int3 numberOfVoxels = VoxelVolumeConfig.NumberOfVoxels;
            DualContouringConfig.Compute.SetInts(ComputeShaderProperties.NumberOfVoxels, numberOfVoxels.x, numberOfVoxels.y, numberOfVoxels.z);
            DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.VoxelSpacing, VoxelVolumeConfig.VoxelSpacing);
        }

        private void ApplyDualContouringConfig()
        {
            float cosOfHalfSharpFeatureAngle = math.cos(math.radians(0.5f * DualContouringConfig.SharpFeatureAngle));
            Shader.SetGlobalFloat(ShaderProperties.CosOfHalfSharpFeatureAngle, cosOfHalfSharpFeatureAngle);

            DualContouringConfig.Compute.SetInt(ComputeShaderProperties.SchmitzParticleIterations, DualContouringConfig.SchmitzParticleIterations);
            DualContouringConfig.Compute.SetFloat(ComputeShaderProperties.SchmitzParticleStepSize, DualContouringConfig.SchmitzParticleStepSize);
        }
    }
}
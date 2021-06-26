using Tuntenfisch.Voxels.DC;
using Tuntenfisch.Voxels.Materials;
using Tuntenfisch.Voxels.Procedural;
using Tuntenfisch.Voxels.Volume;
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
        }
    }
}
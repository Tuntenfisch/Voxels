using Tuntenfisch.Voxels.DC;
using Tuntenfisch.Voxels.Noise;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tuntenfisch.Voxels
{
    public class VoxelConfig : MonoBehaviour
    {
        public VoxelVolumeConfig VoxelVolumeConfig => m_voxelVolumeConfig;
        public DualContouringConfig DualContouringConfig => m_dualContouringConfig;
        public NoiseGraph NoiseGraph => m_noiseGraph;

        [SerializeField]
        private VoxelVolumeConfig m_voxelVolumeConfig;
        [SerializeField]
        private DualContouringConfig m_dualContouringConfig;
        [SerializeField]
        private NoiseGraph m_noiseGraph;

        private void Awake()
        {
            Assert.IsNotNull(m_voxelVolumeConfig);
            Assert.IsNotNull(m_dualContouringConfig);
            Assert.IsNotNull(m_noiseGraph);
        }
    }
}
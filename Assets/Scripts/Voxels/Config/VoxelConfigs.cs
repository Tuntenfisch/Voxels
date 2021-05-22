using UnityEngine;

namespace Tuntenfisch.Voxels.Config
{
    public class VoxelConfigs : MonoBehaviour
    {
        public VoxelVolumeConfig VoxelVolumeConfig => m_voxelVolumeConfig;
        public DualContouringConfig DualContouringConfig => m_dualContouringConfig;
        public NoiseConfig NoiseConfig => m_noiseConfig;

        [SerializeField]
        private VoxelVolumeConfig m_voxelVolumeConfig;
        [SerializeField]
        private DualContouringConfig m_dualContouringConfig;
        [SerializeField]
        private NoiseConfig m_noiseConfig;
    }
}
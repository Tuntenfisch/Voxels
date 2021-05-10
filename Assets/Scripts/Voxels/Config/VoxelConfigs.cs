using Generics;
using UnityEngine;

namespace Voxels.Config
{
    public class VoxelConfigs : SingletonComponent<VoxelConfigs>
    {
        public static VoxelVolumeConfig VoxelVolumeConfig => Instance.m_voxelVolumeConfig;
        public static DualContouringConfig DualContouringConfig => Instance.dualContouringConfig;
        public static NoiseConfig NoiseConfig => Instance.m_noiseConfig;

        [SerializeField]
        private VoxelVolumeConfig m_voxelVolumeConfig;
        [SerializeField]
        private DualContouringConfig dualContouringConfig;
        [SerializeField]
        private NoiseConfig m_noiseConfig;
    }
}
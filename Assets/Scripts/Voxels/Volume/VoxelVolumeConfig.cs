using System;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Volume
{
    [CreateAssetMenu(fileName = "Voxel Volume Config", menuName = "Voxels/Voxel Volume Config")]
    public class VoxelVolumeConfig : ScriptableObject
    {
        public event Action OnDirtied;
        public event Action OnLateDirtied;

        // Voxel volume properties.
        public ComputeShader Compute => m_compute;
        public int NumberOfVoxelsAlongAxis => m_numberOfVoxelsAlongAxis;
        public int NumberOfCellsAlongAxis => NumberOfVoxelsAlongAxis - 1;
        public int VoxelCount => NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;
        public int CellCount => NumberOfCellsAlongAxis * NumberOfCellsAlongAxis * NumberOfCellsAlongAxis;
        public int3 NumberOfVoxels => new int3(NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
        public int3 NumberOfCells => new int3(NumberOfCellsAlongAxis, NumberOfCellsAlongAxis, NumberOfCellsAlongAxis);
        public float3 VoxelVolumeDimensions => VoxelSpacing * (float3)NumberOfCells;
        public float VoxelSpacing => m_voxelSpacing;

        [SerializeField]
        private ComputeShader m_compute;
        [Range(67, 131)]
        [SerializeField]
        private int m_numberOfVoxelsAlongAxis = 67;
        [Min(0.25f)]
        [SerializeField]
        private float m_voxelSpacing = 1.0f;

        private void OnValidate()
        {
            m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis) + 3;

            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
        }
    }
}
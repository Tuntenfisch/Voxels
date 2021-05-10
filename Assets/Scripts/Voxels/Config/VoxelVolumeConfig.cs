using System;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels.Config
{
    [CreateAssetMenu(fileName = "Voxel Volume Configuration", menuName = "Voxels/Voxel Volume Configuration", order = 1)]
    public class VoxelVolumeConfig : ScriptableObject
    {
        public event Action OnDirty;

        // Voxel volume properties.
        public ComputeShader Compute => m_compute;
        public int NumberOfVoxelsAlongAxis => m_numberOfVoxelsAlongAxis;
        public int NumberOfCellsAlongAxis => NumberOfVoxelsAlongAxis - 1;
        public int NumberOfVoxels => NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;
        public int NumberOfCells => NumberOfCellsAlongAxis * NumberOfCellsAlongAxis * NumberOfCellsAlongAxis;
        public int3 VoxelVolumeCount => new int3(NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
        public int3 CellVolumeCount => new int3(NumberOfCellsAlongAxis, NumberOfCellsAlongAxis, NumberOfCellsAlongAxis);
        public int MaxNumberOfVertices => NumberOfCells;
        public int MaxNumberOfTriangles => 6 * NumberOfCells;

        [Header("General")]
        [SerializeField]
        private ComputeShader m_compute;
        [Range(16, 128)]
        [SerializeField]
        private int m_numberOfVoxelsAlongAxis = 64;
        [Range(0.25f, 2.0f)]
        [SerializeField]
        private float m_voxelSpacing = 0.5f;

        public float GetVoxelSpacing(int lod) => (1 << lod) * m_voxelSpacing;

        public float3 GetCellVolumeDimensions(int lod) => GetVoxelSpacing(lod) * (float3)CellVolumeCount;

        private void OnValidate()
        {
            m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis);
            OnDirty?.Invoke();
        }

    }
}
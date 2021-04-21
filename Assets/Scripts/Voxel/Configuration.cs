using System;
using UnityEngine;

namespace Voxel
{
    [CreateAssetMenu(fileName = "Voxel Volume Configuration", menuName = "Voxel/Volume Configuration", order = 1)]
    public class Configuration : ScriptableObject
    {
        public event Action OnDirty;

        public ComputeShader CubicalMarchingSquaresCompute => m_cubicalMarchingSquaresCompute;
        public int MaxIterations => m_maxIterations;
        public float StepSize => m_stepSize;
        public float SharpFeatureAngle => m_sharpFeatureAngle;

        public ComputeShader VoxelVolumeCompute => m_voxelVolumeCompute;
        public int NumberOfCellsAlongAxis => m_numberOfVoxelsAlongAxis - 1;
        public int NumberOfCells => NumberOfCellsAlongAxis * NumberOfCellsAlongAxis * NumberOfCellsAlongAxis;
        public int NumberOfVoxelsAlongAxis => m_numberOfVoxelsAlongAxis;
        public int NumberOfVoxels => NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;
        public Vector3Int CellVolumeDimensions => new Vector3Int(NumberOfCellsAlongAxis, NumberOfCellsAlongAxis, NumberOfCellsAlongAxis);
        public Vector3Int VoxelVolumeDimensions => new Vector3Int(NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
        public float VoxelSpacing => m_cellSpacing;
        public int MaxNumberOfFlatFeatureVertices => 3 * NumberOfVoxels - 3 * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;
        // 2 sharp feature vertices per segment * 2 segments per face * 6 faces per cell * number of cells +
        // 7 sharp feature vertices per component * 4 components per cell * number of cells
        public int MaxNumberOfSharpFeatureVertices => 2 * 2 * 6 * NumberOfCells + 7 * 4 * NumberOfCells;
        // 3 indices per triangle * 2 triangles per segment * 2 segments per face * 6 faces per cell * number of cells
        public int MaxNumberOfTriangles => 3 * 2 * 2 * 6 * NumberOfCells;
        public int Seed => m_seed;
        public Vector3 Offset => m_offset;
        public float Height => m_height;
        public float WaveLength => m_wavelength;
        public int NumberOfOctaves => m_numberOfOctaves;
        public float Persistence => m_persistence;
        public float Lacunarity => m_lacunarity;

        [Header("Cubical Marching Squares")]
        [SerializeField]
        private ComputeShader m_cubicalMarchingSquaresCompute;
        [Range(0, 50)]
        [SerializeField]
        private int m_maxIterations = 20;
        [Range(0.0f, 0.4f)]
        [SerializeField]
        private float m_stepSize = 0.2f;
        [Range(0.1f, 180.0f)]
        [SerializeField]
        private float m_sharpFeatureAngle = 50.0f;

        [Space(25.0f)]

        [Header("Voxel/Cell Volume")]
        [SerializeField]
        private ComputeShader m_voxelVolumeCompute;
        [Range(5, 65)]
        [SerializeField]
        private int m_numberOfVoxelsAlongAxis = 33;
        [Range(0.25f, 16.0f)]
        [SerializeField]
        private float m_cellSpacing = 1.0f;

        [Header("Noise Parameters")]
        [SerializeField]
        private int m_seed;
        [SerializeField]
        private Vector3 m_offset;

        [Header("Height Map")]
        [Range(0.0f, 100.0f)]
        [SerializeField]
        private float m_height = 15.0f;
        [Range(0.0f, 100.0f)]
        [SerializeField]
        private float m_wavelength = 35.0f;

        [Header("FBM")]
        [Range(1, 16)]
        [SerializeField]
        private int m_numberOfOctaves = 4;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_persistence = 0.5f;
        [Range(1.0f, 4.0f)]
        [SerializeField]
        private float m_lacunarity = 2.0f;

        private void OnEnable()
        {
            if (CubicalMarchingSquaresCompute == null || VoxelVolumeCompute == null)
            {
                return;
            }

            SetupComputeShaders();
        }

        private void OnValidate()
        {
            m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis) + 1;

            if (CubicalMarchingSquaresCompute == null || VoxelVolumeCompute == null)
            {
                return;
            }

            SetupComputeShaders();
            OnDirty?.Invoke();
        }

        private void SetupComputeShaders()
        {
            CubicalMarchingSquaresCompute.SetInts(ComputeShaderProperties.s_cellDimensions, CellVolumeDimensions.x, CellVolumeDimensions.y, CellVolumeDimensions.z);
            CubicalMarchingSquaresCompute.SetFloat(ComputeShaderProperties.s_cellSpacing, VoxelSpacing);
            CubicalMarchingSquaresCompute.SetInts(ComputeShaderProperties.s_voxelDimensions, VoxelVolumeDimensions.x, VoxelVolumeDimensions.y, VoxelVolumeDimensions.z);
            CubicalMarchingSquaresCompute.SetFloat(ComputeShaderProperties.s_cosOfSharpFeatureAngle, Mathf.Cos(SharpFeatureAngle * Mathf.Deg2Rad));
            CubicalMarchingSquaresCompute.SetInt(ComputeShaderProperties.s_maxIterations, MaxIterations);
            CubicalMarchingSquaresCompute.SetFloat(ComputeShaderProperties.s_stepSize, StepSize);

            VoxelVolumeCompute.SetInts(ComputeShaderProperties.s_cellDimensions, NumberOfCellsAlongAxis, NumberOfCellsAlongAxis, NumberOfCellsAlongAxis);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_cellSpacing, VoxelSpacing);
            VoxelVolumeCompute.SetInts(ComputeShaderProperties.s_voxelDimensions, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_wavelength, WaveLength);
            VoxelVolumeCompute.SetInt(ComputeShaderProperties.s_numberOfOctaves, NumberOfOctaves);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_persistence, Persistence);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_lacunarity, Lacunarity);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_height, Height);
        }
    }
}
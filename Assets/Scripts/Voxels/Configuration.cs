using System;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels
{
    [CreateAssetMenu(fileName = "Voxel Volume Configuration", menuName = "Voxel/Volume Configuration", order = 1)]
    public class Configuration : ScriptableObject
    {
        public event Action OnDirty;
        public ComputeShader DualContouringCompute => m_dualContouringCompute;
        public int SchmitzParticleIterations => m_schmitzParticleIterations;
        public float SchmitzParticleStepSize => m_schmitzParticleStepSize;
        public float SharpFeatureAngle => m_sharpFeatureAngle;
        public ComputeShader VoxelVolumeCompute => m_voxelVolumeCompute;
        public int NumberOfCellsAlongAxis => NumberOfVoxelsAlongAxis - 1;
        public int NumberOfCells => NumberOfCellsAlongAxis * NumberOfCellsAlongAxis * NumberOfCellsAlongAxis;
        public int NumberOfVoxelsAlongAxis => m_numberOfVoxelsAlongAxis;
        public int NumberOfVoxels => NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis * NumberOfVoxelsAlongAxis;
        public int3 CellVolumeCount => new int3(NumberOfCellsAlongAxis, NumberOfCellsAlongAxis, NumberOfCellsAlongAxis);
        public int3 VoxelVolumeCount => new int3(NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
        public float3 CellVolumeDimensions => VoxelSpacing * (float3)CellVolumeCount;
        public float3 VoxelVolumeDimensions => VoxelSpacing * (float3)VoxelVolumeCount;
        public float VoxelSpacing => m_voxelSpacing;
        public int MaxNumberOfVertices => NumberOfCells;
        public int MaxNumberOfTriangles => 6 * NumberOfCells;
        public int Seed => m_seed;
        public float Height => m_height;
        public float WaveLength => m_wavelength;
        public int NumberOfOctaves => m_numberOfOctaves;
        public float Persistence => m_persistence;
        public float Lacunarity => m_lacunarity;

        [Header("Dual Contouring")]
        [SerializeField]
        private ComputeShader m_dualContouringCompute;
        [Range(0, 50)]
        [SerializeField]
        private int m_schmitzParticleIterations = 20;
        [Range(0.0f, 0.4f)]
        [SerializeField]
        private float m_schmitzParticleStepSize = 0.2f;
        [Range(0.1f, 180.0f)]
        [SerializeField]
        private float m_sharpFeatureAngle = 50.0f;

        [Header("Voxel Volume")]
        [SerializeField]
        private ComputeShader m_voxelVolumeCompute;
        [Range(16, 128)]
        [SerializeField]
        private int m_numberOfVoxelsAlongAxis = 32;
        [Range(0.25f, 2.0f)]
        [SerializeField]
        private float m_voxelSpacing = 1.0f;

        [Header("Noise Parameters")]
        [SerializeField]
        private int m_seed;

        [Header("Height Map")]
        [Min(0.0f)]
        [SerializeField]
        private float m_height = 15.0f;
        [Min(0.0f)]
        [SerializeField]
        private float m_wavelength = 35.0f;

        [Header("FBM")]
        [Range(1, 32)]
        [SerializeField]
        private int m_numberOfOctaves = 4;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_persistence = 0.5f;
        [Range(1.0f, 4.0f)]
        [SerializeField]
        private float m_lacunarity = 2.0f;

        private void Awake()
        {
            if (DualContouringCompute == null || VoxelVolumeCompute == null)
            {
                return;
            }

            SetupComputeShaders();
        }

        private void OnValidate()
        {
            m_numberOfVoxelsAlongAxis = Mathf.ClosestPowerOfTwo(m_numberOfVoxelsAlongAxis);

            if (DualContouringCompute == null || VoxelVolumeCompute == null)
            {
                return;
            }

            SetupComputeShaders();
            OnDirty?.Invoke();
        }

        private void SetupComputeShaders()
        {
            DualContouringCompute.SetInts(ComputeShaderProperties.s_voxelVolumeCount, VoxelVolumeCount.x, VoxelVolumeCount.y, VoxelVolumeCount.z);
            DualContouringCompute.SetInt(ComputeShaderProperties.s_schmitzParticleIterations, SchmitzParticleIterations);
            DualContouringCompute.SetFloat(ComputeShaderProperties.s_schmitzParticleStepSize, SchmitzParticleStepSize);

            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_voxelSpacing, VoxelSpacing);
            VoxelVolumeCompute.SetInts(ComputeShaderProperties.s_voxelVolumeCount, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis, NumberOfVoxelsAlongAxis);
            VoxelVolumeCompute.SetInt(ComputeShaderProperties.s_seed, Seed);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_wavelength, WaveLength);
            VoxelVolumeCompute.SetInt(ComputeShaderProperties.s_numberOfOctaves, NumberOfOctaves);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_persistence, Persistence);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_lacunarity, Lacunarity);
            VoxelVolumeCompute.SetFloat(ComputeShaderProperties.s_height, Height);
        }
    }
}
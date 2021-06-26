using Tuntenfisch.Voxels.Materials;
using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUNoiseParameters
    {
        public static int SizeInBytes => s_sizeInBytes;

        private static readonly int s_sizeInBytes = Marshal.SizeOf<GPUNoiseParameters>();

        // General Parameters.
        [SerializeField]
        private MaterialIndex m_materialIndex;
        [SerializeField]
        private int m_seed;
        [SerializeField]
        private NoiseAxes m_noiseAxes;
        [SerializeField]
        private NoiseType m_noiseType;

        // FBM parameters.
        [SerializeField]
        private int m_numberOfOctaves;
        [SerializeField]
        private float m_initialAmplitude;
        [SerializeField]
        private float3 m_initialFrequency;
        [SerializeField]
        private float m_persistence;
        [SerializeField]
        private float3 m_lacunarity;
    }
}

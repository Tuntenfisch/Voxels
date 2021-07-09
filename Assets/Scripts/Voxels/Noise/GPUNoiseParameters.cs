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

        [Header("General")]
        [SerializeField]
        private int m_seed;
        [SerializeField]
        private NoiseAxes m_noiseAxes;
        [SerializeField]
        private NoiseType m_noiseType;

        [Header("Fractional Brownian Motion")]
        [Range(1, 32)]
        [SerializeField]
        private int m_numberOfOctaves;
        [Min(1.0f)]
        [SerializeField]
        private float m_initialAmplitude;
        [SerializeField]
        private float3 m_initialFrequency;
        [Range(0.0f, 2.0f)]
        [SerializeField]
        private float m_persistence;
        [SerializeField]
        private float3 m_lacunarity;
    }
}

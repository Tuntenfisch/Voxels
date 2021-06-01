using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct NoiseParameters
    {
        public static int SizeInBytes => s_sizeInBytes;

        private static readonly int s_sizeInBytes = Marshal.SizeOf<NoiseParameters>();

        // General Parameters.
        [SerializeField]
        private int m_seed;
        [SerializeField]
        private int m_noiseDimensionality;
        [SerializeField]
        private int m_noiseType;

        // FBM parameters.
        [SerializeField]
        private int m_numberOfOctaves;
        [SerializeField]
        private float m_initialAmplitude;
        [SerializeField]
        private float m_initialFrequency;
        [SerializeField]
        private float m_persistence;
        [SerializeField]
        private float m_lacunarity;

        // Combine parameters.
        [SerializeField]
        private int m_operatorIndex;
        [SerializeField]
        private float m_smoothing;
    }
}

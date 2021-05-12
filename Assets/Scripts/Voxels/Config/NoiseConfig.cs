using System;
using UnityEngine;

namespace Voxels.Config
{
    [CreateAssetMenu(fileName = "Noise Config", menuName = "Voxels/Noise Config", order = 3)]
    public class NoiseConfig : ScriptableObject
    {
        public event Action OnDirtied;

        // Noise properties.
        public int Seed => m_seed;

        // Height map properties.
        public float Height => m_height;
        public float WaveLength => m_wavelength;

        // FBM properties.
        public int NumberOfOctaves => m_numberOfOctaves;
        public float Persistence => m_persistence;
        public float Lacunarity => m_lacunarity;

        [Header("Noise Parameters")]
        [SerializeField]
        private int m_seed;

        [Header("Height Map")]
        [Min(0.0f)]
        [SerializeField]
        private float m_height = 300.0f;
        [Min(0.0f)]
        [SerializeField]
        private float m_wavelength = 1000.0f;

        [Header("FBM")]
        [Range(1, 32)]
        [SerializeField]
        private int m_numberOfOctaves = 16;
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_persistence = 0.5f;
        [Range(1.0f, 4.0f)]
        [SerializeField]
        private float m_lacunarity = 2.0f;

        private void OnValidate() => OnDirtied?.Invoke();
    }
}
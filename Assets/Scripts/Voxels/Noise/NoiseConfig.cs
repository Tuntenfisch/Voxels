using System;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise
{
    [CreateAssetMenu(fileName = "Noise Config", menuName = "Voxels/Noise Config", order = 3)]
    public class NoiseConfig : ScriptableObject
    {
        public event Action OnDirtied;

        public NoiseParameters[] NoiseLayers => m_noiseLayers;

        [SerializeField]
        private NoiseParameters[] m_noiseLayers;

        private void OnValidate() => OnDirtied?.Invoke();
    }
}
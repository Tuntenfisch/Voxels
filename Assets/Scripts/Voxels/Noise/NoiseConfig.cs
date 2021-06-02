using System;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise
{
    [CreateAssetMenu(fileName = "Noise Config", menuName = "Voxels/Noise Config")]
    public class NoiseConfig : ScriptableObject
    {
        public event Action OnDirtied;

        public NoiseParameters[] NoiseLayers => m_noiseLayers;

        [SerializeField]
        private NoiseParameters[] m_noiseLayers;

        public void MakeDirty() => OnDirtied?.Invoke();
    }
}
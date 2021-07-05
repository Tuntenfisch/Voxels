using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUMaterialInfo
    {
        public Color Color => m_color;
        public bool Blend => m_blend;

        [SerializeField]
        private Color m_color;
        [SerializeField]
        private bool m_blend;

        public GPUMaterialInfo(Color color, bool blend)
        {
            m_color = color;
            m_blend = blend;
        }
    }
}
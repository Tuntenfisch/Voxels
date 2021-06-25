using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo
    {
        public MaterialIndex MaterialIndex => m_materialIndex;
        public Color Color => m_color;

        [SerializeField]
        private MaterialIndex m_materialIndex;
        [SerializeField]
        private Color m_color;
    }
}
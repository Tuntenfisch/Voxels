using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUMaterialInfo
    {
        public MaterialIndex MaterialIndex => m_materialIndex;
        public Color Color => m_color;

        [SerializeField]
        private MaterialIndex m_materialIndex;
        [SerializeField]
        private Color m_color;

        public GPUMaterialInfo(MaterialIndex materialIndex, Color color)
        {
            m_materialIndex = materialIndex;
            m_color = color;
        }
    }
}
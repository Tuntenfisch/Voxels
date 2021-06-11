using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUCSGPrimitive
    {
        public static int SizeInBytes => s_sizeInBytes;

        public CSGPrimitiveType PrimitiveType => m_primitiveType;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUCSGPrimitive>();

        [SerializeField]
        private CSGPrimitiveType m_primitiveType;

        public GPUCSGPrimitive(CSGPrimitiveType primitiveType)
        {
            m_primitiveType = primitiveType;
        }
    }
}
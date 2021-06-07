using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUCSGPrimitive
    {
        public static int SizeInBytes => s_sizeInBytes;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUCSGPrimitive>();

        [SerializeField]
        private CSGPrimitiveType m_primitiveType;
        [SerializeField]
        private float3 m_payload0;
        [SerializeField]
        private float3 m_payload1;

        public static GPUCSGPrimitive CreateSpherePrimitive(float3 center, float radius)
        {
            GPUCSGPrimitive primitive = new GPUCSGPrimitive
            {
                m_primitiveType = CSGPrimitiveType.Sphere,
                m_payload0 = center,
                m_payload1 = new float3(radius, 0.0f, 0.0f)
            };

            return primitive;
        }

        public static GPUCSGPrimitive CreateCuboidPrimitive(float3 center, float3 size)
        {
            GPUCSGPrimitive primitive = new GPUCSGPrimitive
            {
                m_primitiveType = CSGPrimitiveType.Cuboid,
                m_payload0 = center,
                m_payload1 = size
            };

            return primitive;
        }
    }
}
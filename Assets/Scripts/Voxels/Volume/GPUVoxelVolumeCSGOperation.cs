using System;
using System.Runtime.InteropServices;
using Tuntenfisch.Voxels.CSG;
using UnityEngine;

namespace Tuntenfisch.Voxels.Volume
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUVoxelVolumeCSGOperation
    {
        public static int SizeInBytes => s_sizeInBytes;

        public GPUCSGOperator CSGOperator => m_csgOperator;
        public GPUCSGPrimitive CSGPrimitive => m_csgPrimitive;
        public Matrix4x4 TransformMatrix => m_transformMatrix;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUVoxelVolumeCSGOperation>();

        private GPUCSGOperator m_csgOperator;
        private GPUCSGPrimitive m_csgPrimitive;
        private Matrix4x4 m_transformMatrix;

        public GPUVoxelVolumeCSGOperation(GPUCSGOperator csgOperator, GPUCSGPrimitive csgPrimitive, Matrix4x4 transformMatrix)
        {
            m_csgOperator = csgOperator;
            m_csgPrimitive = csgPrimitive;
            m_transformMatrix = transformMatrix;
        }
    }
}
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

        public Matrix4x4 TransformMatrix => m_transformMatrix;
        public GPUCSGOperator CSGOperator => m_csgOperator;
        public GPUCSGPrimitive CSGPrimitive => m_csgPrimitive;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUVoxelVolumeCSGOperation>();

        private Matrix4x4 m_transformMatrix;
        private GPUCSGOperator m_csgOperator;
        private GPUCSGPrimitive m_csgPrimitive;

        public GPUVoxelVolumeCSGOperation(Matrix4x4 transformMatrix, GPUCSGOperator csgOperator, GPUCSGPrimitive csgPrimitive)
        {
            m_transformMatrix = transformMatrix;
            m_csgOperator = csgOperator;
            m_csgPrimitive = csgPrimitive;
        }
    }
}
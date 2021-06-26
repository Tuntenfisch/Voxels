using System;
using System.Runtime.InteropServices;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.Materials;
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
        public MaterialIndex MaterialIndex => m_materialIndex;
        public Matrix4x4 TransformMatrix => m_transformMatrix;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUVoxelVolumeCSGOperation>();

        [SerializeField]
        private GPUCSGOperator m_csgOperator;
        [SerializeField]
        private GPUCSGPrimitive m_csgPrimitive;
        [SerializeField]
        private readonly MaterialIndex m_materialIndex;
        [SerializeField]
        private Matrix4x4 m_transformMatrix;


        public GPUVoxelVolumeCSGOperation(GPUCSGOperator csgOperator, GPUCSGPrimitive csgPrimitive, MaterialIndex materialIndex, Matrix4x4 transformMatrix)
        {
            m_csgOperator = csgOperator;
            m_csgPrimitive = csgPrimitive;
            m_materialIndex = materialIndex;
            m_transformMatrix = transformMatrix;
        }
    }
}
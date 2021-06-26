using System;
using System.Runtime.InteropServices;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.Materials;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUGenerationGraphNode
    {
        public static int SizeInBytes => s_sizeInBytes;

        public NodeType NodeType => m_nodeType;
        public Matrix4x4 TransformMatrix => m_transformMatrix;
        public GPUNoiseParameters NoiseParameters => m_noiseParameters;
        public GPUCSGPrimitive CSGPrimitive => m_csgPrimitive;
        public GPUCSGOperator CSGOperator => m_csgOperator;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUGenerationGraphNode>();

        [SerializeField]
        private NodeType m_nodeType;
        [SerializeField]
        private Matrix4x4 m_transformMatrix;
        [SerializeField]
        private GPUNoiseParameters m_noiseParameters;
        [SerializeField]
        private GPUCSGPrimitive m_csgPrimitive;
        [SerializeField]
        private MaterialIndex m_materialIndex;
        [SerializeField]
        private GPUCSGOperator m_csgOperator;

        public GPUGenerationGraphNode(NodeType nodeType, Matrix4x4 transformMatrix, GPUNoiseParameters noiseParameters, GPUCSGPrimitive csgPrimitive, MaterialIndex materialIndex, GPUCSGOperator csgOperator)
        {
            m_nodeType = nodeType;
            m_transformMatrix = transformMatrix;
            m_noiseParameters = noiseParameters;
            m_csgPrimitive = csgPrimitive;
            m_materialIndex = materialIndex;
            m_csgOperator = csgOperator;
        }
    }
}
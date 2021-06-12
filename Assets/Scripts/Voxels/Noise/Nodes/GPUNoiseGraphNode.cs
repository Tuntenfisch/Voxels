using System;
using System.Runtime.InteropServices;
using Tuntenfisch.Voxels.CSG;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUNoiseGraphNode
    {
        public static int SizeInBytes => s_sizeInBytes;

        public NodeType NodeType => m_nodeType;
        public Matrix4x4 TransformMatrix => m_transformMatrix;
        public GPUNoiseParameters NoiseParameters => m_noiseParameters;
        public GPUCSGOperator CSGOperator => m_csgOperator;
        public GPUCSGPrimitive CSGPrimitive => m_csgPrimitive;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUNoiseGraphNode>();

        [SerializeField]
        private NodeType m_nodeType;
        [SerializeField]
        private Matrix4x4 m_transformMatrix;
        [SerializeField]
        private GPUNoiseParameters m_noiseParameters;
        [SerializeField]
        private GPUCSGOperator m_csgOperator;
        [SerializeField]
        private GPUCSGPrimitive m_csgPrimitive;

        public GPUNoiseGraphNode(NodeType nodeType, Matrix4x4 transformMatrix, GPUNoiseParameters noiseParameters, GPUCSGOperator csgOperator, GPUCSGPrimitive csgPrimitive)
        {
            m_nodeType = nodeType;
            m_transformMatrix = transformMatrix;
            m_noiseParameters = noiseParameters;
            m_csgOperator = csgOperator;
            m_csgPrimitive = csgPrimitive;
        }
    }
}
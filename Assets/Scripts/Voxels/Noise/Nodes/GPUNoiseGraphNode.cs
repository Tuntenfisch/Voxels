using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUNoiseGraphNode
    {
        public static int SizeInBytes => s_sizeInBytes;

        public NodeType NodeType => m_nodeType;
        public int DataIndex => m_dataIndex;

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUNoiseGraphNode>();

        [SerializeField]
        private NodeType m_nodeType;
        [SerializeField]
        private int m_dataIndex;

        public GPUNoiseGraphNode(NodeType nodeType, int dataIndex)
        {
            m_nodeType = nodeType;
            m_dataIndex = dataIndex;
        }
    }
}
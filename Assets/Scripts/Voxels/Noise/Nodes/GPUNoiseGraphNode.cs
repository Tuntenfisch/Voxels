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

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUNoiseGraphNode>();

        [SerializeField]
        private readonly NodeType m_nodeType;
        private readonly int m_dataIndex;

        public GPUNoiseGraphNode(NodeType nodeType, int dataIndex)
        {
            m_nodeType = nodeType;
            m_dataIndex = dataIndex;
        }
    }
}
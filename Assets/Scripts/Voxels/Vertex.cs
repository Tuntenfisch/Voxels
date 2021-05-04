using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Voxels
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public static VertexAttributeDescriptor[] Attributes => s_attributes;
        public static int SizeInBytes => s_sizeInBytes;

        private static readonly VertexAttributeDescriptor[] s_attributes =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
        };
        private static readonly int s_sizeInBytes = Marshal.SizeOf<Vertex>();

        private float3 m_position;
        private float3 m_normal;
    }
}

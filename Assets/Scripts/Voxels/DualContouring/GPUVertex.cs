using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Tuntenfisch.Voxels.DualContouring
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUVertex
    {
        public static int SizeInBytes => s_sizeInBytes;
        public static VertexAttributeDescriptor[] Attributes => s_attributes;

        private static readonly int s_sizeInBytes = Marshal.SizeOf<GPUVertex>();
        private static readonly VertexAttributeDescriptor[] s_attributes =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
        };

        private float3 m_position;
        private float3 m_normal;
    }
}
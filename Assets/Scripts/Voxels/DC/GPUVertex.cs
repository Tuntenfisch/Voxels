using System.Runtime.InteropServices;
using Tuntenfisch.Voxels.Materials;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Tuntenfisch.Voxels.DC
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
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float16, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 1)
        };

        public float3 Position => m_position;
        public half4 Normal => m_normal;
        public MaterialIndex MaterialIndex => m_materialIndex;

        private float3 m_position;
        private half4 m_normal;
        private readonly MaterialIndex m_materialIndex;
    }
}
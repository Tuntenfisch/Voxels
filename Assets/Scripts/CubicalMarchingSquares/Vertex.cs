using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubicalMarchingSquares
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public static readonly VertexAttributeDescriptor[] s_attributes =
        {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
    };

        public static readonly int s_sizeInBytes = 6 * sizeof(float);

        public Vector3 m_position;
        public Vector3 m_normal;
    }
}

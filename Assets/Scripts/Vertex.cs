using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 m_position;
    public Vector3 m_normal;
}

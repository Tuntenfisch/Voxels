using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG
{
    public class CSGUtility : MonoBehaviour
    {
        [SerializeField]
        private Material m_hologramMaterial;

        private Dictionary<CSGPrimitiveType, Mesh> m_csgPrimitiveMeshes;

        private void Awake()
        {
            m_csgPrimitiveMeshes = new Dictionary<CSGPrimitiveType, Mesh>
            {
                [CSGPrimitiveType.Sphere] = Resources.GetBuiltinResource<Mesh>("Sphere.fbx"),
                [CSGPrimitiveType.Cuboid] = Resources.GetBuiltinResource<Mesh>("Cube.fbx")
            };
        }

        public void DrawCSGPrimitiveHologram(GPUCSGPrimitive csgPrimitive)
        {
            Mesh mesh = m_csgPrimitiveMeshes[csgPrimitive.PrimitiveType];
            Matrix4x4 matrix = csgPrimitive.PrimitiveType switch
            {
                CSGPrimitiveType.Sphere => Matrix4x4.TRS(csgPrimitive.Center, Quaternion.identity, csgPrimitive.Radius * new float3(2.0f, 2.0f, 2.0f)),
                _ => Matrix4x4.TRS(csgPrimitive.Center, Quaternion.identity, csgPrimitive.Size),
            };
            Graphics.DrawMesh(mesh, matrix, m_hologramMaterial, 0, null, 0, null, false);
        }
    }
}
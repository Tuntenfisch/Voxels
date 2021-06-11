using System.Collections.Generic;
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

        public void DrawCSGPrimitiveHologram(CSGPrimitiveType primitiveType, Matrix4x4 objectToWorldMatrix)
        {
            Mesh mesh = m_csgPrimitiveMeshes[primitiveType];
            Graphics.DrawMesh(mesh, objectToWorldMatrix, m_hologramMaterial, 0, null, 0, null, false);
        }
    }
}
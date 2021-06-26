using Tuntenfisch.Voxels.Materials;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/Material", order = (int)NodeType.Material)]
    [NodeTint(c_internalNodeColor)]
    public class MaterialNode : GenerationGraphNode
    {
        public MaterialIndex MaterialIndex => m_materialIndex;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float2 m_valueAndGradient;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_voxel;

        [SerializeField]
        private MaterialIndex m_materialIndex;

        public override NodeType GetNodeType() => NodeType.Material;
    }
}
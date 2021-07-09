using Tuntenfisch.Attributes;
using Tuntenfisch.Voxels.CSG;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/CSG Primitive", order = (int)NodeType.CSGPrimitive)]
    [NodeTint(c_internalNodeColor)]
    public class CSGPrimitiveNode : GenerationGraphNode
    {
        public GPUCSGPrimitive CSGPrimitive => m_csgPrimitive;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float2 m_voxel;

        [InlineField]
        [SerializeField]
        private GPUCSGPrimitive m_csgPrimitive;

        public override NodeType GetNodeType() => NodeType.CSGPrimitive;
    }
}
using Tuntenfisch.Voxels.CSG;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/CSG Primitive", order = (int)NodeType.CSGPrimitive)]
    [NodeTint(c_internalNodeColor)]
    public class CSGPrimitiveNode : NoiseGraphNode
    {
        public GPUCSGPrimitive CSGPrimitive => m_csgPrimitive;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float4 m_voxel;

        [SerializeField]
        private GPUCSGPrimitive m_csgPrimitive;

        public override NodeType GetNodeType() => NodeType.CSGPrimitive;
    }
}
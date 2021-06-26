using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/Output", order = (int)NodeType.Output)]
    [DisallowMultipleNodes]
    [NodeTint(c_rootNodeColor)]
    public class OutputNode : GenerationGraphNode
    {
        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_voxel;

        public override NodeType GetNodeType() => NodeType.Output;
    }
}
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/Position")]
    [NodeTint(c_leafNodeColor)]
    public class PositionNode : NoiseGraphNode
    {
        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_position;

        public override NodeType GetNodeType() => NodeType.Position;
    }
}
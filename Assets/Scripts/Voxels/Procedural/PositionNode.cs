using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/Position", order = (int)NodeType.Position)]
    [NodeTint(c_leafNodeColor)]
    public class PositionNode : GenerationGraphNode
    {
        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float m_position;

        public override NodeType GetNodeType() => NodeType.Position;
    }
}
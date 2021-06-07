using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/Output")]
    [DisallowMultipleNodes]
    [NodeTint(c_rootNodeColor)]
    public class OutputNode : NoiseGraphNode
    {
        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float4 m_valueAndGradient;

        public override NodeType GetNodeType() => NodeType.Output;
    }
}
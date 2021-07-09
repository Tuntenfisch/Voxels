using Tuntenfisch.Attributes;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/Domain Warp", order = (int)NodeType.DomainWarp)]
    [NodeWidth(272)]
    [NodeTint(c_internalNodeColor)]
    public class DomainWarpNode : GenerationGraphNode
    {
        public GPUNoiseParameters NoiseParameters => m_noiseParameters;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float m_warpedPosition;

        [InlineField]
        [SerializeField]
        private GPUNoiseParameters m_noiseParameters;

        public override NodeType GetNodeType() => NodeType.DomainWarp;
    }
}
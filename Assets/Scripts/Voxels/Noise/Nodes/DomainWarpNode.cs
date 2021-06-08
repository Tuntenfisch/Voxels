using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/Domain Warp")]
    [NodeWidth(272)]
    [NodeTint(c_internalNodeColor)]
    public class DomainWarpNode : NoiseGraphNode
    {
        public GPUNoiseParameters NoiseParameters => m_noiseParameters;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_warpedPosition;

        [SerializeField]
        private GPUNoiseParameters m_noiseParameters;

        public override NodeType GetNodeType() => NodeType.DomainWarp;
    }
}
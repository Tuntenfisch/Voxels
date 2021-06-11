using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/Noise", order = (int)NodeType.Noise)]
    [NodeWidth(272)]
    [NodeTint(c_internalNodeColor)]
    public class NoiseNode : NoiseGraphNode
    {
        public GPUNoiseParameters NoiseParameters => m_noiseParameters;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float4 m_valueAndGradient;

        [SerializeField]
        private GPUNoiseParameters m_noiseParameters;

        public override NodeType GetNodeType() => NodeType.Noise;
    }
}
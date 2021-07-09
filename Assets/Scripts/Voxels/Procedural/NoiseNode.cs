using Tuntenfisch.Attributes;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/Noise", order = (int)NodeType.Noise)]
    [NodeWidth(272)]
    [NodeTint(c_internalNodeColor)]
    public class NoiseNode : GenerationGraphNode
    {
        public GPUNoiseParameters NoiseParameters => m_noiseParameters;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float2 m_valueAndGradient;

        [InlineField]
        [SerializeField]
        private GPUNoiseParameters m_noiseParameters;

        public override NodeType GetNodeType() => NodeType.Noise;
    }
}
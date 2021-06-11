using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/Transform", order = (int)NodeType.Transform)]
    [NodeTint(c_internalNodeColor)]
    public class TransformNode : NoiseGraphNode
    {
        public Matrix4x4 Matrix
        {
            get
            {
                Matrix4x4 matrix = Matrix4x4.TRS(m_translation, Quaternion.Euler(m_rotation.x, m_rotation.y, m_rotation.z), m_scale);

                return m_invert ? matrix.inverse : matrix;
            }
        }

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_position;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_transformedPosition;

        [SerializeField]
        private float3 m_translation;
        [SerializeField]
        private float3 m_rotation;
        [SerializeField]
        private float3 m_scale;
        [SerializeField]
        private bool m_invert;

        public override NodeType GetNodeType() => NodeType.Transform;
    }
}
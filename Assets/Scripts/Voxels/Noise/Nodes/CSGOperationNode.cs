using Tuntenfisch.Voxels.CSG;
using Unity.Mathematics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes
{
    [CreateNodeMenu("Noise Graph Nodes/CSG Operation", order = (int)NodeType.CSGOperation)]
    [NodeTint(c_internalNodeColor)]
    public class CSGOperationNode : NoiseGraphNode
    {
        public GPUCSGOperator CSGOperator => m_csgOperator;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float4 m_voxelA;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float4 m_voxelB;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float4 m_voxel;

        [SerializeField]
        private GPUCSGOperator m_csgOperator;

        public override NodeType GetNodeType() => NodeType.CSGOperation;
    }
}
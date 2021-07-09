using Tuntenfisch.Voxels.CSG;
using Unity.Mathematics;
using UnityEngine;
using Tuntenfisch.Attributes;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateNodeMenu("Generation Nodes/CSG Operation", order = (int)NodeType.CSGOperation)]
    [NodeTint(c_internalNodeColor)]
    public class CSGOperationNode : GenerationGraphNode
    {
        public GPUCSGOperator CSGOperator => m_csgOperator;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_voxelA;

        [Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_voxelB;

        [Output(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        [SerializeField]
        private float3 m_voxel;

        [InlineField]
        [SerializeField]
        private GPUCSGOperator m_csgOperator;

        public override NodeType GetNodeType() => NodeType.CSGOperation;
    }
}
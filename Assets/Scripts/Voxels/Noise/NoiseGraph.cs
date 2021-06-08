using System;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.Noise.Nodes;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;

namespace Tuntenfisch.Voxels.Noise
{
    [CreateAssetMenu(fileName = "Noise Config", menuName = "Voxels/Noise Config")]
    public class NoiseGraph : NodeGraph
    {
        public event Action OnDirtied;

        public List<GPUNoiseGraphNode> Nodes => m_nodes;
        public List<GPUNoiseParameters> NoiseParameters => m_noiseParameters;
        public List<GPUCSGOperator> CSGOperators => m_csgOperators;
        public List<GPUCSGPrimitive> CSGPrimitives => m_csgPrimitives;

        [SerializeField]
        private List<GPUNoiseGraphNode> m_nodes;
        [SerializeField]
        private List<GPUNoiseParameters> m_noiseParameters;
        [SerializeField]
        private List<GPUCSGOperator> m_csgOperators;
        [SerializeField]
        private List<GPUCSGPrimitive> m_csgPrimitives;

        public void Rebuild()
        {
            int outputNodeIndex = GetOutputNodeIndex();

            Assert.IsTrue(outputNodeIndex != -1, "Invalid noise graph: Output node is missing.");

            m_nodes ??= new List<GPUNoiseGraphNode>();
            m_nodes.Clear();
            m_noiseParameters ??= new List<GPUNoiseParameters>();
            m_noiseParameters.Clear();
            m_csgOperators ??= new List<GPUCSGOperator>();
            m_csgOperators.Clear();
            m_csgPrimitives ??= new List<GPUCSGPrimitive>();
            m_csgPrimitives.Clear();

            foreach (NoiseGraphNode node in IterateOverGraphInPreorder((NoiseGraphNode)nodes[outputNodeIndex]))
            {
                int dataIndex = -1;

                switch (node.GetNodeType())
                {
                    case NodeType.DomainWarp:
                        DomainWarpNode domainWarpNode = (DomainWarpNode)node;
                        m_noiseParameters.Add(domainWarpNode.NoiseParameters);
                        dataIndex = m_noiseParameters.Count - 1;
                        break;

                    case NodeType.Noise:
                        NoiseNode noiseNode = (NoiseNode)node;
                        m_noiseParameters.Add(noiseNode.NoiseParameters);
                        dataIndex = m_noiseParameters.Count - 1;
                        break;

                    case NodeType.CSGOperation:
                        CSGOperationNode csgOperationNode = (CSGOperationNode)node;
                        m_csgOperators.Add(csgOperationNode.CSGOperator);
                        dataIndex = m_csgOperators.Count - 1;
                        break;

                    case NodeType.CSGPrimitive:
                        CSGPrimitiveNode csgPrimitiveNode = (CSGPrimitiveNode)node;
                        m_csgPrimitives.Add(csgPrimitiveNode.CSGPrimitive);
                        dataIndex = m_csgPrimitives.Count - 1;
                        break;
                }
                m_nodes.Add(new GPUNoiseGraphNode(node.GetNodeType(), dataIndex));
            }
            OnDirtied?.Invoke();
        }

        private IEnumerable<NoiseGraphNode> IterateOverGraphInPreorder(NoiseGraphNode node)
        {
            if (node.GetNodeType() == NodeType.Position)
            {
                return Enumerable.Repeat(node, 1);
            }
            else
            {
                NodePort[] inputs = node.Inputs.ToArray();

                IEnumerable<NoiseGraphNode> leftNodes = Enumerable.Empty<NoiseGraphNode>();
                IEnumerable<NoiseGraphNode> rightNodes = Enumerable.Empty<NoiseGraphNode>();

                if (inputs.Length > 0)
                {
                    leftNodes = IterateOverGraphInPreorder((NoiseGraphNode)inputs[0].Connection.node);
                }

                if (inputs.Length > 1)
                {
                    rightNodes = IterateOverGraphInPreorder((NoiseGraphNode)inputs[1].Connection.node);
                }

                return Enumerable.Concat(Enumerable.Concat(leftNodes, rightNodes), Enumerable.Repeat(node, 1));
            }
        }

        private int GetOutputNodeIndex() => nodes.FindIndex((node) => ((NoiseGraphNode)node).GetNodeType() == NodeType.Output);
    }
}
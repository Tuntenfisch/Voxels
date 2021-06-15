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
        public event Action OnLateDirtied;

        public List<GPUNoiseGraphNode> Nodes => m_nodes;

        [SerializeField]
        private List<GPUNoiseGraphNode> m_nodes;

        public void Rebuild()
        {
            int outputNodeIndex = GetOutputNodeIndex();

            Assert.IsTrue(outputNodeIndex != -1, "Invalid noise graph: Output node is missing.");

            m_nodes ??= new List<GPUNoiseGraphNode>();
            m_nodes.Clear();

            foreach (NoiseGraphNode node in IterateOverGraphInPreorder((NoiseGraphNode)nodes[outputNodeIndex]))
            {
                Matrix4x4 transformMatrix = Matrix4x4.identity;
                GPUNoiseParameters noiseParameters = new GPUNoiseParameters();
                GPUCSGOperator csgOperator = new GPUCSGOperator();
                GPUCSGPrimitive csgPrimitive = new GPUCSGPrimitive();

                switch (node.GetNodeType())
                {
                    case NodeType.Transform:
                        TransformNode transformNode = (TransformNode)node;
                        transformMatrix = transformNode.TransformMatrix;
                        break;

                    case NodeType.DomainWarp:
                        DomainWarpNode domainWarpNode = (DomainWarpNode)node;
                        noiseParameters = domainWarpNode.NoiseParameters;
                        break;

                    case NodeType.Noise:
                        NoiseNode noiseNode = (NoiseNode)node;
                        noiseParameters = noiseNode.NoiseParameters;
                        break;

                    case NodeType.CSGOperation:
                        CSGOperationNode csgOperationNode = (CSGOperationNode)node;
                        csgOperator = csgOperationNode.CSGOperator;
                        break;

                    case NodeType.CSGPrimitive:
                        CSGPrimitiveNode csgPrimitiveNode = (CSGPrimitiveNode)node;
                        csgPrimitive = csgPrimitiveNode.CSGPrimitive;
                        break;
                }
                m_nodes.Add(new GPUNoiseGraphNode(node.GetNodeType(), transformMatrix, noiseParameters, csgOperator, csgPrimitive));
            }
            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
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
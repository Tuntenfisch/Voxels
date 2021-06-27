using System;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.Materials;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;

namespace Tuntenfisch.Voxels.Procedural
{
    [CreateAssetMenu(fileName = "Generation Graph", menuName = "Voxels/Generation Graph")]
    public class GenerationGraph : NodeGraph
    {
        public event Action OnDirtied;
        public event Action OnLateDirtied;

        public List<GPUGenerationGraphNode> Nodes => m_nodes;

        [SerializeField]
        private List<GPUGenerationGraphNode> m_nodes;

        public void Rebuild()
        {
            int outputNodeIndex = GetOutputNodeIndex();

            Assert.IsTrue(outputNodeIndex != -1, "Invalid noise graph: Output node is missing.");

            m_nodes ??= new List<GPUGenerationGraphNode>();
            m_nodes.Clear();

            foreach (GenerationGraphNode node in IterateOverGraphInPreorder((GenerationGraphNode)nodes[outputNodeIndex]))
            {
                Matrix4x4 transformMatrix = Matrix4x4.identity;
                GPUNoiseParameters noiseParameters = new GPUNoiseParameters();
                GPUCSGPrimitive csgPrimitive = new GPUCSGPrimitive();
                MaterialIndex materialIndex = default;
                GPUCSGOperator csgOperator = new GPUCSGOperator();

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

                    case NodeType.CSGPrimitive:
                        CSGPrimitiveNode csgPrimitiveNode = (CSGPrimitiveNode)node;
                        csgPrimitive = csgPrimitiveNode.CSGPrimitive;
                        break;

                    case NodeType.Material:
                        MaterialNode materialNode = (MaterialNode)node;
                        materialIndex = materialNode.MaterialIndex;
                        break;

                    case NodeType.CSGOperation:
                        CSGOperationNode csgOperationNode = (CSGOperationNode)node;
                        csgOperator = csgOperationNode.CSGOperator;
                        break;
                }
                m_nodes.Add(new GPUGenerationGraphNode(node.GetNodeType(), transformMatrix, noiseParameters, csgPrimitive, materialIndex, csgOperator));
            }
            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
        }

        private IEnumerable<GenerationGraphNode> IterateOverGraphInPreorder(GenerationGraphNode node)
        {
            if (node.GetNodeType() == NodeType.Position)
            {
                return Enumerable.Repeat(node, 1);
            }
            else
            {
                NodePort[] inputs = node.Inputs.ToArray();

                IEnumerable<GenerationGraphNode> leftNodes = Enumerable.Empty<GenerationGraphNode>();
                IEnumerable<GenerationGraphNode> rightNodes = Enumerable.Empty<GenerationGraphNode>();

                if (inputs.Length > 0)
                {
                    leftNodes = IterateOverGraphInPreorder((GenerationGraphNode)inputs[0].Connection.node);
                }

                if (inputs.Length > 1)
                {
                    rightNodes = IterateOverGraphInPreorder((GenerationGraphNode)inputs[1].Connection.node);
                }

                return Enumerable.Concat(Enumerable.Concat(leftNodes, rightNodes), Enumerable.Repeat(node, 1));
            }
        }

        private int GetOutputNodeIndex() => nodes.FindIndex((node) => ((GenerationGraphNode)node).GetNodeType() == NodeType.Output);
    }
}
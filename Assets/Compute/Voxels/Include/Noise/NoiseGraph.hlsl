#ifndef TUNTENFISCH_NOISE_GRAPH
#define TUNTENFISCH_NOISE_GRAPH

#include "Assets/Compute/Voxels/Include/Noise/Noise.hlsl"

static const uint noiseGraphNodeTypePosition = 0;
static const uint noiseGraphNodeTypeNoise = 1;
static const uint noiseGraphNodeTypeCSGOperation = 2;
static const uint noiseGraphNodeTypeCSGPrimitive = 3;
static const uint noiseGraphNodeTypeOutput = 4;

struct NoiseGraphNode
{
    uint nodeType;
    uint dataIndex;
};

StructuredBuffer<NoiseGraphNode> noiseGraphNodes;
StructuredBuffer<NoiseParameters> noiseGraphNoiseParameters;
StructuredBuffer<CSGOperator> noiseGraphCSGOperators;
StructuredBuffer<CSGPrimitive> noiseGraphCSGPrimitives;

uint numberOfNoiseGraphNoiseNodes;

struct NoiseGraphStack
{
    float4x4 buffer;
    uint count;

    void Push(float4 value_gradient)
    {
        switch(count++)
        {
            case 0:
                buffer[0] = value_gradient;
                break;

            case 1:
                buffer[1] = value_gradient;
                break;

            case 2:
                buffer[2] = value_gradient;
                break;

            default:
                buffer[3] = value_gradient;
                break;
        }
    }

    float4 Pop()
    {
        return buffer[--count];
    }

    static NoiseGraphStack Create()
    {
        NoiseGraphStack stack;
        stack.count = 0;

        return stack;
    }
};
float4 GenerateGraphFBMNoise(float3 position)
{
    float4 finalValue_finalGradient = 0.0f;
    
    NoiseGraphStack stack = NoiseGraphStack::Create();
    
    for (uint nodeIndex = 0; nodeIndex < numberOfNoiseGraphNoiseNodes; nodeIndex++)
    {
        NoiseGraphNode node = noiseGraphNodes[nodeIndex];
        
        switch(node.nodeType)
        {
            case noiseGraphNodeTypePosition:
                stack.Push(float4(position, 0.0f));
                break;
            
            case noiseGraphNodeTypeNoise:
                NoiseParameters noiseParameters = noiseGraphNoiseParameters[node.dataIndex];
                stack.Push(GenerateFBMNoise(stack.Pop().xyz, noiseParameters));
                break;
            
            case noiseGraphNodeTypeCSGOperation:
                CSGOperator csgOperator = noiseGraphCSGOperators[node.dataIndex];
                stack.Push(ApplyCSGOperator(stack.Pop(), stack.Pop(), csgOperator));
                break;
            
            case noiseGraphNodeTypeCSGPrimitive:
                CSGPrimitive csgPrimitive = noiseGraphCSGPrimitives[node.dataIndex];
                stack.Push(EvaluateCSGPrimitive(csgPrimitive, stack.Pop().xyz));
                break;
            
            case noiseGraphNodeTypeOutput:
                finalValue_finalGradient = stack.Pop();
                break;
        }
    }

    return finalValue_finalGradient;
}

#endif
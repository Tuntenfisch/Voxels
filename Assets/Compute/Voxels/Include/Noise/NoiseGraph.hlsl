#ifndef TUNTENFISCH_NOISE_GRAPH
#define TUNTENFISCH_NOISE_GRAPH

#include "Assets/Compute/Voxels/Include/Noise/Noise.hlsl"

static const uint noiseGraphNodeTypePosition = 0;
static const uint noiseGraphNodeTypeDomainWarp = 1;
static const uint noiseGraphNodeTypeNoise = 2;
static const uint noiseGraphNodeTypeCSGOperation = 3;
static const uint noiseGraphNodeTypeCSGPrimitive = 4;
static const uint noiseGraphNodeTypeOutput = 5;

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
    float4 buffer[10];
    uint count;

    void Push(float4 value_gradient)
    {
        [branch]
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

            case 3:
                buffer[3] = value_gradient;
                break;

            case 4:
                buffer[4] = value_gradient;
                break;

            case 5:
                buffer[5] = value_gradient;
                break;

            case 6:
                buffer[6] = value_gradient;
                break;

            case 7:
                buffer[7] = value_gradient;
                break;

            case 8:
                buffer[8] = value_gradient;
                break;

            default:
                buffer[9] = value_gradient;
                break;
        }
    }

    void Push(float3 position)
    {
        Push(float4(position, 0.0f));
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

        [branch]
        switch(node.nodeType)
        {
            case noiseGraphNodeTypePosition:
                stack.Push(position);
                break;

            case noiseGraphNodeTypeDomainWarp:
                stack.Push(WarpDomain(stack.Pop().xyz, noiseGraphNoiseParameters[node.dataIndex]));
                break;

            case noiseGraphNodeTypeNoise:
                stack.Push(GenerateFBMNoise(stack.Pop().xyz, noiseGraphNoiseParameters[node.dataIndex]));
                break;
            
            case noiseGraphNodeTypeCSGOperation:
                float4 rhs = stack.Pop();
                float4 lhs = stack.Pop();
                stack.Push(ApplyCSGOperator(lhs, rhs, noiseGraphCSGOperators[node.dataIndex]));
                break;
            
            case noiseGraphNodeTypeCSGPrimitive:
                stack.Push(EvaluateCSGPrimitive(stack.Pop().xyz, noiseGraphCSGPrimitives[node.dataIndex]));
                break;
            
            case noiseGraphNodeTypeOutput:
                finalValue_finalGradient = stack.Pop();
                break;
        }
    }

    return finalValue_finalGradient;
}

#endif
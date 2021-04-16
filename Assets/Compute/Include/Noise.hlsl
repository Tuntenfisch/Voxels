#ifndef NOISE__559277903
#define NOISE__559277903

#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

interface INoise
{
    float4 GenerateNoise(float3 position);
};

struct SimplexNoise2D : INoise
{
    float4 GenerateNoise(float3 position)
    {
        float4 value_gradient = snoise_grad(float3(position.x, 0.0f, position.z)).wxyz;

        return float4(value_gradient.x, float3(value_gradient.y, 0.0f, value_gradient.w));
    }
};

float4 GenerateFBMNoise
(
    INoise noise,
    float3 position,
    uint numberOfOctaves,
    Buffer<float3> octaveOffsets,
    float frequency,
    float persistence,
    float lacunarity
)
{
    float4 value_gradient = 0.0f;
    float sumOfAmplitudes = 0.0f;
    float amplitude = 1.0f;

    for (uint octave = 0; octave < numberOfOctaves; octave++)
    {
        float4 value = noise.GenerateNoise(frequency * (position + octaveOffsets[octave]));

        sumOfAmplitudes += amplitude;
        
        value_gradient.x += amplitude * value.x;
        value_gradient.yzw += amplitude * value.yzw * frequency;

        amplitude *= persistence;
        frequency *= lacunarity;
    }
    value_gradient /= sumOfAmplitudes;
    
    return value_gradient;
}

float4 GetBillowNoise2D(float4 value_gradient)
{
    value_gradient.x = abs(value_gradient.x);
    value_gradient.yzw = value_gradient.x * value_gradient.yzw / abs(value_gradient.x);

    return value_gradient;
}

float4 GetRidgeNoise2D(float4 value_gradient)
{
    value_gradient = GetBillowNoise2D(value_gradient);

    value_gradient.x = -value_gradient.x;
    value_gradient.yzw *= -1.0f;

    return value_gradient;
}

#endif
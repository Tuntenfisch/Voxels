#ifndef NOISE__559277903
#define NOISE__559277903

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
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

struct SimplexNoise3D : INoise
{
    float4 GenerateNoise(float3 position)
    {
        return snoise_grad(position).wxyz;
    }
};

float3 CalculateOctaveOffset(int seed, uint octave)
{
    return GenerateHashedRandomFloat(uint2(seed, octave)) * 10000.0f;
}

// Based on https://www.iquilezles.org/www/articles/morenoise/morenoise.htm.
float4 GenerateFBMNoise
(
    int seed,
    INoise noise,
    float3 position,
    uint numberOfOctaves,
    float frequency,
    float persistence,
    float lacunarity
)
{
    float4 fbm_value_gradient = 0.0f;
    float3 sumOfGradients = 0.0f;
    float sumOfAmplitudes = 0.0f;
    float amplitude = 1.0f;

    for (uint octave = 0; octave < numberOfOctaves; octave++)
    {
        // We need to multiply the derivative by frequency because of the chain rule:
        // d / dp * n(f * p) = f * n'(f * p)
        float4 value_gradient = noise.GenerateNoise(frequency * (position + CalculateOctaveOffset(seed, octave)));
        value_gradient.yzw *= frequency;

        sumOfGradients += value_gradient.yzw;
        sumOfAmplitudes += amplitude;

        value_gradient *= amplitude / (1.0f + dot(sumOfGradients, sumOfGradients));
        fbm_value_gradient += value_gradient;

        amplitude *= persistence;
        frequency *= lacunarity;
    }
    fbm_value_gradient /= sumOfAmplitudes;
    
    return fbm_value_gradient;
}

float4 GetBillowNoise(float4 value_gradient)
{
    value_gradient.yzw = value_gradient.x * value_gradient.yzw / abs(value_gradient.x);
    value_gradient.x = abs(value_gradient.x) - 0.25f;

    return value_gradient;
}

float4 GetRidgeNoise(float4 value_gradient)
{
    value_gradient = GetBillowNoise(value_gradient);
    value_gradient *= -1.0f;

    return value_gradient;
}

#endif
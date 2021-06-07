#ifndef TUNTENFISCH_NOISE
#define TUNTENFISCH_NOISE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

#include "Assets/Compute/Voxels/Include/CSG.hlsl"

static const uint noiseXZ = 0;
static const uint noiseXYZ = 1;

static const uint noiseTypeDefault = 0;
static const uint noiseTypeBillow = 1;
static const uint noiseTypeRidge = 2;

struct NoiseParameters
{
    // General Parameters.
    uint seed;
    uint noiseAxes;
    uint noiseType;

    // FBM noiseParameters.
    uint numberOfOctaves;
    float initialAmplitude;
    float3 initialFrequency;
    float persistence;
    float3 lacunarity;

    static NoiseParameters Create(uint seed, uint noiseAxes, uint noiseType, uint numberOfOctaves, float initialAmplitude, float3 initialFrequency, float persistence, float3 lacunarity)
    {
        NoiseParameters noiseParameters;
        noiseParameters.seed = seed;
        noiseParameters.noiseAxes = noiseAxes;
        noiseParameters.noiseType = noiseType;
        noiseParameters.numberOfOctaves = numberOfOctaves;
        noiseParameters.initialAmplitude = initialAmplitude;
        noiseParameters.initialFrequency = initialFrequency;
        noiseParameters.persistence = persistence;
        noiseParameters.lacunarity = lacunarity;

        return noiseParameters;
    }
};

float3 CalculateOctaveOffset(uint seed, uint octave)
{
    return GenerateHashedRandomFloat(uint2(seed, octave)) * 10000.0f;
}

float4 GenerateNoise(float3 position, uint noiseAxes)
{
    switch(noiseAxes)
    {
        case noiseXZ:
            float3 value_gradient = SimplexNoiseGrad(position.xz).zxy;

            return float4(value_gradient.x, float3(value_gradient.y, 0.0f, value_gradient.z));
        default:
            return SimplexNoiseGrad(position).wxyz;
    }
}

// Based on https://www.iquilezles.org/www/articles/morenoise/morenoise.htm.
float4 GenerateDefaultFBMNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 fbmValue_fbmGradient = 0.0f;
    float3 sumOfGradients = 0.0f;
    float amplitude = noiseParameters.initialAmplitude;
    float3 frequency = noiseParameters.initialFrequency;

    for (uint octave = 0; octave < noiseParameters.numberOfOctaves; octave++)
    {
        // We need to multiply the derivative by frequency because of the chain rule:
        // d / dp * n(f * p) = f * n'(f * p)
        float4 value_gradient = GenerateNoise(frequency * (position + CalculateOctaveOffset(noiseParameters.seed, octave)), noiseParameters.noiseAxes);

        value_gradient.yzw *= frequency;
        sumOfGradients += value_gradient.yzw;
        value_gradient *= amplitude / (1.0f + dot(sumOfGradients, sumOfGradients));
        fbmValue_fbmGradient += value_gradient;

        amplitude *= noiseParameters.persistence;
        frequency *= noiseParameters.lacunarity;
    }

    return fbmValue_fbmGradient;
}

float4 GenerateFBMBillowNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 value_gradient = GenerateDefaultFBMNoise(position, noiseParameters);
    value_gradient.yzw = value_gradient.x * value_gradient.yzw / abs(value_gradient.x);
    value_gradient.x = abs(value_gradient.x) - 0.25f;

    return value_gradient;
}

float4 GenerateFBMRidgeNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 value_gradient = GenerateFBMBillowNoise(position, noiseParameters);
    value_gradient *= -1.0f;

    // Square ridge noise for more pronounced ridges.
    value_gradient.yzw = 2.0f * value_gradient.x * value_gradient.yzw;
    value_gradient.x = value_gradient.x * value_gradient.x - 0.0625f;

    return value_gradient;
}

float4 GenerateFBMNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 value_gradient = 0.0f;

    [branch]
    switch(noiseParameters.noiseType)
    {
        case noiseTypeRidge:
            value_gradient = GenerateFBMRidgeNoise(position, noiseParameters);
            break;

        case noiseTypeBillow:
            value_gradient = GenerateFBMBillowNoise(position, noiseParameters);
            break;

        default:
            value_gradient = GenerateDefaultFBMNoise(position, noiseParameters);
            break;
    }

    if (noiseParameters.noiseAxes == noiseXZ)
    {
        value_gradient *= -1.0f;
        value_gradient.x += position.y;
        value_gradient.z += 1.0f;
    }

    return value_gradient;
}

#endif
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
    [branch]
    switch(noiseAxes)
    {
        case noiseXZ:
            float3 valueAndGradient = SimplexNoiseGrad(position.xz).zxy;

            return float4(valueAndGradient.x, float3(valueAndGradient.y, 0.0f, valueAndGradient.z));
        default:
            return SimplexNoiseGrad(position).wxyz;
    }
}

// Based on https://www.iquilezles.org/www/articles/morenoise/morenoise.htm.
float4 GenerateDefaultFBMNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 fbmValueAndFBMGradient = 0.0f;
    float3 sumOfGradients = 0.0f;
    float amplitude = noiseParameters.initialAmplitude;
    float3 frequency = noiseParameters.initialFrequency;

    for (uint octave = 0; octave < noiseParameters.numberOfOctaves; octave++)
    {
        // We need to multiply the derivative by frequency because of the chain rule:
        // d / dp * n(f * p) = f * n'(f * p)
        float4 valueAndGradient = GenerateNoise(frequency * (position + CalculateOctaveOffset(noiseParameters.seed, octave)), noiseParameters.noiseAxes);

        valueAndGradient.yzw *= frequency;
        sumOfGradients += valueAndGradient.yzw;
        valueAndGradient *= amplitude / (1.0f + dot(sumOfGradients, sumOfGradients));
        fbmValueAndFBMGradient += valueAndGradient;

        amplitude *= noiseParameters.persistence;
        frequency *= noiseParameters.lacunarity;
    }

    return fbmValueAndFBMGradient;
}

float4 GenerateFBMBillowNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 valueAndGradient = GenerateDefaultFBMNoise(position, noiseParameters);
    valueAndGradient.yzw = valueAndGradient.x * valueAndGradient.yzw / abs(valueAndGradient.x);
    valueAndGradient.x = abs(valueAndGradient.x) - 0.25f;

    return valueAndGradient;
}

float4 GenerateFBMRidgeNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 valueAndGradient = GenerateFBMBillowNoise(position, noiseParameters);
    valueAndGradient *= -1.0f;

    // Square ridge noise for more pronounced ridges.
    valueAndGradient.yzw = 2.0f * valueAndGradient.x * valueAndGradient.yzw;
    valueAndGradient.x = valueAndGradient.x * valueAndGradient.x - 0.0625f;

    return valueAndGradient;
}

float4 GenerateFBMNoise(float3 position, NoiseParameters noiseParameters)
{
    float4 valueAndGradient = 0.0f;

    [branch]
    switch(noiseParameters.noiseType)
    {
        case noiseTypeRidge:
            valueAndGradient = GenerateFBMRidgeNoise(position, noiseParameters);
            break;

        case noiseTypeBillow:
            valueAndGradient = GenerateFBMBillowNoise(position, noiseParameters);
            break;

        default:
            valueAndGradient = GenerateDefaultFBMNoise(position, noiseParameters);
            break;
    }

    if (noiseParameters.noiseAxes == noiseXZ)
    {
        valueAndGradient *= -1.0f;
        valueAndGradient.x += position.y;
        valueAndGradient.z += 1.0f;
    }

    return valueAndGradient;
}

float3 WarpDomain(float3 position, NoiseParameters noiseParameters)
{
    float3 value = 0.0f;

    // Add a random offset to the position so the values for x, y and z aren't all the same.
    value.x = GenerateFBMNoise(position + float3(500.0f, 1000.0f, 1500.0f), noiseParameters).x;

    if (noiseParameters.noiseAxes != noiseXZ)
    {
        value.y = GenerateFBMNoise(position, noiseParameters).x;
    }
    value.z = GenerateFBMNoise(position - float3(500.0f, 1000.0f, 1500.0f), noiseParameters).x;

    return position + value;
}

#endif
#ifndef TUNTENFISCH_NOISE
#define TUNTENFISCH_NOISE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise2D.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

#include "Assets/Compute/Voxels/Include/ConstructiveSolidGeometry.hlsl"

#define NOISE_2D 0
#define NOISE_3D 1

#define NOISE_TYPE_DEFAULT 0
#define NOISE_TYPE_BILLOW 1
#define NOISE_TYPE_RIDGE 2

struct NoiseParameters
{
    // General Parameters.
    uint seed;
    uint noiseDimensionality;
    uint noiseType;

    // FBM parameters.
    uint numberOfOctaves;
    float initialAmplitude;
    float initialFrequency;
    float persistence;
    float lacunarity;

    // Combine parameters.
    uint operatorIndex;
    float smoothing;
};

StructuredBuffer<NoiseParameters> noiseLayers;

uint numberOfNoiseLayers;

float3 CalculateOctaveOffset(uint seed, uint octave)
{
    return GenerateHashedRandomFloat(uint2(seed, octave)) * 10000.0f;
}

float4 GenerateNoise(float3 position, uint noiseDimensionality)
{
    switch(noiseDimensionality)
    {
        case NOISE_2D:
            float3 value_gradient = SimplexNoiseGrad(position.xz).zxy;

            return float4(value_gradient.x, float3(value_gradient.y, 0.0f, value_gradient.z));
        default:
            return SimplexNoiseGrad(position).wxyz;
    }
}

// Based on https://www.iquilezles.org/www/articles/morenoise/morenoise.htm.
float4 GenerateDefaultFBMNoise(float3 position, NoiseParameters parameters)
{
    float4 fbmValue_fbmGradient = 0.0f;
    float3 sumOfGradients = 0.0f;
    float amplitude = parameters.initialAmplitude;
    float frequency = parameters.initialFrequency;

    for (uint octave = 0; octave < parameters.numberOfOctaves; octave++)
    {
        // We need to multiply the derivative by frequency because of the chain rule:
        // d / dp * n(f * p) = f * n'(f * p)
        float4 value_gradient = GenerateNoise(frequency * (position + CalculateOctaveOffset(parameters.seed, octave)), parameters.noiseDimensionality);

        value_gradient.yzw *= frequency;
        sumOfGradients += value_gradient.yzw;
        value_gradient *= amplitude / (1.0f + dot(sumOfGradients, sumOfGradients));
        fbmValue_fbmGradient += value_gradient;

        amplitude *= parameters.persistence;
        frequency *= parameters.lacunarity;
    }

    return fbmValue_fbmGradient;
}

float4 GenerateFBMBillowNoise(float3 position, NoiseParameters parameters)
{
    float4 value_gradient = GenerateDefaultFBMNoise(position, parameters);
    value_gradient.yzw = value_gradient.x * value_gradient.yzw / abs(value_gradient.x);
    value_gradient.x = abs(value_gradient.x) - 0.25f;

    return value_gradient;
}

float4 GenerateFBMRidgeNoise(float3 position, NoiseParameters parameters)
{
    float4 value_gradient = GenerateFBMBillowNoise(position, parameters);
    value_gradient *= -1.0f;

    // Square ridge noise for more pronounced ridges.
    value_gradient.yzw = 2.0f * value_gradient.x * value_gradient.yzw;
    value_gradient.x = value_gradient.x * value_gradient.x - 0.0625f;

    return value_gradient;
}

float4 GenerateFBMNoise(float3 position, NoiseParameters parameters)
{
    switch(parameters.noiseType)
    {
        case NOISE_TYPE_BILLOW:
            return GenerateFBMBillowNoise(position, parameters);

        case NOISE_TYPE_RIDGE:
            return GenerateFBMRidgeNoise(position, parameters);

        default:
            return GenerateDefaultFBMNoise(position, parameters);
    }
}

float4 GenerateLayeredFBMNoise(float3 position)
{
    float4 finalValue_finalGradient;

    for (uint noiseLayerIndex = 0; noiseLayerIndex < numberOfNoiseLayers; noiseLayerIndex++)
    {
        NoiseParameters parameters = noiseLayers[noiseLayerIndex];
        float4 value_gradient = GenerateFBMNoise(position, parameters);

        if (parameters.noiseDimensionality == NOISE_2D)
        {
            value_gradient *= -1.0f;
            value_gradient.x += position.y;
            value_gradient.z += 1.0f;
        }

        if (noiseLayerIndex == 0)
        {
            finalValue_finalGradient = value_gradient;
        }
        else
        {
            finalValue_finalGradient = ApplyCSGOperator(finalValue_finalGradient, value_gradient, parameters.operatorIndex, parameters.smoothing);
        }
    }

    return finalValue_finalGradient;
}

#endif
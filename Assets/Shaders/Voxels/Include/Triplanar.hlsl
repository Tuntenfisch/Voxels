#ifndef TUNTENFISCH_VOXELS_TRIPLANAR
#define TUNTENFISCH_VOXELS_TRIPLANAR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

void GetTriplanarProjections(float3 position, half3 normal, out float2 xProjection, out float2 yProjection, out float2 zProjection)
{
    position *= _TriplanarCoordinateScaling;

    xProjection = position.zy;
    yProjection = position.xz;
    zProjection = position.xy;

    // Account for mirrored mapping.
    if (normal.x < 0.0h)
    {
        xProjection.x = -xProjection.x;
    }

    if (normal.y < 0.0h)
    {
        yProjection.x = -yProjection.x;
    }

    if (normal.z >= 0.0h)
    {
        zProjection.x = -zProjection.x;
    }

    // Offset coordinates to prevent sudden repetitions.
    xProjection.y += 0.5h;
    zProjection.x += 0.5h;
}

float3 GetTriplanarWeights(float3 normal)
{
    float3 triplanarWeights = abs(normal);
    triplanarWeights = saturate(triplanarWeights - _TriplanarBlendOffset);
    triplanarWeights = pow(triplanarWeights, _TriplanarBlendExponent);
    triplanarWeights /= dot(triplanarWeights, 1.0f);

    return triplanarWeights;
}

half4 TriplanarSampleTexture2DArray(float3 position, half3 normal, TEXTURE2D_ARRAY(textureName), SAMPLER(samplerName), uint index)
{
    float2 xProjection, yProjection, zProjection;
    GetTriplanarProjections(position, normal, xProjection, yProjection, zProjection);

    float3 triplanarWeights = GetTriplanarWeights(normal);

    half4 valueX = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, xProjection, index);
    half4 valueY = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, yProjection, index);
    half4 valueZ = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, zProjection, index);

    return triplanarWeights.x * valueX + triplanarWeights.y * valueY + triplanarWeights.z * valueZ;
}

#endif
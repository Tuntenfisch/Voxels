#ifndef TUNTENFISCH_VOXELS_TRIPLANAR
#define TUNTENFISCH_VOXELS_TRIPLANAR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

half4 TriplanarSampleTexture2DArray(float3 position, half3 normal, TEXTURE2D_ARRAY(textureName), SAMPLER(samplerName), uint index)
{
    float2 xProjection = position.zy;
    float2 yProjection = position.xz;
    float2 zProjection = position.xy;

    float3 triplanarWeights = abs(normal);
    triplanarWeights /= dot(triplanarWeights, 1.0f);

    half4 albedoX = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, xProjection, index);
    half4 albedoY = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, yProjection, index);
    half4 albedoZ = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, zProjection, index);

    return triplanarWeights.x * albedoX + triplanarWeights.y * albedoY + triplanarWeights.z * albedoZ;
}

#endif
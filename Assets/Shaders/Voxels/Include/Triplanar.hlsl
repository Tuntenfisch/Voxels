#ifndef TUNTENFISCH_VOXELS_TRIPLANAR
#define TUNTENFISCH_VOXELS_TRIPLANAR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

void GetTriplanarProjections(float3 positionWS, half3 normalWS, out float2 uvX, out float2 uvY, out float2 uvZ)
{
    positionWS *= _TriplanarCoordinateScaling;

    uvX = positionWS.zy;
    uvY = positionWS.xz;
    uvZ = positionWS.xy;

    half3 signs = sign(normalWS);

    // Account for mirrored mapping.
    uvX.x *= signs.x;
    uvY.x *= signs.y;
    uvZ.x *= -signs.z;

    // Offset coordinates to prevent sudden repetitions.
    uvX.y += 0.5h;
    uvZ.x += 0.5h;
}

float3 GetTriplanarWeights(float3 normalWS)
{
    float3 triplanarWeights = abs(normalWS);
    triplanarWeights = saturate(triplanarWeights - _TriplanarBlendOffset);
    triplanarWeights = pow(triplanarWeights, _TriplanarBlendExponent);
    triplanarWeights /= dot(triplanarWeights, 1.0f);

    return triplanarWeights;
}

half4 TriplanarSampleTexture2DArray(float3 positionWS, half3 normalWS, TEXTURE2D_ARRAY(textureName), SAMPLER(samplerName), uint index)
{
    float2 uvX, uvY, uvZ;
    GetTriplanarProjections(positionWS, normalWS, uvX, uvY, uvZ);

    float3 triplanarWeights = GetTriplanarWeights(normalWS);

    half4 valueX = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, uvX, index);
    half4 valueY = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, uvY, index);
    half4 valueZ = SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, uvZ, index);

    return triplanarWeights.x * valueX + triplanarWeights.y * valueY + triplanarWeights.z * valueZ;
}

#endif
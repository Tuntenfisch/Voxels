#ifndef TUNTENFISCH_VOXELS_SHADOW_CASTER_PASS
#define TUNTENFISCH_VOXELS_SHADOW_CASTER_PASS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct VertexPassInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct FragmentPassInput
{
    float4 positionCS : SV_POSITION;
};

float4 GetShadowPositionHClip(VertexPassInput input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

    #if defined(UNITY_REVERSED_Z)
        positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
        positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

FragmentPassInput ShadowPassVertex(VertexPassInput input)
{
    FragmentPassInput output;
    output.positionCS = GetShadowPositionHClip(input);

    return output;
}

half4 ShadowPassFragment(FragmentPassInput input) : SV_TARGET
{
    return 0.0h;
}

#endif
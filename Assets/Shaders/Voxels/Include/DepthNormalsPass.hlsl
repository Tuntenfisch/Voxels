#ifndef TUNTENFISCH_VOXELS_DEPTH_ONLY_PASS
#define TUNTENFISCH_VOXELS_DEPTH_ONLY_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct VertexPassInput
{
    float4 positionOS : POSITION;
    float3 normal : NORMAL;
};

struct FragmentPassInput
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD2;
};

FragmentPassInput DepthNormalsVertex(VertexPassInput input)
{
    FragmentPassInput output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal);
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);

    return output;
}

float4 DepthNormalsFragment(FragmentPassInput input) : SV_TARGET
{
    return float4(PackNormalOctRectEncode(TransformWorldToViewDir(input.normalWS, true)), 0.0f, 0.0f);
}

#endif
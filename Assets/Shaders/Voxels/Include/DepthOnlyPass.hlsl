#ifndef TUNTENFISCH_VOXELS_DEPTH_ONLY_PASS
#define TUNTENFISCH_VOXELS_DEPTH_ONLY_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct VertexPassInput
{
    float4 position : POSITION;
};

struct FragmentPassInput
{
    float4 positionCS : SV_POSITION;
};

FragmentPassInput DepthOnlyVertex(VertexPassInput input)
{
    FragmentPassInput output;
    output.positionCS = TransformObjectToHClip(input.position.xyz);

    return output;
}

half4 DepthOnlyFragment(FragmentPassInput input) : SV_TARGET
{
    return 0;
}

#endif
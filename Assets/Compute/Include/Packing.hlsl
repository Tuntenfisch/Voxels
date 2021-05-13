#ifndef TUNTENFISCH_PACKING
#define TUNTENFISCH_PACKING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

uint PackFloats(float2 unpackedFloats)
{
    uint packedFloats = f32tof16(unpackedFloats.x) | f32tof16(unpackedFloats.y) << 16;

    return packedFloats;
}

float2 UnpackFloats(uint packedFloats)
{
    float2 unpackedFloats = float2
    (
        f16tof32(packedFloats),
        f16tof32(packedFloats >> 16)
    );

    return unpackedFloats;
}

#endif
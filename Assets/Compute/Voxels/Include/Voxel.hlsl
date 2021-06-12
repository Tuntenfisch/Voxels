#ifndef TUNTENFISCH_VOXEL
#define TUNTENFISCH_VOXEL

#include "Assets/Compute/Include/Packing.hlsl"

struct Voxel
{
    float value;
    uint packedGradient;

    float GetValue()
    {
        return value;
    }
    
    float3 GetGradient()
    {
        return UnpackNormalOctQuadEncode(UnpackFloats(packedGradient));
    }

    float4 GetValueAndGradient()
    {
        return float4(GetValue(), GetGradient());
    }

    static Voxel Create(float value, float3 gradient)
    {
        Voxel voxel;
        voxel.value = value;
        voxel.packedGradient = PackFloats(PackNormalOctQuadEncode(normalize(gradient)));

        return voxel;
    }
};

#endif
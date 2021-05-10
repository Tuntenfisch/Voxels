#ifndef VOXEL__559277903
#define VOXEL__559277903

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

    static Voxel Create(float value, float3 gradient)
    {
        Voxel voxel;
        voxel.value = value;
        voxel.packedGradient = PackFloats(PackNormalOctQuadEncode(normalize(gradient)));

        return voxel;
    }
};

#endif
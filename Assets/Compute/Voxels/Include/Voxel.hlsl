#ifndef TUNTENFISCH_VOXELS_VOXEL
#define TUNTENFISCH_VOXELS_VOXEL

#include "Assets/Compute/Include/Packing.hlsl"
#include "Assets/Compute/Voxels/Include/MaterialIndex.hlsl"

struct Voxel
{
    float4 valueAndGradient;
    uint materialIndex;

    float GetValue()
    {
        return valueAndGradient.x;
    }
    
    float3 GetGradient()
    {
        return valueAndGradient.yzw;
    }

    static Voxel Create(float value, float3 gradient, uint materialIndex)
    {
        Voxel voxel;

        voxel.valueAndGradient = float4(value, gradient);
        voxel.materialIndex = value >= 0.0f ? materialIndexAir : materialIndex;

        return voxel;
    }
};

struct PackedVoxel
{
    uint packedValueAndMaterialIndex;
    uint packedGradient;

    static PackedVoxel Create(uint packedValueAndMaterialIndex, uint packedGradient)
    {
        PackedVoxel packedVoxel;
        packedVoxel.packedValueAndMaterialIndex = packedValueAndMaterialIndex;
        packedVoxel.packedGradient = packedGradient;

        return packedVoxel;
    }
};

PackedVoxel PackVoxel(Voxel voxel)
{
    // Technically value can be any arbitrarily large float so using only 16 bits for
    // precision wouldn't be great. But since we only actually do something with a
    // voxel's value if it's close to 0 (that means the voxel is near the isosurface)
    // it shouldn't pose a problem.
    uint packedValueAndMaterialIndex = f32tof16(voxel.GetValue()) | voxel.materialIndex << 16;
    uint packedGradient = PackFloats(PackNormalOctQuadEncode(normalize(voxel.GetGradient())));

    return PackedVoxel::Create(packedValueAndMaterialIndex, packedGradient);
}

Voxel UnpackVoxel(PackedVoxel voxel)
{
    float value = f16tof32(voxel.packedValueAndMaterialIndex);
    float3 gradient = UnpackNormalOctQuadEncode(UnpackFloats(voxel.packedGradient));
    uint materialIndex = voxel.packedValueAndMaterialIndex >> 16;

    return Voxel::Create(value, gradient, materialIndex);
}

#endif
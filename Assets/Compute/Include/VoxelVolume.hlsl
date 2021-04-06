#ifndef VOXEL_VOLUME
#define VOXEL_VOLUME

#include "HermiteVolume.hlsl"

uint3 voxelDimensions;
float voxelSpacing;
uint voxelStride;
float3 localToWorldOffset;

bool IsOutOfVoxelBounds(uint3 position)
{
    return any(step(voxelDimensions, position));
}

float3 CalculateLocalPosition(uint3 position)
{
    return voxelStride * voxelSpacing * (position - 0.5f * voxelDimensions);
}

float3 CalculateWorldPosition(uint3 position)
{
    return CalculateLocalPosition(position) + localToWorldOffset;
}

#endif
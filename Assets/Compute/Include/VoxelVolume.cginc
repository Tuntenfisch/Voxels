#ifndef VOXEL_VOLUME
#define VOXEL_VOLUME

#include "Assets/Compute/Include/HermiteVolume.cginc"

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
    return voxelStride * voxelSpacing * (position - 0.5 * voxelDimensions + 0.5);
}

float3 CalculateWorldPosition(uint3 position)
{
    return CalculateLocalPosition(position) + localToWorldOffset;
}

#endif
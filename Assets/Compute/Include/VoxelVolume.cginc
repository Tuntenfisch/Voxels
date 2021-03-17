#ifndef VOXEL_VOLUME
#define VOXEL_VOLUME

#include "Assets/Compute/Include/DensityVolume.cginc"

uint3 voxelDimensions;
float voxelSpacing;
float3 offset;

bool IsOutOfVoxelBounds(uint3 id)
{
    return any(step(voxelDimensions, id));
}

float3 CalculateLocalPosition(uint3 id)
{
    return stride * voxelSpacing * (id - 0.5 * voxelDimensions + 0.5);
}

float3 CalculateWorldPosition(uint3 id)
{
    return CalculateLocalPosition(id) + offset;
}

#endif
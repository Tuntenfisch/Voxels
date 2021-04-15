#ifndef VOXEL_VOLUME__559277903
#define VOXEL_VOLUME__559277903

RWStructuredBuffer<Voxel> voxelVolume;

uint3 voxelDimensions;

bool IsOutOfVoxelVolumeBounds(uint3 voxelID)
{
    return any(step(voxelDimensions, voxelID));
}

uint3 ClampToVoxelVolumeBounds(uint3 voxelID)
{
    return clamp(voxelID, 0, voxelDimensions - 1);
}

bool IsOnVoxelVolumeSurface(uint3 voxelID)
{
    return any(voxelID == 0 || voxelID == voxelDimensions - 1);
}

uint CalculateVoxelVolumeIndex(uint3 voxelID)
{
    return dot(voxelID, uint3(1, voxelDimensions.x, voxelDimensions.x * voxelDimensions.y));
}

#endif
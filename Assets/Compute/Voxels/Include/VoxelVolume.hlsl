#ifndef TUNTENFISCH_VOXELS_VOXEL_VOLUME
#define TUNTENFISCH_VOXELS_VOXEL_VOLUME

RWStructuredBuffer<PackedVoxel> voxelVolume;

uint3 numberOfVoxels;
float voxelSpacing;
float3 voxelVolumeToWorldSpaceOffset;

bool IsOutOfVoxelVolumeBounds(uint3 coordinate)
{
    return any(coordinate > numberOfVoxels - 1);
}

uint CalculateVoxelVolumeIndex(uint3 coordinate)
{
    return dot(coordinate, uint3(1, numberOfVoxels.x, numberOfVoxels.x * numberOfVoxels.y));
}

Voxel GetVoxel(uint3 coordinate)
{
    return UnpackVoxel(voxelVolume[CalculateVoxelVolumeIndex(coordinate)]);
}

void SetVoxel(uint3 coordinate, Voxel voxel)
{
    voxelVolume[CalculateVoxelVolumeIndex(coordinate)] = PackVoxel(voxel);
}

float3 VoxelToVoxelVolumeSpace(uint3 coordinate, float3 position = 0.0f)
{
    return voxelSpacing * (position + coordinate - 0.5f * (numberOfVoxels - 1.0f));
}

float3 VoxelVolumeToVoxelSpace(uint3 coordinate, float3 position = 0.0f)
{
    return position / voxelSpacing - coordinate + 0.5f * (numberOfVoxels - 1.0f);
}

float3 VoxelVolumeToWorldSpace(float3 position)
{
    return position + voxelVolumeToWorldSpaceOffset;
}

float3 WorldToVoxelVolumeSpace(float3 position)
{
    return position - voxelVolumeToWorldSpaceOffset;
}

#endif
#ifndef VOXEL_VOLUME__559277903
#define VOXEL_VOLUME__559277903

RWStructuredBuffer<Voxel> voxelVolume;

uint3 voxelVolumeCount;
float voxelSpacing;
float3 voxelVolumeToWorldOffset;

bool IsOutOfVoxelVolumeBounds(uint3 id, int padding = 0)
{
    return any(step(voxelVolumeCount + padding, id));
}

uint3 ClampToVoxelVolumeBounds(uint3 id)
{
    return clamp(id, 0, voxelVolumeCount - 1);
}

uint CalculateVoxelVolumeIndex(uint3 id)
{
    return dot(id, uint3(1, voxelVolumeCount.x, voxelVolumeCount.x * voxelVolumeCount.y));
}

Voxel GetVoxel(uint3 id)
{
    return voxelVolume[CalculateVoxelVolumeIndex(id)];
}

void SetVoxel(uint3 id, Voxel voxel)
{
    voxelVolume[CalculateVoxelVolumeIndex(id)] = voxel;
}

float3 VoxelToVoxelVolumeSpace(uint3 id, float3 position = 0.0f)
{
    return voxelSpacing * (position + id - 0.5f * (voxelVolumeCount - 1.0f));
}

float3 VoxelVolumeToVoxelSpace(uint3 id, float3 position = 0.0f)
{
    return position / voxelSpacing - id + 0.5f * (voxelVolumeCount - 1.0f);
}

float3 VoxelVolumeToWorldSpace(float3 position)
{
    return position + voxelVolumeToWorldOffset;
}

float3 WorldToVoxelVolumeSpace(float3 position)
{
    return position - voxelVolumeToWorldOffset;
}

#endif
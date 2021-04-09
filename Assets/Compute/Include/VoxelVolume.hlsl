#ifndef VOXEL_VOLUME

    #define VOXEL_VOLUME

    uint3 voxelDimensions;
    uint voxelStride;
    float voxelSpacing;
    float3 voxelVolumeToWorldSpaceOffset;

    bool IsOutOfVoxelBounds(uint3 voxelID)
    {
        return any(step(voxelDimensions, voxelID));
    }

    float3 VoxelToVoxelVolumeSpace(uint3 voxelID, float3 voxelSpacePosition = 0.0f)
    {
        return voxelStride * voxelSpacing * ((voxelSpacePosition + voxelID) - 0.5f * voxelDimensions);
    }

    float3 VoxelVolumeToVoxelSpace(uint3 voxelID, float3 voxelVolumeSpacePosition = 0.0f)
    {
        return(voxelVolumeSpacePosition / (voxelStride * voxelSpacing) + 0.5f * voxelDimensions) - voxelID;
    }

    float3 VoxelVolumeToWorldSpace(float3 voxelVolumeSpacePosition)
    {
        return voxelVolumeSpacePosition + voxelVolumeToWorldSpaceOffset;
    }

    float3 WorldToVoxelVolumeSpace(float3 worldSpacePosition)
    {
        return worldSpacePosition - voxelVolumeToWorldSpaceOffset;
    }

#endif
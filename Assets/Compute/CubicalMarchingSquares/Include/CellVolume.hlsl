#ifndef CELL_VOLUME__559277903
#define CELL_VOLUME__559277903

uint3 cellDimensions;
float cellSpacing;
float3 cellVolumeToWorldSpaceOffset;

bool IsOutOfCellBounds(uint3 cellID)
{
    return any(step(cellDimensions, cellID));
}

float3 CellToCellVolumeSpace(uint3 cellID, float3 cellSpacePosition = 0.0f)
{
    return cellSpacing * ((cellSpacePosition + cellID) - 0.5f * cellDimensions);
}

float3 CellVolumeToCellSpace(uint3 cellID, float3 cellVolumeSpacePosition = 0.0f)
{
    return(cellVolumeSpacePosition / cellSpacing + 0.5f * cellDimensions) - cellID;
}

float3 CellVolumeToWorldSpace(float3 cellVolumeSpacePosition)
{
    return cellVolumeSpacePosition + cellVolumeToWorldSpaceOffset;
}

float3 WorldToCellVolumeSpace(float3 worldSpacePosition)
{
    return worldSpacePosition - cellVolumeToWorldSpaceOffset;
}

#endif
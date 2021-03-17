#ifndef DENSITY_VOLUME
#define DENSITY_VOLUME

RWStructuredBuffer<float> densityVolume;
RWStructuredBuffer<float3> densityGradient;

uint3 densityDimensions;
uint stride;

bool IsOutOfDensityBounds(uint3 id)
{
    return any(step(densityDimensions, id));
}

uint CalculateDensityIndex(uint3 id)
{
    return stride * dot(id, uint3(1, densityDimensions.x, densityDimensions.x * densityDimensions.y));
}

#endif
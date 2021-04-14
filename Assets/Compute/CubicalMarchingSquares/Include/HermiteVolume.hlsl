#ifndef HERMITE_VOLUME__559277903
#define HERMITE_VOLUME__559277903

RWStructuredBuffer<HermiteSample> hermiteVolume;

uint3 hermiteDimensions;

bool IsOutOfHermiteVolumeBounds(uint3 hermiteID)
{
    return any(step(hermiteDimensions, hermiteID));
}

uint3 ClampToHermiteVolumeBounds(uint3 hermiteID)
{
    return clamp(hermiteID, 0, hermiteDimensions - 1);
}

bool IsOnHermiteVolumeSurface(uint3 hermiteID)
{
    return any(hermiteID == 0 || hermiteID == hermiteDimensions - 1);
}

uint CalculateHermiteVolumeIndex(uint3 hermiteID)
{
    return dot(hermiteID, uint3(1, hermiteDimensions.x, hermiteDimensions.x * hermiteDimensions.y));
}

#endif
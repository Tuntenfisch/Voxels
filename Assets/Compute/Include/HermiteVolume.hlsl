#ifndef HERMITE_VOLUME

    #define HERMITE_VOLUME

    RWStructuredBuffer<HermiteSample> hermiteVolume;

    uint3 hermiteDimensions;

    bool IsOutOfHermiteBounds(uint3 hermiteID)
    {
        return any(step(hermiteDimensions, hermiteID));
    }

    bool IsOnHermiteSurface(uint3 hermiteID)
    {
        return any(hermiteID == 0 || hermiteID == hermiteDimensions - 1);
    }

    uint CalculateHermiteIndex(uint3 hermiteID)
    {
        return dot(hermiteID, uint3(1, hermiteDimensions.x, hermiteDimensions.x * hermiteDimensions.y));
    }

#endif
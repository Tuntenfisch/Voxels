#ifndef HERMITE_VOLUME
#define HERMITE_VOLUME

// >>> HermiteData
struct HermiteData
{
    float4 density_gradient;
};

void InitializeHermiteData(out HermiteData hermiteData, float density, float3 gradient)
{
    hermiteData.density_gradient.x = density;
    hermiteData.density_gradient.yzw = gradient;
}

float GetHermiteDataDensity(HermiteData hermiteData)
{
    return hermiteData.density_gradient.x;
}

float3 GetHermiteDataGradient(HermiteData hermiteData)
{
    return hermiteData.density_gradient.yzw;
}
// <<<

RWStructuredBuffer<HermiteData> hermiteVolume;

uint3 hermiteDimensions;

bool IsOutOfHermiteBounds(uint3 position)
{
    return any(step(hermiteDimensions, position));
}

uint CalculateHermiteIndex(uint3 position)
{
    return dot(position, uint3(1, hermiteDimensions.x, hermiteDimensions.x * hermiteDimensions.y));
}

#endif
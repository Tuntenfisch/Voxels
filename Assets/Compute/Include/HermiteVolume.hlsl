#ifndef HERMITE_VOLUME
#define HERMITE_VOLUME

// >>> HermiteData
struct HermiteSample
{
    float4 density_gradient;
    
    float GetDensity()
    {
        return density_gradient.x;
    }
    
    float3 GetGradient()
    {
        return density_gradient.yzw;
    }
};

HermiteSample HermiteSampleConstructor(float density, float3 gradient)
{
    HermiteSample sample;
    sample.density_gradient.x = density;
    sample.density_gradient.yzw = gradient;

    return sample;
}
// <<<

RWStructuredBuffer<HermiteSample> hermiteVolume;

uint3 hermiteDimensions;

bool IsOutOfHermiteBounds(uint3 position)
{
    return any(step(hermiteDimensions, position));
}

bool IsOnHermiteSurface(uint3 position)
{
    return any(position == 0 || position == hermiteDimensions - 1);
}

uint CalculateHermiteIndex(uint3 position)
{
    return dot(position, uint3(1, hermiteDimensions.x, hermiteDimensions.x * hermiteDimensions.y));
}

#endif
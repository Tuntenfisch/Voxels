#ifndef DENSITY_FUNCTIONS
#define DENSITY_FUNCTIONS

// Several distance functions taken from https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm.

float Sphere(float3 samplePosition, float radius)
{
    return radius - length(samplePosition);
}

float Box(float3 samplePosition, float3 extent)
{
    float3 q = abs(samplePosition) - extent;
    return -length(max(q, 0.0)) - min(max(q.x, max(q.y, q.z)), 0.0);
}

float RoundBox(float3 samplePosition, float3 extent, float cornerRadius)
{
    return Box(samplePosition, extent) + cornerRadius;
}

float SolidAngle(float3 samplePosition, float2 sinCosOfAngle, float radius)
{
    float2 q = float2(length(samplePosition.xz), samplePosition.y);
    float l = length(q) - radius;
    float m = length(q - sinCosOfAngle * clamp(dot(q, sinCosOfAngle), 0.0, radius));
    return -max(l, m * sign(sinCosOfAngle.y * q.x - sinCosOfAngle.x * q.y));
}

#endif
#ifndef SDF
#define SDF

float4 Union(float4 a, float4 b)
{
    return a.x < b.x ? a : b;
}

float4 Intersection(float4 a, float4 b)
{
    return a.x > b.x ? a : b;
}

float4 Difference(float4 a, float4 b)
{
    return Intersection(-a, b);
}

float4 Cube(float3 position, float3 center, float3 size)
{
    float4 value_gradient = 0.0f;
    
    float3 d = abs(position - center) - 0.5f * size;
    float3 s = sign(position - center);
    float g = max(d.x, max(d.y, d.z));
    
    value_gradient.x = length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0f);
    value_gradient.yzw = s * ((g > 0.0) ? normalize(max(d, 0.0)) : step(d.yzx, d.xyz) * step(d.zxy, d.xyz));
    
    return value_gradient;
}

float4 Sphere(float3 position, float3 center, float radius)
{
    float4 value_gradient = 0.0f;
    
    value_gradient.x = length(position - center) - radius;
    value_gradient.yzw = normalize(position - center);
    
    return value_gradient;
}

#endif
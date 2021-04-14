#ifndef SDF__559277903
#define SDF__559277903

// Pretty much everything below is either copied from or based on articles by Inigo Quilez (https://www.iquilezles.org/).

interface ISDFPrimitive
{
    float4 Evaluate(float3 position);
};

float4 Union(float4 a, float4 b)
{
    return a.x < b.x ? a : b;
}

float4 Intersection(float4 a, float4 b)
{
    return -Union(-a, -b);
}

float4 Difference(float4 a, float4 b)
{
    return Intersection(a, -b);
}

float4 SmoothUnion(float4 a, float4 b, float smoothing)
{
    float h = max(smoothing - abs(a.x - b.x), 0.0f);
    float m = 0.25f * h * h / smoothing;
    float n = 0.50f * h / smoothing;

    return float4(min(a.x, b.x) - m, lerp(a.yzw, b.yzw, a.x < b.x ? n : 1.0f - n));
}

float4 SmoothIntersection(float4 a, float4 b, float smoothing)
{
    return -SmoothUnion(-a, -b, smoothing);
}

float4 SmoothDifference(float4 a, float4 b, float smoothing)
{
    return SmoothIntersection(a, -b, smoothing);
}

struct SDFCube : ISDFPrimitive
{
    float3 center;
    float3 size;

    float4 Evaluate(float3 position)
    {
        position -= center;

        float4 value_gradient = 0.0f;
        
        float3 d = abs(position) - 0.5f * size;
        float3 smoothing = sign(position);
        float g = max(d.x, max(d.y, d.z));
        
        value_gradient.x = length(max(d, 0.0f)) + min(max(d.x, max(d.y, d.z)), 0.0f);
        value_gradient.yzw = smoothing * (g > 0.0f ? normalize(max(d, 0.0f)) : step(d.yzx, d.xyz) * step(d.zxy, d.xyz));
        
        return value_gradient;
    }

    static SDFCube Create(float3 center = 0.0f, float3 size = 1.0f)
    {
        SDFCube cube;
        cube.center = center;
        cube.size = size;

        return cube;
    }
};

struct SDFSphere : ISDFPrimitive
{
    float3 center;
    float radius;

    float4 Evaluate(float3 position)
    {
        position -= center;

        float4 value_gradient = 0.0f;
        
        value_gradient.x = length(position) - radius;
        value_gradient.yzw = normalize(position);
        
        return value_gradient;
    }

    static SDFSphere Create(float3 center = 0.0f, float radius = 0.5f)
    {
        SDFSphere sphere;
        sphere.center = center;
        sphere.radius = radius;

        return sphere;
    }
};

#endif
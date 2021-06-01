#ifndef TUNTENFISCH_CONSTRUCTIVE_SOLID_GEOMETRY
#define TUNTENFISCH_CONSTRUCTIVE_SOLID_GEOMETRY

// Pretty much everything below is either copied from or based on articles by Inigo Quilez (https://www.iquilezles.org/).

#define CSG_UNION 0
#define CSG_INTERSECTION 1
#define CSG_DIFFERENCE 2
#define CSG_SMOOTH_UNION 3
#define CSG_SMOOTH_INTERSECTION 4
#define CSG_SMOOTH_DIFFERENCE 5

float4 Union(float4 lhs, float4 rhs)
{
    return lhs.x < rhs.x ? lhs : rhs;
}

float4 Intersection(float4 lhs, float4 rhs)
{
    return -Union(-lhs, -rhs);
}

float4 Difference(float4 lhs, float4 rhs)
{
    return Intersection(lhs, -rhs);
}

float4 SmoothUnion(float4 lhs, float4 rhs, float smoothing)
{
    float h = max(smoothing - abs(lhs.x - rhs.x), 0.0f);
    float m = 0.25f * h * h / smoothing;
    float n = 0.50f * h / smoothing;

    return float4(min(lhs.x, rhs.x) - m, lerp(lhs.yzw, rhs.yzw, lhs.x < rhs.x ? n : 1.0f - n));
}

float4 SmoothIntersection(float4 lhs, float4 rhs, float smoothing)
{
    return -SmoothUnion(-lhs, -rhs, smoothing);
}

float4 SmoothDifference(float4 lhs, float4 rhs, float smoothing)
{
    return SmoothIntersection(lhs, -rhs, smoothing);
}

float4 ApplyCSGOperator(float4 lhs, float4 rhs, uint operatorIndex, float smoothing = 1.0f)
{
    switch (operatorIndex)
    {
        case CSG_UNION:
            return Union(lhs, rhs);

        case CSG_INTERSECTION:
            return Intersection(lhs, rhs);

        case CSG_DIFFERENCE:
            return Difference(lhs, rhs);

        case CSG_SMOOTH_UNION:
            return SmoothUnion(lhs, rhs, smoothing);

        case CSG_SMOOTH_INTERSECTION:
            return SmoothIntersection(lhs, rhs, smoothing);

        default:
            return SmoothDifference(lhs, rhs, smoothing);
    }
}

interface ICSGPrimitive
{
    float4 Evaluate(float3 position);
};

struct CSGCube : ICSGPrimitive
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

    static CSGCube Create(float3 center = 0.0f, float3 size = 1.0f)
    {
        CSGCube cube;
        cube.center = center;
        cube.size = size;

        return cube;
    }
};

struct CSGSphere : ICSGPrimitive
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

    static CSGSphere Create(float3 center = 0.0f, float radius = 0.5f)
    {
        CSGSphere sphere;
        sphere.center = center;
        sphere.radius = radius;

        return sphere;
    }
};

#endif
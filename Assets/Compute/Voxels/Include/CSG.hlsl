#ifndef TUNTENFISCH_VOXELS_CSG
#define TUNTENFISCH_VOXELS_CSG

// Pretty much everything below is either copied from or based on articles by Inigo Quilez (https://www.iquilezles.org/).

static const uint csgOperatorIndexUnion = 0;
static const uint csgOperatorIndexIntersection = 1;
static const uint csgOperatorIndexDifference = 2;
static const uint csgOPeratorIndexSmoothUnion = 3;
static const uint csgOPeratorIndexSmoothIntersection = 4;
static const uint csgOperatorIndexSmoothDifference = 5;

static const uint csgPrimitiveTypeSphere = 0;
static const uint csgPrimitiveTypeCuboid = 1;

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

struct CSGOperator
{
    uint operatorIndex;
    float smoothing;
};

float4 ApplyCSGOperator(float4 lhs, float4 rhs, CSGOperator csgOperator)
{
    [branch]
    switch(csgOperator.operatorIndex)
    {
        case csgOperatorIndexUnion:
            return Union(lhs, rhs);

        case csgOperatorIndexIntersection:
            return Intersection(lhs, rhs);

        case csgOperatorIndexDifference:
            return Difference(lhs, rhs);

        case csgOPeratorIndexSmoothUnion:
            return SmoothUnion(lhs, rhs, csgOperator.smoothing);

        case csgOPeratorIndexSmoothIntersection:
            return SmoothIntersection(lhs, rhs, csgOperator.smoothing);

        default:
            return SmoothDifference(lhs, rhs, csgOperator.smoothing);
    }
}

struct CSGPrimitive
{
    uint materialIndex;
    uint primitiveType;
};

float4 EvaluateCSGSphere(float3 position)
{
    float4 valueAndGradient = 0.0f;
    
    valueAndGradient.x = length(position) - 0.5f;
    valueAndGradient.yzw = normalize(position);

    return valueAndGradient;
}

float4 EvaluateCSGCuboid(float3 position)
{
    float4 valueAndGradient = 0.0f;
    
    float3 d = abs(position) - 0.5f;
    float3 smoothing = sign(position);
    float g = max(d.x, max(d.y, d.z));
    
    valueAndGradient.x = length(max(d, 0.0f)) + min(max(d.x, max(d.y, d.z)), 0.0f);
    valueAndGradient.yzw = smoothing * (g > 0.0f ? normalize(max(d, 0.0f)) : step(d.yzx, d.xyz) * step(d.zxy, d.xyz));
    
    return valueAndGradient;
}

float4 EvaluateCSGPrimitive(float3 position, CSGPrimitive primitive)
{
    [branch]
    switch(primitive.primitiveType)
    {
        case csgPrimitiveTypeSphere:
            return EvaluateCSGSphere(position);

        default:
            return EvaluateCSGCuboid(position);
    }
}

#endif
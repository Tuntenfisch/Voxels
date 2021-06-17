#ifndef TUNTENFISCH_VERTEX
#define TUNTENFISCH_VERTEX

#include "Assets/Compute/Include/Packing.hlsl"

struct Vertex
{
    float3 position;
    uint2 packedNormal;

    float3 GetPosition()
    {
        return position;
    }

    void SetPosition(float3 newPosition)
    {
        position = newPosition;
    }

    float3 GetNormal()
    {
        float3 normal = float3
        (
            UnpackFloats(packedNormal.x),
            UnpackFloats(packedNormal.y).x
        );

        return normal;
    }

    void SetNormal(float3 newNormal)
    {
        packedNormal.x = PackFloats(newNormal.xy);
        packedNormal.y = PackFloats(float2(newNormal.z, 0.0f));
    }

    static Vertex Create(float3 position = 0.0f, float3 normal = 0.0f)
    {
        Vertex vertex;
        vertex.position = position;
        vertex.packedNormal.x = PackFloats(normal.xy);
        vertex.packedNormal.y = PackFloats(float2(normal.z, 0.0f));

        return vertex;
    }
};

#endif
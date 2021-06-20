#ifndef TUNTENFISCH_VERTEX
#define TUNTENFISCH_VERTEX

#include "Assets/Compute/Include/Packing.hlsl"

struct Vertex
{
    float3 position;
    float3 normal;

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
        return normal;
    }

    void SetNormal(float3 newNormal)
    {
        normal = newNormal;
    }

    static Vertex Create(float3 position = 0.0f, float3 normal = 0.0f)
    {
        Vertex vertex;
        vertex.position = position;
        vertex.normal = normal;

        return vertex;
    }
};

#endif
#ifndef TUNTENFISCH_VOXELS_VERTEX
#define TUNTENFISCH_VOXELS_VERTEX

#include "Assets/Compute/Include/Packing.hlsl"

struct Vertex
{
    float3 position;
    uint2 halfPrecisionNormal;
    uint materialIndex;

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
        return float3(UnpackFloats(halfPrecisionNormal.x), UnpackFloats(halfPrecisionNormal.y).x);
    }

    void SetNormal(float3 newNormal)
    {
        halfPrecisionNormal = uint2(PackFloats(newNormal.xy), PackFloats(float2(newNormal.z, 0.0f)));
    }

    uint GetMaterialIndex()
    {
        return materialIndex;
    }

    void SetMaterialIndex(uint newMaterialIndex)
    {
        materialIndex = newMaterialIndex;
    }

    static Vertex Create(float3 position = 0.0f, float3 normal = 0.0f, uint materialIndex = 0)
    {
        Vertex vertex;
        vertex.position = position;
        vertex. halfPrecisionNormal = uint2(PackFloats(normal.xy), PackFloats(float2(normal.z, 0.0f)));
        vertex.materialIndex = materialIndex;

        return vertex;
    }
};

#endif
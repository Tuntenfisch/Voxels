#ifndef TUNTENFISCH_VOXELS_VERTEX
#define TUNTENFISCH_VOXELS_VERTEX

#include "Assets/Compute/Include/Packing.hlsl"

struct Vertex
{
    float3 position;
    float materialIndex;

    float3 GetPosition()
    {
        return position;
    }

    void SetPosition(float3 newPosition)
    {
        position = newPosition;
    }

    uint GetMaterialIndex()
    {
        return materialIndex;
    }

    void SetMaterialIndex(uint newMaterialIndex)
    {
        materialIndex =  newMaterialIndex;
    }

    static Vertex Create(float3 position = 0.0f, uint materialIndex = 0)
    {
        Vertex vertex;
        vertex.position = position;
        vertex.materialIndex = materialIndex;

        return vertex;
    }
};

#endif
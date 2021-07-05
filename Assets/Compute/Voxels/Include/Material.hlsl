#ifndef TUNTENFISCH_VOXELS_MATERIAL
#define TUNTENFISCH_VOXELS_MATERIAL

#include "Assets/Compute/Include/Enumeration.hlsl"

ENUM MaterialIndex
{
    static const uint Dirt = 0;
    static const uint Rock = 1;
    static const uint Sand = 2;
    static const uint Grass = 3;
};

static const uint numberOfMaterials = 4;

#endif
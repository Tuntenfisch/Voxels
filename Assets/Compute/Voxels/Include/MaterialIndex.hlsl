#ifndef TUNTENFISCH_VOXELS_MATERIAL_INDEX
#define TUNTENFISCH_VOXELS_MATERIAL_INDEX

#include "Assets/Compute/Include/Enumeration.hlsl"

ENUM MaterialIndex
{
    static const uint Air = 0;
    static const uint Dirt = 1;
    static const uint Rock = 2;
    static const uint Sand = 3;
};

static const uint numberOfMaterials = 4;

#endif
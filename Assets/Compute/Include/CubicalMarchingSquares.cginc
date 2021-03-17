#ifndef CUBICAL_MARCHING_SQUARES
#define CUBICAL_MARCHING_SQUARES

#include "Assets/Compute/Include/Vertex.cginc"

struct Segment
{
    uint4 edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB;
    uint2 sharpFeatureVertexIndices;
};

struct Component
{
    uint4 packetSegmentsIndices[2];
};

void initializeComponent(out Component component)
{
    component.packetSegmentsIndices[0] = 0;
    component.packetSegmentsIndices[1] = 0;
}

static const uint4x4 uint4x4Identity = uint4x4
(
    1, 0, 0, 0,
    0, 1, 0, 0,
    0, 0, 1, 0,
    0, 0, 0, 1
);

//     7---------6
//    /|        /|
//   / |       / |
//  /  |      /  |
// 4---------5   |
// |   |     |   |
// |   3-----|---2
// |  /      |  /
// | /       | /
// |/        |/
// 0---------1
static const uint3 voxelCorners[8] =
{
    uint3(0, 0, 0),
    uint3(1, 0, 0),
    uint3(1, 0, 1),
    uint3(0, 0, 1),
    uint3(0, 1, 0),
    uint3(1, 1, 0),
    uint3(1, 1, 1),
    uint3(0, 1, 1) 
};

//     +----6----+
//    /|        /|
//   7 |       5 |
//  /  11     / 10
// +----4----+   |
// |   |     |   |
// |   +----2|---+
// 8  /      9  /
// | 3       | 1
// |/        |/
// +----0----+
static const uint2 voxelEdges[12] =
{
    uint2(0, 1),
    uint2(1, 2),
    uint2(2, 3),
    uint2(3, 0),
    uint2(4, 5),
    uint2(5, 6),
    uint2(6, 7),
    uint2(7, 4),
    uint2(0, 4),
    uint2(1, 5),
    uint2(2, 6),
    uint2(3, 7)
};

//     +----6----+
//    /|        /|
//   7 |       5 |
//  /  11     / 10
// +----4----+   |
// |   |     |   |
// |   +----2|---+
// 8  /      9  /
// | 3       | 1
// |/        |/
// +----0----+
static const uint4 voxelFaces[6] =
{
    uint4(2, 11, 6, 10),    // rear
    uint4(1, 10, 5,  9),    // right
    uint4(0,  9, 4,  8),    // front
    uint4(3,  8, 7, 11),    // left
    uint4(0,  1, 2,  3),    // bottom
    uint4(4,  5, 6,  7)     // top
};

//     7---------6
//    /|        /|
//   / |       / |
//  /  |      /  |
// 4---------5   |
// |   |     |   |
// |   3-----|---2
// |  /      |  /
// | /       | /
// |/        |/
// 0---------1
static const uint4 voxelFaceSampleIndices[6] = 
{
    uint4(2, 3, 7, 6),  // rear
    uint4(1, 2, 6, 5),  // right
    uint4(0, 1, 5, 4),  // front
    uint4(3, 0, 4, 7),  // left
    uint4(0, 1, 2, 3),  // bottom
    uint4(4, 5, 6, 7)   // top
};

// +----2----+
// |         |
// 3         1
// |         |
// +----0----+
static const uint4 marchingSquaresSegments[16] = 
{
    { uint4(-1, -1, -1, -1) },
    { uint4( 3,  0, -1, -1) },
    { uint4( 0,  1, -1, -1) },
    { uint4( 3,  1, -1, -1) },
    { uint4( 1,  2, -1, -1) },
    { uint4( 3,  2,  1,  0) },   // ambiguous case
    { uint4( 0,  2, -1, -1) },
    { uint4( 3,  2, -1, -1) },
    { uint4( 2,  3, -1, -1) },
    { uint4( 2,  0, -1, -1) },
    { uint4( 3,  0,  2,  1) },   // ambiguous case
    { uint4( 2,  1, -1, -1) },
    { uint4( 1,  3, -1, -1) },
    { uint4( 1,  0, -1, -1) },
    { uint4( 0,  3, -1, -1) },
    { uint4(-1, -1, -1, -1) }
};

static const float3x3 normalToFaceTangentMatrices[6] =
{                         
    float3x3
    (
        0.0, -1.0,  0.0,
        1.0,  0.0,  0.0,
        0.0,  0.0,  0.0
    ),
    float3x3
    (
        0.0,  0.0,  0.0,
        0.0,  0.0, -1.0,
        0.0,  1.0,  0.0
    ),
    float3x3
    (
        0.0, -1.0,  0.0,
        1.0,  0.0,  0.0,
        0.0,  0.0,  0.0
    ),
    float3x3
    (
        0.0,  0.0,  0.0,
        0.0,  0.0, -1.0,
        0.0,  1.0,  0.0
    ),
    float3x3
    (
        0.0,  0.0, -1.0,
        0.0,  0.0,  0.0,
        1.0,  0.0,  0.0
    ),
    float3x3
    (
        0.0,  0.0, -1.0,
        0.0,  0.0,  0.0,
        1.0,  0.0,  0.0
    ),
};

#endif
#ifndef CUBICAL_MARCHING_SQUARES
#define CUBICAL_MARCHING_SQUARES

#include "Assets/Compute/Include/Vertex.cginc"

// >>> Flags
struct Flags
{
    uint buffer;
};

void InitializeBitFlag(out Flags flags)
{
    flags.buffer = 0;
}

bool HasFlag(Flags flags, uint index)
{
    return (flags.buffer >> index) & 1;
}

void ClearFlag(inout Flags flags, uint index)
{
    flags.buffer &= ~(1 << index);
}

void SetFlag(inout Flags flags, uint index)
{
    flags.buffer |= 1 << index;
}
// <<<

static const uint4x4 uint4x4Identity = uint4x4
(
    1, 0, 0, 0,
    0, 1, 0, 0,
    0, 0, 1, 0,
    0, 0, 0, 1
);

// >>> Segment
struct Segment
{
    uint4 edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB;
    uint2 sharpFeatureVertexIndices;
};

void InitializeSegment
(
    out Segment segment,
    uint edgeIndexA, 
    uint edgeVertexIndexA, 
    uint edgeIndexB, 
    uint edgeVertexIndexB,
    uint2 sharpFeatureVertexIndices
)
{
    segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB = uint4(edgeIndexA, edgeVertexIndexA, edgeIndexB, edgeVertexIndexB);
    segment.sharpFeatureVertexIndices = sharpFeatureVertexIndices;
}

uint GetEdgeIndexA(Segment segment)
{
    return segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.x;
}

uint GetEdgeVertexIndexA(Segment segment)
{
    return segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.y;
}

uint GetEdgeIndexB(Segment segment)
{
    return segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.z;
}

uint GetEdgeVertexIndexB(Segment segment)
{
    return segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.w;
}

uint GetSharpFeatureVertexIndexA(Segment segment)
{
    return segment.sharpFeatureVertexIndices.x;
}

uint GetSharpFeatureVertexIndexB(Segment segment)
{
    return segment.sharpFeatureVertexIndices.y;
}

void SwapEdges(inout Segment segment)
{
    Segment oldSegment = segment;

    segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.xy = oldSegment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.zw;
    segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.zw = oldSegment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.xy;
}
// <<<

// >>> Component
struct Component
{
    uint4 packedSegmentIndices[2];
};

void InitializeComponent(out Component component)
{
    component.packedSegmentIndices[0] = 0;
    component.packedSegmentIndices[1] = 0;
}

uint GetPackedSegmentIndex(Component component, uint index)
{
    return component.packedSegmentIndices[index >> 2][index & 3];
}

void SetPackedSegmentIndex(inout uint4 packedSegmentIndices[2], uint index, uint segmentIndex)
{
    packedSegmentIndices[index >> 2] += segmentIndex * uint4x4Identity[index & 3];
}

uint GetComponentLength(Component component)
{
    return component.packedSegmentIndices[1].w;
}

void SetComponentLength(inout Component component, uint length)
{
    component.packedSegmentIndices[1].w = length;
}
// <<<

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

float3 ClampPositionToVoxel(float3 position)
{
    return clamp(position, voxelCorners[0], voxelCorners[6]);
}

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

bool IsOnVoxelEdge(float3 position)
{
    bool3 xyz = position == 0.0 || position == 1.0;

    return xyz.x && (xyz.y || xyz.z) || (xyz.y && xyz.z);
}

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
    { uint4( 3,  2,  1,  0) },  // ambiguous case
    { uint4( 0,  2, -1, -1) },
    { uint4( 3,  2, -1, -1) },
    { uint4( 2,  3, -1, -1) },
    { uint4( 2,  0, -1, -1) },
    { uint4( 3,  0,  2,  1) },  // ambiguous case
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
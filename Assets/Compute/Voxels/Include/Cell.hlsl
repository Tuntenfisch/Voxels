#ifndef CELL__559277903
#define CELL__559277903

//     5---------6
//    /|        /|
//   / |       / |
//  /  |      /  |
// 1---------2
// |   |     |   |
// |   4-----|---7
// |  /      |  /
// | /       | /
// |/        |/
// 0---------3
static const uint3 cellCorners[8] =
{
    uint3(0, 0, 0),
    uint3(0, 1, 0),
    uint3(1, 1, 0),
    uint3(1, 0, 0),
    uint3(0, 0, 1),
    uint3(0, 1, 1),
    uint3(1, 1, 1),
    uint3(1, 0, 1)
};

bool IsOutsideCell(float3 position, float epsilon = 0.0f)
{
    float3 min = (float3) (cellCorners[0] - epsilon);
    float3 max = (float3) (cellCorners[6] + epsilon);
    
    return any(position < min || position > max);
}

float3 ClampToCell(float3 position, float epsilon = 0.0f)
{
    float3 min = (float3) (cellCorners[0] - epsilon);
    float3 max = (float3) (cellCorners[6] + epsilon);
    
    return clamp(position, min, max);
}

struct CellEdge
{
    uint2 cornerIndices;

    uint GetCornerStartIndex()
    {
        return cornerIndices.x;
    }
    
    uint GetCornerEndIndex()
    {
        return cornerIndices.y;
    }

    static CellEdge Create(uint cornerStartIndex, uint cornerEndIndex)
    {
        CellEdge cellEdge;
        cellEdge.cornerIndices.x = cornerStartIndex;
        cellEdge.cornerIndices.y = cornerEndIndex;
        
        return cellEdge;
    }
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
static const CellEdge cellEdges[12] =
{
    CellEdge::Create(0, 3),
    CellEdge::Create(3, 7),
    CellEdge::Create(7, 4),
    CellEdge::Create(4, 0),
    CellEdge::Create(1, 2),
    CellEdge::Create(2, 6),
    CellEdge::Create(5, 6),
    CellEdge::Create(5, 1),
    CellEdge::Create(0, 1),
    CellEdge::Create(3, 2),
    CellEdge::Create(7, 6),
    CellEdge::Create(4, 5)
};

static const int farEdgeIndices[3] =
{
    5,
    6,
    10
};

static const int3 vertexIndexOffsets[3][3] =
{
    { int3(1, 0, 0), int3(1, 1, 0), int3(0, 1, 0) },
    { int3(0, 0, 1), int3(0, 1, 1), int3(0, 1, 0) },
    { int3(1, 0, 0), int3(1, 0, 1), int3(0, 0, 1) }
};

#endif
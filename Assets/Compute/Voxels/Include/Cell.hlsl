#ifndef TUNTENFISCH_VOXELS_CELL
#define TUNTENFISCH_VOXELS_CELL

static const uint numberOfCellCorners = 8;

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
static const uint3 cellCorners[numberOfCellCorners] =
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

static const uint numberOfCellEdges = 12;

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
static const CellEdge cellEdges[numberOfCellEdges] =
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

#endif
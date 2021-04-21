#ifndef MARCHING_SQUARES__559277903
#define MARCHING_SQUARES__559277903

// +----2----+
// |         |
// 3         1
// |         |
// +----0----+
static const uint4 marchingSquaresSegments[16] =
{
    uint4(-1, -1, -1, -1),
    uint4(3, 0, -1, -1),
    uint4(0, 1, -1, -1),
    uint4(3, 1, -1, -1),
    uint4(1, 2, -1, -1),
    uint4(3, 0, 1, 2),
    uint4(0, 2, -1, -1),
    uint4(3, 2, -1, -1),
    uint4(2, 3, -1, -1),
    uint4(2, 0, -1, -1),
    uint4(0, 1, 2, 3),
    uint4(2, 1, -1, -1),
    uint4(1, 3, -1, -1),
    uint4(1, 0, -1, -1),
    uint4(0, 3, -1, -1),
    uint4(-1, -1, -1, -1)
};

#endif
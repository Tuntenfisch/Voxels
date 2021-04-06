#ifndef CUBICAL_MARCHING_SQUARES_MARCHING_SQUARES
#define CUBICAL_MARCHING_SQUARES_MARCHING_SQUARES

// +----2----+
// |         |
// 3         1
// |         |
// +----0----+
static const uint4 marchingSquaresSegments[16] =
{
    { uint4(-1, -1, -1, -1) },
    { uint4(3, 0, -1, -1) },
    { uint4(0, 1, -1, -1) },
    { uint4(3, 1, -1, -1) },
    { uint4(1, 2, -1, -1) },
    { uint4(3, 2, 1, 0) },
    { uint4(0, 2, -1, -1) },
    { uint4(3, 2, -1, -1) },
    { uint4(2, 3, -1, -1) },
    { uint4(2, 0, -1, -1) },
    { uint4(3, 0, 2, 1) },
    { uint4(2, 1, -1, -1) },
    { uint4(1, 3, -1, -1) },
    { uint4(1, 0, -1, -1) },
    { uint4(0, 3, -1, -1) },
    { uint4(-1, -1, -1, -1) }
};

#endif
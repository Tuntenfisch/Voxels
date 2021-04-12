#ifndef CUBICAL_MARCHING_SQUARES_MARCHING_SQUARES
#define CUBICAL_MARCHING_SQUARES_MARCHING_SQUARES

// +----2----+
// |         |
// 3         1
// |         |
// +----0----+
//
// The order of the segments for each case matters and is taken
// from https://gist.github.com/TheCyberBrick/f798c0d79cf207cdbbf3033332e9a909.
static const uint4 marchingSquaresSegments[16] = {
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
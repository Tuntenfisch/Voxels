#ifndef CUBICAL_MARCHING_SQUARES_VOXEL
#define CUBICAL_MARCHING_SQUARES_VOXEL

static const uint voxelCornersCount = 8;

//     7---------6
//    /|        /|
//   / |       / |
//  /  |      /  |
// 3---------2   |
// |   |     |   |
// |   4-----|---5
// |  /      |  /
// | /       | /
// |/        |/
// 0---------1
static const uint3 voxelCorners[voxelCornersCount] = {
    uint3(0, 0, 0),
    uint3(1, 0, 0),
    uint3(1, 1, 0),
    uint3(0, 1, 0),
    uint3(0, 0, 1),
    uint3(1, 0, 1),
    uint3(1, 1, 1),
    uint3(0, 1, 1)
};

bool IsOutsideVoxel(float3 voxelSpacePosition, float epsilon = 0.0f)
{
    float3 min = (float3) (voxelCorners[0] - epsilon);
    float3 max = (float3) (voxelCorners[6] + epsilon);
    
    return any(voxelSpacePosition < min || voxelSpacePosition > max);
}

float3 ClampToVoxel(float3 voxelSpacePosition, float epsilon = 0.0f)
{
    float3 min = (float3) (voxelCorners[0] - epsilon);
    float3 max = (float3) (voxelCorners[6] + epsilon);
    
    return clamp(voxelSpacePosition, min, max);
}

struct VoxelEdge
{
    uint2 voxelCornerIndices;

    uint GetVoxelCornerStartIndex()
    {
        return voxelCornerIndices.x;
    }
    
    uint GetVoxelCornerEndIndex()
    {
        return voxelCornerIndices.y;
    }

    static VoxelEdge Create(uint voxelCornerStartIndex, uint voxelCornerEndIndex)
    {
        VoxelEdge voxelEdge;
        voxelEdge.voxelCornerIndices.x = voxelCornerStartIndex;
        voxelEdge.voxelCornerIndices.y = voxelCornerEndIndex;
        
        return voxelEdge;
    }
};

//     +----6----+
//    /|        /|
//   11|      10 |
//  /  7      /  5
// +----2----+   |
// |   |     |   |
// |   +----4|---+
// 3  /      1  /
// | 8       | 9
// |/        |/
// +----0----+
static const VoxelEdge voxelEdges[12] = {
    VoxelEdge::Create(0, 1),
    VoxelEdge::Create(1, 2),
    VoxelEdge::Create(2, 3),
    VoxelEdge::Create(3, 0),
    VoxelEdge::Create(4, 5),
    VoxelEdge::Create(5, 6),
    VoxelEdge::Create(6, 7),
    VoxelEdge::Create(7, 4),
    VoxelEdge::Create(0, 4),
    VoxelEdge::Create(1, 5),
    VoxelEdge::Create(2, 6),
    VoxelEdge::Create(3, 7)
};

static const uint voxelFacesCount = 6;

struct VoxelFace
{
    uint4 voxelEdgeIndices;
    uint4 voxelCornerIndices;
    float3x3 normalToFaceTangentMatrix;
    
    float3 GetFaceTangent(float3 normal)
    {
        const float epsilon = 1e-4f;
        
        float3 tangent = mul(normalToFaceTangentMatrix, normal);
        
        // Ensure that the tangent isn't zero length. This can happen when
        // the normal coincides with the voxel face's plane normal.
        if (dot(tangent, tangent) < epsilon)
        {
            tangent += mul(normalToFaceTangentMatrix, float3(epsilon, epsilon, epsilon));
        }
        
        return tangent;
    }

    static VoxelFace Create(uint4 voxelEdgeIndices, uint4 voxelCornerIndices, float3x3 normalToFaceTangentMatrix)
    {
        VoxelFace voxelFace;
        voxelFace.voxelEdgeIndices = voxelEdgeIndices;
        voxelFace.voxelCornerIndices = voxelCornerIndices;
        voxelFace.normalToFaceTangentMatrix = normalToFaceTangentMatrix;
        
        return voxelFace;
    }
};

static const VoxelFace voxelFaces[voxelFacesCount] = {
    VoxelFace::Create(uint4(0, 1, 2, 3), uint4(0, 1, 2, 3), float3x3(0.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)),
    VoxelFace::Create(uint4(9, 5, 10, 1), uint4(1, 5, 6, 2), float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f)),
    VoxelFace::Create(uint4(4, 7, 6, 5), uint4(5, 4, 7, 6), float3x3(0.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)),
    VoxelFace::Create(uint4(8, 3, 11, 7), uint4(4, 0, 3, 7), float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f)),
    VoxelFace::Create(uint4(4, 9, 0, 8), uint4(4, 5, 1, 0), float3x3(0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)),
    VoxelFace::Create(uint4(2, 10, 6, 11), uint4(3, 2, 6, 7), float3x3(0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)),
};

// Note: The order of the elements in voxelCorners, voxelEdges and voxelFaces (as well as marchingSquaresSegments) is important.
// Initially I had an arbitrary order for each of those and it resulted in some triangle faces facing the wrong way.  One way
// to solve this would be to render both faces (front and back) but that obviously decreases performance. I eventually found a
// CMS snippet here https://gist.github.com/TheCyberBrick/f798c0d79cf207cdbbf3033332e9a909 and used its specific ordering for
// the respective arrays. It worked fine initially but occasionally one triangle would still be rendered facing wrong.
//
// I didn't really want to wrap my head around what the correct order is myself. To be honest I don't even know if there is an
// order (for my arrays) resulting in all triangles rendered always facing outwards of the volume they are supposed to enclose.
//
// In the original CMS paper (https://www.csie.ntu.edu.tw/~cyy/publications/papers/Ho2005CMS.pdf) they mention that they
// provided the implementation as an open soruce library here http://graphics.csie.ntu.edu.tw/CMS/. But the link
// is no longer accessible...
//
// Well, it turns out that a snapshots of that website exist on https://web.archive.org/. The exact snapshot doesn't matter
// too much (I think). Anyways all the way on the bottom of one of the snapshots is a download link to a "preliminary version".
// After adapting my arrays to the ordering found in the source code of that version it seems to work fine. I hope this is the end of it...

#endif
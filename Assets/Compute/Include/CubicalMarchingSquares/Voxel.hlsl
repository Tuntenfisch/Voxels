#ifndef CUBICAL_MARCHING_SQUARES_VOXEL

    #define CUBICAL_MARCHING_SQUARES_VOXEL

    static const uint voxelCornersCount = 8;

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
    static const uint3 voxelCorners[voxelCornersCount] = {
        uint3(0, 0, 0),
        uint3(1, 0, 0),
        uint3(1, 0, 1),
        uint3(0, 0, 1),
        uint3(0, 1, 0),
        uint3(1, 1, 0),
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
    };

    VoxelEdge VoxelEdgeConstructor(uint voxelCornerStartIndex, uint voxelCornerEndIndex)
    {
        VoxelEdge voxelEdge;
        voxelEdge.voxelCornerIndices.x = voxelCornerStartIndex;
        voxelEdge.voxelCornerIndices.y = voxelCornerEndIndex;
        
        return voxelEdge;
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
    static const VoxelEdge voxelEdges[12] = {
        VoxelEdgeConstructor(0, 1),
        VoxelEdgeConstructor(1, 2),
        VoxelEdgeConstructor(2, 3),
        VoxelEdgeConstructor(3, 0),
        VoxelEdgeConstructor(4, 5),
        VoxelEdgeConstructor(5, 6),
        VoxelEdgeConstructor(6, 7),
        VoxelEdgeConstructor(7, 4),
        VoxelEdgeConstructor(0, 4),
        VoxelEdgeConstructor(1, 5),
        VoxelEdgeConstructor(2, 6),
        VoxelEdgeConstructor(3, 7)
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
    };

    VoxelFace VoxelFaceConstructor(uint4 voxelEdgeIndices, uint4 voxelCornerIndices, float3x3 normalToFaceTangentMatrix)
    {
        VoxelFace voxelFace;
        voxelFace.voxelEdgeIndices = voxelEdgeIndices;
        voxelFace.voxelCornerIndices = voxelCornerIndices;
        voxelFace.normalToFaceTangentMatrix = normalToFaceTangentMatrix;
        
        return voxelFace;
    }

    // The order of the faces matters and is taken from https://gist.github.com/TheCyberBrick/f798c0d79cf207cdbbf3033332e9a909.
    static const VoxelFace voxelFaces[voxelFacesCount] = {
        VoxelFaceConstructor(uint4(0, 9, 4, 8), uint4(0, 1, 5, 4), float3x3(0.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)), // Front face (xy plane, z = 0)
        VoxelFaceConstructor(uint4(1, 10, 5, 9), uint4(1, 2, 6, 5), float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f)), // Right face (yz plane, x = 1)
        VoxelFaceConstructor(uint4(2, 11, 6, 10), uint4(2, 3, 7, 6), float3x3(0.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)), // Rear face (xy plane, z = 1)
        VoxelFaceConstructor(uint4(3, 8, 7, 11), uint4(3, 0, 4, 7), float3x3(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f)), // Left face (yz plane, x = 0)
        VoxelFaceConstructor(uint4(2, 1, 0, 3), uint4(3, 2, 1, 0), float3x3(0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)), // Bottom face (xz plane, y = 0)
        VoxelFaceConstructor(uint4(4, 5, 6, 7), uint4(4, 5, 6, 7), float3x3(0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)), // Top face (xz plane, y = 1)
    };

#endif
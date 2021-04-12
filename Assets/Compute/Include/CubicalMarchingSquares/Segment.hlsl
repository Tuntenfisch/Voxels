#ifndef CUBICAL_MARCHING_SQUARES_SEGMENT

    #define CUBICAL_MARCHING_SQUARES_SEGMENT

    static const uint maxSegmentsCount = 12;

    struct Segment
    {
        uint4 edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB;
        uint2 sharpFeatureVertexIndices;
        
        uint GetEdgeIndexA()
        {
            return edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.x;
        }

        uint GetEdgeVertexIndexA()
        {
            return edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.y;
        }

        uint GetEdgeIndexB()
        {
            return edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.z;
        }

        uint GetEdgeVertexIndexB()
        {
            return edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.w;
        }

        uint GetSharpFeatureVertexIndexA()
        {
            return sharpFeatureVertexIndices.x;
        }

        uint GetSharpFeatureVertexIndexB()
        {
            return sharpFeatureVertexIndices.y;
        }
        
        bool HasSharpFeature()
        {
            return sharpFeatureVertexIndices.x != -1;
        }

        void SwapEdges()
        {
            edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB = edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB.zwxy;
        }

        static Segment Create(uint edgeIndexA, uint edgeVertexIndexA, uint edgeIndexB, uint edgeVertexIndexB, uint2 sharpFeatureVertexIndices)
        {
            Segment segment;
            segment.edgeIndexA_edgeVertexIndexA_edgeIndexB_edgeVertexIndexB = uint4(edgeIndexA, edgeVertexIndexA, edgeIndexB, edgeVertexIndexB);
            segment.sharpFeatureVertexIndices = sharpFeatureVertexIndices;
            
            return segment;
        }
    };

    

#endif
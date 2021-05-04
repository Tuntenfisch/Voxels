#ifndef COMPONENT__559277903
#define COMPONENT__559277903

static const uint maxComponentsCount = 4;

struct Component
{
    uint4 packedSegmentsIndices[2];
    
    uint GetPackedSegmentsIndex(uint index)
    {
        return packedSegmentsIndices[index >> 2][index & 3];
    }

    void SetPackedSegmentsIndex(uint index, uint segmentsIndex)
    {
        const uint4x4 uint4x4Identity = uint4x4
        (
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        packedSegmentsIndices[0] += (index >> 2) == 0 ? segmentsIndex * uint4x4Identity[index & 3] : 0;
        packedSegmentsIndices[1] += (index >> 2) == 1 ? segmentsIndex * uint4x4Identity[index & 3] : 0;
    }

    uint GetLength()
    {
        return packedSegmentsIndices[1].w;
    }

    void SetLength(uint length)
    {
        packedSegmentsIndices[1].w = length;
    }

    static Component Create()
    {
        Component component;
        component.packedSegmentsIndices[0] = 0;
        component.packedSegmentsIndices[1] = 0;
        
        return component;
    }
};

#endif
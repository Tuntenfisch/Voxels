#ifndef CUBICAL_MARCHING_SQUARES_FLAGS
#define CUBICAL_MARCHING_SQUARES_FLAGS

struct Flags
{
    uint buffer;
    
    bool HasFlag(uint index)
    {
        return (buffer >> index) & 1;
    }
    
    void ClearFlag(uint index)
    {
        buffer &= ~(1 << index);
    }
    
    void SetFlag(uint index)
    {
        buffer |= 1 << index;
    }
};

Flags BitFlagConstructor()
{
    Flags flags;
    flags.buffer = 0;
    
    return flags;
}

#endif
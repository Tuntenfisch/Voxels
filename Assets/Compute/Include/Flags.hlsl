#ifndef TUNTENFISCH_FLAGS
#define TUNTENFISCH_FLAGS

struct Flags
{
    uint value;
    
    bool HasFlag(uint index)
    {
        return(value >> index) & 1;
    }
    
    void ClearFlag(uint index)
    {
        value &= ~(1 << index);
    }
    
    void SetFlag(uint index)
    {
        value |= 1 << index;
    }

    static Flags Create(uint value = 0)
    {
        Flags flags;
        flags.value = value;
        
        return flags;
    }
};

#endif
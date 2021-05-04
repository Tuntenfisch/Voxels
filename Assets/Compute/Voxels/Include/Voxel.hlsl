#ifndef VOXEL__559277903
#define VOXEL__559277903

struct Voxel
{
    float4 value_gradient;
    
    float GetValue()
    {
        return value_gradient.x;
    }
    
    float3 GetGradient()
    {
        return value_gradient.yzw;
    }

    static Voxel Create(float value, float3 gradient)
    {
        Voxel voxel;
        voxel.value_gradient.x = value;
        voxel.value_gradient.yzw = gradient;

        return voxel;
    }
};

#endif
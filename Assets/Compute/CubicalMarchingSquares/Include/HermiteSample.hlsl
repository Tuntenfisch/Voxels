#ifndef HERMITE_SAMPLE__559277903
#define HERMITE_SAMPLE__559277903

struct HermiteSample
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

    static HermiteSample Create(float value, float3 gradient)
    {
        HermiteSample sample;
        sample.value_gradient.x = value;
        sample.value_gradient.yzw = gradient;

        return sample;
    }
};



#endif
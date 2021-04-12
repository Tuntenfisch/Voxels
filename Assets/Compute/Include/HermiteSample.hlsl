#ifndef HERMITE_SAMPLE
#define HERMITE_SAMPLE

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
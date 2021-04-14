#ifndef VERTEX__559277903
#define VERTEX__559277903

struct Vertex
{
    float3 position;
    float3 normal;

    static Vertex Create(float3 position = 0.0f, float3 normal = 0.0f)
    {
        Vertex vertex;
        vertex.position = position;
        vertex.normal = normal;

        return vertex;
    }
};

#endif
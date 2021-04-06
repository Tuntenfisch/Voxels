#ifndef VERTEX
#define VERTEX

struct Vertex
{
    float3 position;
    float3 normal;
};

Vertex VertexConstructor(float3 position, float3 normal)
{
    Vertex vertex;
    
    vertex.position = position;
    vertex.normal = normal;
    
    return vertex;
}

#endif
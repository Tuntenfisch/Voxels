#ifndef VERTEX

    #define VERTEX

    struct Vertex
    {
        float3 position;
        float3 normal;
    };

    Vertex VertexConstructor(float3 position = 0.0f, float3 normal = 0.0f)
    {
        Vertex vertex;
        vertex.position = position;
        vertex.normal = normal;
        
        return vertex;
    }

#endif
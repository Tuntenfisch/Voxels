using Unity.Collections;

namespace Tuntenfisch.Voxels.DC
{
    public delegate void OnMeshGenerated(int vertexCount, int triangleCount, NativeArray<GPUVertex> vertices, NativeArray<int> triangles);
}
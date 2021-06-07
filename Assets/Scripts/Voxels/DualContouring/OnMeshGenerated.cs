using Unity.Collections;

namespace Tuntenfisch.Voxels.DualContouring
{
    public delegate void OnMeshGenerated(int vertexCount, int triangleCount, NativeArray<GPUVertex> vertices, NativeArray<int> triangles);
}
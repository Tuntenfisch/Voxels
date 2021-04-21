using Unity.Collections;
using UnityEngine;

namespace Voxel
{
    public interface IMeshGenerationRequester
    {
        public (ComputeBuffer voxelVolumeBuffer, Vector3 worldPosition) GetMeshGenerationArguments();

        public void OnMeshGenerated(NativeArray<Vertex>? nullableVertices, NativeArray<int>? nullableTriangles);
    }
}
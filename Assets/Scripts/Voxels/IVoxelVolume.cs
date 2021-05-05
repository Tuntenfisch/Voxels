using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels
{
    public interface IVoxelVolume
    {
        public (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing, bool respectSharpFeatures) GetArguments();

        public void OnMeshGenerated(NativeArray<Vertex>? nullableVertices, NativeArray<int>? nullableTriangles);
    }
}
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels
{
    public interface IVoxelVolume
    {
        internal (ComputeBuffer voxelVolumeBuffer, float3 worldPosition, float voxelSpacing) GetArguments();

        internal void OnMeshGenerated(NativeArray<Vertex>? nullableVertices, NativeArray<int>? nullableTriangles);
    }
}
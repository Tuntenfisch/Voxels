using UnityEngine;

namespace Tuntenfisch.Voxels
{
    public static class ComputeShaderProperties
    {
        public static readonly int CellStride = Shader.PropertyToID("cellStride");
        public static readonly int CosOfSharpFeatureAngle = Shader.PropertyToID("cosOfSharpFeatureAngle");
        public static readonly int GeneratedVerticesIndexLookupTable = Shader.PropertyToID("generatedVerticesIndexLookupTable");
        public static readonly int GeneratedTriangles = Shader.PropertyToID("generatedTriangles");
        public static readonly int GeneratedVertices0 = Shader.PropertyToID("generatedVertices0");
        public static readonly int GeneratedVertices1 = Shader.PropertyToID("generatedVertices1");
        public static readonly int NoiseGraphNodes = Shader.PropertyToID("noiseGraphNodes");
        public static readonly int NumberOfNoiseGraphNoiseNodes = Shader.PropertyToID("numberOfNoiseGraphNoiseNodes");
        public static readonly int NumberOfVoxels = Shader.PropertyToID("numberOfVoxels");
        public static readonly int NumberOfVoxelVolumeCSGOperations = Shader.PropertyToID("numberOfVoxelVolumeCSGOperations");
        public static readonly int SchmitzParticleIterations = Shader.PropertyToID("schmitzParticleIterations");
        public static readonly int SchmitzParticleStepSize = Shader.PropertyToID("schmitzParticleStepSize");
        public static readonly int SubSampledCellVolumeFaces = Shader.PropertyToID("subSampledCellVolumeFaces");
        public static readonly int VoxelSpacing = Shader.PropertyToID("voxelSpacing");
        public static readonly int VoxelVolume = Shader.PropertyToID("voxelVolume");
        public static readonly int VoxelVolumeCSGOperations = Shader.PropertyToID("voxelVolumeCSGOperations");
        public static readonly int VoxelVolumeToWorldSpaceOffset = Shader.PropertyToID("voxelVolumeToWorldSpaceOffset");
    }
}
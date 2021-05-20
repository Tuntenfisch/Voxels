using UnityEngine;

namespace Tuntenfisch.Voxels
{
    internal static class ComputeShaderProperties
    {
        public static readonly int CellStride = Shader.PropertyToID("cellStride");
        public static readonly int CosOfSharpFeatureAngle = Shader.PropertyToID("cosOfSharpFeatureAngle");
        public static readonly int GeneratedVertexIndicesLookupTable = Shader.PropertyToID("generatedVertexIndicesLookupTable");
        public static readonly int GeneratedTriangles = Shader.PropertyToID("generatedTriangles");
        public static readonly int GeneratedVertices = Shader.PropertyToID("generatedVertices");
        public static readonly int Height = Shader.PropertyToID("height");
        public static readonly int Lacunarity = Shader.PropertyToID("lacunarity");
        public static readonly int NumberOfOctaves = Shader.PropertyToID("numberOfOctaves");
        public static readonly int NumberOfVoxels = Shader.PropertyToID("numberOfVoxels");
        public static readonly int Persistence = Shader.PropertyToID("persistence");
        public static readonly int SchmitzParticleIterations = Shader.PropertyToID("schmitzParticleIterations");
        public static readonly int SchmitzParticleStepSize = Shader.PropertyToID("schmitzParticleStepSize");
        public static readonly int Seed = Shader.PropertyToID("seed");
        public static readonly int SubSampledCellVolumeFaces = Shader.PropertyToID("subSampledCellVolumeFaces");
        public static readonly int VoxelSpacing = Shader.PropertyToID("voxelSpacing");
        public static readonly int VoxelVolume = Shader.PropertyToID("voxelVolume");
        public static readonly int VoxelVolumeToWorldOffset = Shader.PropertyToID("voxelVolumeToWorldOffset");
        public static readonly int Wavelength = Shader.PropertyToID("wavelength");
    }
}
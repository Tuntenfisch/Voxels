using UnityEngine;

namespace Voxels
{
    internal static class ComputeShaderProperties
    {
        public static readonly int s_cosOfSharpFeatureAngle = Shader.PropertyToID("cosOfSharpFeatureAngle");
        public static readonly int s_flatFeatureVertexIndicesLookupTable = Shader.PropertyToID("flatFeatureVertexIndicesLookupTable");
        public static readonly int s_generatedTriangles = Shader.PropertyToID("generatedTriangles");
        public static readonly int s_generatedVertices = Shader.PropertyToID("generatedVertices");
        public static readonly int s_height = Shader.PropertyToID("height");
        public static readonly int s_lacunarity = Shader.PropertyToID("lacunarity");
        public static readonly int s_maxIterations = Shader.PropertyToID("maxIterations");
        public static readonly int s_numberOfOctaves = Shader.PropertyToID("numberOfOctaves");
        public static readonly int s_persistence = Shader.PropertyToID("persistence");
        public static readonly int s_seed = Shader.PropertyToID("seed");
        public static readonly int s_stepSize = Shader.PropertyToID("stepSize");
        public static readonly int s_subSampledCellVolumeFaces = Shader.PropertyToID("subSampledCellVolumeFaces");
        public static readonly int s_voxelSpacing = Shader.PropertyToID("voxelSpacing");
        public static readonly int s_voxelVolume = Shader.PropertyToID("voxelVolume");
        public static readonly int s_voxelVolumeCount = Shader.PropertyToID("voxelVolumeCount");
        public static readonly int s_voxelVolumeToWorldOffset = Shader.PropertyToID("voxelVolumeToWorldOffset");
        public static readonly int s_wavelength = Shader.PropertyToID("wavelength");
    }
}
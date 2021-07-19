using UnityEngine;

namespace Tuntenfisch.Voxels
{
    public static class ShaderProperties
    {
        public static readonly int CosOfHalfSharpFeatureAngle = Shader.PropertyToID("cosOfHalfSharpFeatureAngle");
        public static readonly int MaterialAlbedoTextures = Shader.PropertyToID("materialAlbedoTextures");
        public static readonly int MaterialMOHSTextures = Shader.PropertyToID("materialMOHSTextures");
        public static readonly int MaterialNormalTextures = Shader.PropertyToID("materialNormalTextures");
    }
}
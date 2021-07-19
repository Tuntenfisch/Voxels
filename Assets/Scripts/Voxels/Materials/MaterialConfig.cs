using System;
using System.Collections.Generic;
using Tuntenfisch.Generics;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [CreateAssetMenu(fileName = "Material Config", menuName = "Voxels/Material Config")]
    public class MaterialConfig : ManagedScriptableObject
    {
        public event Action OnDirtied;
        public event Action OnLateDirtied;

        public Material RenderMaterial => m_renderMaterial;
        public List<MaterialInfo> MaterialInfos => m_materialInfos;

        [SerializeField]
        private Material m_renderMaterial;
        [SerializeField]
        private List<MaterialInfo> m_materialInfos;

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ApplyMaterialConfig();

            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
        }

        protected override void OnScriptableObjectAwake() => ApplyMaterialConfig();

        protected override void OnScriptableObjectDestroy() { }

        private void ApplyMaterialConfig()
        {
            // The dimensions should be the same across all types of textures.
            int width = MaterialInfos[0].AlbedoTexture.width;
            int height = MaterialInfos[0].AlbedoTexture.height;

            Texture2DArray materialAlbedoTextures = new Texture2DArray(width, height, MaterialInfos.Count, MaterialInfos[0].AlbedoTexture.format, true);
            Texture2DArray materialNormalTextures = new Texture2DArray(width, height, MaterialInfos.Count, MaterialInfos[0].NormalTexture.format, true);
            Texture2DArray materialMOHSTextures = new Texture2DArray(width, height, MaterialInfos.Count, MaterialInfos[0].MOHSTexture.format, true);

            for (int index = 0; index < MaterialInfos.Count; index++)
            {
                Graphics.CopyTexture(MaterialInfos[index].AlbedoTexture, 0, materialAlbedoTextures, index);
                Graphics.CopyTexture(MaterialInfos[index].NormalTexture, 0, materialNormalTextures, index);
                Graphics.CopyTexture(MaterialInfos[index].MOHSTexture, 0, materialMOHSTextures, index);
            }
            materialAlbedoTextures.Apply(false);
            materialNormalTextures.Apply(false);
            materialMOHSTextures.Apply(false);

            Shader.SetGlobalTexture(ShaderProperties.MaterialAlbedoTextures, materialAlbedoTextures);
            Shader.SetGlobalTexture(ShaderProperties.MaterialNormalTextures, materialNormalTextures);
            Shader.SetGlobalTexture(ShaderProperties.MaterialMOHSTextures, materialMOHSTextures);
        }
    }
}
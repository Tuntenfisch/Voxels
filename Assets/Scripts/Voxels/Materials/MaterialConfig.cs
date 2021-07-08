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

            Texture2DArray materialAlbedoTextureArray = new Texture2DArray(width, height, MaterialInfos.Count, MaterialInfos[0].AlbedoTexture.format, true);
            Texture2DArray materialNormalTextureArray = new Texture2DArray(width, height, MaterialInfos.Count, MaterialInfos[0].NormalTexture.format, true);
            Texture2DArray materialMOHSTextureArray = new Texture2DArray(width, height, MaterialInfos.Count, MaterialInfos[0].MOHSTexture.format, true);

            for (int index = 0; index < MaterialInfos.Count; index++)
            {
                Texture2D materialAlbedoTexture = MaterialInfos[index].AlbedoTexture;
                Texture2D materialNormalTexture = MaterialInfos[index].NormalTexture;
                Texture2D materialMOHSTexture = MaterialInfos[index].MOHSTexture;

                Graphics.CopyTexture(materialAlbedoTexture, 0, materialAlbedoTextureArray, index);
                Graphics.CopyTexture(materialNormalTexture, 0, materialNormalTextureArray, index);
                Graphics.CopyTexture(materialMOHSTexture, 0, materialMOHSTextureArray, index);
            }
            materialAlbedoTextureArray.Apply(false);
            materialNormalTextureArray.Apply(false);
            materialMOHSTextureArray.Apply(false);

            Shader.SetGlobalTexture(nameof(materialAlbedoTextureArray), materialAlbedoTextureArray);
            Shader.SetGlobalTexture(nameof(materialNormalTextureArray), materialNormalTextureArray);
            Shader.SetGlobalTexture(nameof(materialMOHSTextureArray), materialMOHSTextureArray);
        }
    }
}
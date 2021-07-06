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
            ApplyMaterialConfig();

            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
        }

        protected override void OnScriptableObjectAwake() => ApplyMaterialConfig();

        protected override void OnScriptableObjectDestroy() { }

        private void ApplyMaterialConfig()
        {
            int width = MaterialInfos[0].Albedo.width;
            int height = MaterialInfos[0].Albedo.height;
            Texture2DArray materialAlbedosTextureArray = new Texture2DArray(width, height, MaterialInfos.Count, TextureFormat.RGBA32, true);

            for (int index = 0; index < MaterialInfos.Count; index++)
            {
                Texture2D materialAlbedoTexture = MaterialInfos[index].Albedo;
                Graphics.CopyTexture(materialAlbedoTexture, 0, materialAlbedosTextureArray, index);
            }
            materialAlbedosTextureArray.Apply();
            Shader.SetGlobalTexture(nameof(materialAlbedosTextureArray), materialAlbedosTextureArray);
        }
    }
}
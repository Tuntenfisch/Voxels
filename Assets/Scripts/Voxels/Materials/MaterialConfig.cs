using System;
using System.Collections.Generic;
using Tuntenfisch.Generics;
using UnityEngine;
using UnityEngine.Assertions;

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
            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
        }

        protected override void OnScriptableObjectAwake()
        {
            OnDirtied += ApplyMaterialAlbedos;
            ApplyMaterialAlbedos();
        }

        protected override void OnScriptableObjectDestroy() => OnDirtied -= ApplyMaterialAlbedos;

        private void ApplyMaterialAlbedos()
        {
            int width = m_materialInfos[0].Albedo.width;
            int height = m_materialInfos[0].Albedo.height;
            Texture2DArray materialAlbedosTextureArray = new Texture2DArray(width, height, m_materialInfos.Count, TextureFormat.RGBA32, true);
            
            for (int index = 0; index < m_materialInfos.Count; index++)
            {
                Texture2D materialAlbedoTexture = m_materialInfos[index].Albedo;
                Graphics.CopyTexture(materialAlbedoTexture, 0, materialAlbedosTextureArray, index);
            }
            materialAlbedosTextureArray.Apply();
            Shader.SetGlobalTexture(nameof(materialAlbedosTextureArray), materialAlbedosTextureArray);
        }
    }
}
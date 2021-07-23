using System;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Generics;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
            Shader.SetGlobalTexture(ShaderProperties.MaterialAlbedoTextures, CreateTexture2DArray(MaterialInfos.Select((MaterialInfo materialInfo) => materialInfo.AlbedoTexture)));
            Shader.SetGlobalTexture(ShaderProperties.MaterialNormalTextures, CreateTexture2DArray(MaterialInfos.Select((MaterialInfo materialInfo) => materialInfo.NormalTexture)));
            Shader.SetGlobalTexture(ShaderProperties.MaterialMOHSTextures, CreateTexture2DArray(MaterialInfos.Select((MaterialInfo materialInfo) => materialInfo.MOHSTexture)));
        }

        private Texture2DArray CreateTexture2DArray(IEnumerable<Texture2D> textures)
        {
            // The dimensions should be the same across all types of textures.
            Texture2DArray textureArray = new Texture2DArray(textures.First().width, textures.First().height, MaterialInfos.Count, textures.First().format, true);

            foreach ((Texture2D texture, int index) iterator in textures.Select((texture, index) => (texture, index)))
            {
#if UNITY_EDITOR
                if (!iterator.texture.isReadable)
                {
                    Debug.LogWarning($"Please enable read/write for \"{AssetDatabase.GetAssetPath(iterator.texture)}\"! Otherwise material visuals won't display properly in standalone build.");
                }
#endif
                Graphics.CopyTexture(iterator.texture, 0, textureArray, iterator.index);
            }
            textureArray.Apply(false);

            return textureArray;
        }
    }
}
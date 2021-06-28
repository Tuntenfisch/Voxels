using System;
using System.Collections.Generic;
using System.Linq;
using Tuntenfisch.Generics;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [CreateAssetMenu(fileName = "Material Config", menuName = "Voxels/Material Config")]
    public class MaterialConfig : ManagedScriptableObject
    {
        public event Action OnDirtied;
        public event Action OnLateDirtied;

        public Material RenderMaterial => m_renderMaterial;
        public List<GPUMaterialInfo> MaterialInfos => m_materialInfos;

        [SerializeField]
        private Material m_renderMaterial;
        [SerializeField]
        private List<GPUMaterialInfo> m_materialInfos;

        private void OnValidate()
        {
            OnDirtied?.Invoke();
            OnLateDirtied?.Invoke();
        }

        protected override void OnScriptableObjectAwake()
        {
            OnDirtied += ApplyMaterialColors;
            ApplyMaterialColors();
        }

        protected override void OnScriptableObjectDestroy() => OnDirtied -= ApplyMaterialColors;

        private void ApplyMaterialColors()
        {
            Color32[] materialColors = (from materialInfo in m_materialInfos select (Color32)materialInfo.Color).ToArray();
            Texture2D materialColorsLookupTexture = new Texture2D(materialColors.Length, 1);
            materialColorsLookupTexture.SetPixels32(materialColors);
            ((Texture2D)m_renderMaterial.GetTexture(nameof(materialColorsLookupTexture))).LoadImage(materialColorsLookupTexture.EncodeToPNG());
        }
    }
}
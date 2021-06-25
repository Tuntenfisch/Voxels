using System;
using System.Linq;
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
            IEnumerable<Color> materialColors = from materialInfo in m_materialInfos select materialInfo.Color;
            m_renderMaterial.SetColorArray(nameof(materialColors), materialColors.ToArray());
        }
    }
}
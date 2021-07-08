using System;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [Serializable]
    public class MaterialInfo
    {
        public Texture2D AlbedoTexture => m_albedoTexture;
        public Texture2D NormalTexture => m_normalTexture;
        public Texture2D MOHSTexture => m_mohsTexture;

        [SerializeField]
        private Texture2D m_albedoTexture;
        [SerializeField]
        private Texture2D m_normalTexture;
        [SerializeField]
        private Texture2D m_mohsTexture;
    }
}
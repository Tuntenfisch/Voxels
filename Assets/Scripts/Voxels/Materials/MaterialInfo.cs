﻿using System;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials
{
    [Serializable]
    public class MaterialInfo
    {
        public Texture2D Albedo => m_albedo;

        [SerializeField]
        private Texture2D m_albedo;
    }
}
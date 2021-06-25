using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUCSGOperator
    {
        public static int SizeInBytes => s_sizeInBytes;

        public CSGOperatorIndex OperatorIndex { get => m_operatorIndex; set => m_operatorIndex = value; }
        public float Smoothing { get => m_smoothing; set => m_smoothing = value; }

        private readonly static int s_sizeInBytes = Marshal.SizeOf<GPUCSGOperator>();

        [SerializeField]
        private CSGOperatorIndex m_operatorIndex;
        [SerializeField]
        private float m_smoothing;

        public GPUCSGOperator(CSGOperatorIndex operatorIndex, float smoothing = 0.0f)
        {
            m_operatorIndex = operatorIndex;
            m_smoothing = smoothing;
        }
    }
}
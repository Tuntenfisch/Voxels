using UnityEngine;

namespace Utils
{
    public static class ComputeShaderUtil
    {
        public static void Dispatch(this ComputeShader computeShader, int kernelID, Vector3Int xyz)
        {
            computeShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out uint threadGroupSizeZ);

            computeShader.Dispatch
            (
                kernelID,
                Mathf.CeilToInt(xyz.x / (float)threadGroupSizeX),
                Mathf.CeilToInt(xyz.y / (float)threadGroupSizeY),
                Mathf.CeilToInt(xyz.z / (float)threadGroupSizeZ)
            );
        }
    }
}
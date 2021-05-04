using Unity.Mathematics;
using UnityEngine;

namespace Extensions
{
    public static class ComputeShaderExtensions
    {
        public static void Dispatch(this ComputeShader computeShader, int kernelID, int3 numberOfInvocations)
        {
            uint3 threadGroupSize = new uint3();
            computeShader.GetKernelThreadGroupSizes(kernelID, out threadGroupSize.x, out threadGroupSize.y, out threadGroupSize.z);
            int3 threadGroups = (int3)math.ceil(numberOfInvocations / (float3)threadGroupSize);

            computeShader.Dispatch(kernelID, threadGroups.x, threadGroups.y, threadGroups.z);
        }
    }
}
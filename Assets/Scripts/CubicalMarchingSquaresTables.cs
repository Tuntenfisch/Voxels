using UnityEngine;

public static class CubicalMarchingSquaresTables
{
    // +----2----+
    // |         |
    // 3         1
    // |         |
    // +----0----+
    public static readonly int[][][] s_segments = 
    {
        new int[][] { new int[] { -1, -1 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  3,  0 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  0,  1 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  3,  1 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  1,  2 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  3,  2 }, new int[] {  1,  0 } },   // ambiguous case
        new int[][] { new int[] {  0,  2 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  3,  2 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  2,  3 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  0,  2 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  3,  0 }, new int[] {  2,  1 } },   // ambiguous case
        new int[][] { new int[] {  2,  1 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  1,  3 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  1,  0 }, new int[] { -1, -1 } },
        new int[][] { new int[] {  0,  3 }, new int[] { -1, -1 } },
        new int[][] { new int[] { -1, -1 }, new int[] { -1, -1 } }
    };

    public static readonly Matrix4x4[] s_normalToFaceTangentMatrices =
    {
        new Matrix4x4(new Vector4(0.0f, 1.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, -1.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 1.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, -1.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
        new Matrix4x4(new Vector4(0.0f, 0.0f, 1.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f), new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f))
    };
}

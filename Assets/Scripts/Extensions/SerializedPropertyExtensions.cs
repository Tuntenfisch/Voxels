using Unity.Mathematics;
using UnityEditor;

namespace Extensions
{
    public static class SerializedPropertyExtensions
    {
        public static float3 GetFloat3Value(this SerializedProperty serializedProperty)
        {
            float3 vector = int3.zero;

            SerializedProperty iterator = serializedProperty.FindPropertyRelative("x");

            for (int index = 0; index < 3; index++)
            {
                vector[index] = iterator.floatValue;
                iterator.Next(false);
            }

            return vector;
        }
    }
}
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Extensions
{
    public static class SerializedPropertyExtensions
    {
        public static float3 GetFloat3Value(this SerializedProperty property)
        {
            float3 vector = float3.zero;

            SerializedProperty iterator = property.FindPropertyRelative("x");

            for (int index = 0; index < 3; index++)
            {
                vector[index] = iterator.floatValue;
                iterator.Next(false);
            }

            return vector;
        }

        public static void SetFloat3Value(this SerializedProperty property, Vector3 value)
        {
            SerializedProperty iterator = property.FindPropertyRelative("x");

            for (int index = 0; index < 3; index++)
            {
                iterator.floatValue = value[index];
                iterator.Next(false);
            }
        }

        public static void SetInt3Value(this SerializedProperty property, float3 value)
        {
            Vector3 vector = new Vector3(value.x, value.y, value.z);
            SetFloat3Value(property, vector);
        }

        public static int3 GetInt3Value(this SerializedProperty property)
        {
            int3 vector = int3.zero;

            SerializedProperty iterator = property.FindPropertyRelative("x");

            for (int index = 0; index < 3; index++)
            {
                vector[index] = iterator.intValue;
                iterator.Next(false);
            }

            return vector;
        }

        public static void SetInt3Value(this SerializedProperty property, Vector3Int value)
        {
            SerializedProperty iterator = property.FindPropertyRelative("x");

            for (int index = 0; index < 3; index++)
            {
                iterator.intValue = value[index];
                iterator.Next(false);
            }
        }

        public static void SetInt3Value(this SerializedProperty property, int3 value)
        {
            Vector3Int vector = new Vector3Int(value.x, value.y, value.z);
            SetInt3Value(property, vector);
        }
    }
}
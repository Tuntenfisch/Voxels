using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Tuntenfisch.Extensions;

namespace Tuntenfisch.Editor
{
    [CustomPropertyDrawer(typeof(float3))]
    public class Float3PropertyDrawer : PropertyDrawer
    {
        SerializedProperty m_float3;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (m_float3 != property)
            {
                m_float3 = property;
            }

            m_float3.SetFloat3Value(EditorGUIExtensions.Float3Field(position, property.displayName, m_float3.GetFloat3Value()));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = 3;

            return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
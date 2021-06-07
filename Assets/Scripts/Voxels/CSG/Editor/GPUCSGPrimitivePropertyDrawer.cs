using Tuntenfisch.Extensions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(GPUCSGPrimitive))]
    public class GPUCSGPrimitivePropertyDrawer : PropertyDrawer
    {
        private SerializedProperty m_csgPrimitive;
        private SerializedProperty m_primitiveType;
        private SerializedProperty m_payload0;
        private SerializedProperty m_payload1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (m_csgPrimitive != property)
            {
                m_csgPrimitive = property;
                InitializeProperties();
            }

            EditorGUI.PropertyField(position, m_primitiveType);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            switch ((CSGPrimitiveType)m_primitiveType.intValue)
            {
                case CSGPrimitiveType.Sphere:
                    m_payload0.SetFloat3Value(EditorGUIExtensions.Float3Field(position, "Center", m_payload0.GetFloat3Value()));
                    position.y += 3.0f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    float3 payload1 = m_payload1.GetFloat3Value();
                    payload1.x = EditorGUI.FloatField(position, "Radius", payload1.x);
                    m_payload1.SetFloat3Value(payload1);
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case CSGPrimitiveType.Cuboid:
                    m_payload0.SetFloat3Value(EditorGUIExtensions.Float3Field(position, "Center", m_payload0.GetFloat3Value()));
                    position.y += 3.0f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    m_payload1.SetFloat3Value(EditorGUIExtensions.Float3Field(position, "Size", m_payload1.GetFloat3Value()));
                    position.y += 3.0f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    break;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_csgPrimitive != property)
            {
                m_csgPrimitive = property;
                InitializeProperties();
            }
            int lineCount = 1;

            switch((CSGPrimitiveType)m_primitiveType.intValue)
            {
                case CSGPrimitiveType.Sphere:
                    lineCount += 4;
                    break;

                case CSGPrimitiveType.Cuboid:
                    lineCount += 6;
                    break;
            }

            return lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        private void InitializeProperties()
        {
            m_primitiveType = m_csgPrimitive.FindPropertyRelative(nameof(m_primitiveType));
            m_payload0 = m_csgPrimitive.FindPropertyRelative(nameof(m_payload0));
            m_payload1 = m_csgPrimitive.FindPropertyRelative(nameof(m_payload1));
        }
    }
}
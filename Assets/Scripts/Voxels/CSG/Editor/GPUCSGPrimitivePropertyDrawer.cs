using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(GPUCSGPrimitive))]
    public class GPUCSGPrimitivePropertyDrawer : PropertyDrawer
    {
        private SerializedProperty m_csgPrimitive;
        private SerializedProperty m_materialIndex;
        private SerializedProperty m_primitiveType;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (m_csgPrimitive != property)
            {
                m_csgPrimitive = property;
                InitializeProperties();
            }

            EditorGUI.PropertyField(position, m_materialIndex);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_primitiveType);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = 2;

            return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        private void InitializeProperties()
        {
            m_materialIndex = m_csgPrimitive.FindPropertyRelative(nameof(m_materialIndex));
            m_primitiveType = m_csgPrimitive.FindPropertyRelative(nameof(m_primitiveType));
        }
    }
}
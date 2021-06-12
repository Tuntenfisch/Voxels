using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Nodes.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(GPUNoiseGraphNode))]
    public class GPUNoiseGraphNodePropertyDrawer : PropertyDrawer
    {

        private SerializedProperty m_node;
        private SerializedProperty m_nodeType;
        private SerializedProperty m_transformMatrix;
        private SerializedProperty m_noiseParameters;
        private SerializedProperty m_csgOperator;
        private SerializedProperty m_csgPrimitive;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (m_node != property)
            {
                m_node = property;
                InitializeProperties();
            }

            EditorGUI.PropertyField(position, m_nodeType);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_transformMatrix, true);
            position.y += EditorGUI.GetPropertyHeight(m_transformMatrix, true) + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_noiseParameters, true);
            position.y += EditorGUI.GetPropertyHeight(m_noiseParameters, true) + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_csgOperator, true);
            position.y += EditorGUI.GetPropertyHeight(m_csgOperator, true) + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_csgPrimitive, true);
            position.y += EditorGUI.GetPropertyHeight(m_csgPrimitive, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_node != property)
            {
                m_node = property;
                InitializeProperties();
            }

            float propertyHeight = 0.0f;

            propertyHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            propertyHeight += EditorGUI.GetPropertyHeight(m_transformMatrix, true) + EditorGUIUtility.standardVerticalSpacing;
            propertyHeight += EditorGUI.GetPropertyHeight(m_noiseParameters, true) + EditorGUIUtility.standardVerticalSpacing;
            propertyHeight += EditorGUI.GetPropertyHeight(m_csgOperator, true) + EditorGUIUtility.standardVerticalSpacing;
            propertyHeight += EditorGUI.GetPropertyHeight(m_csgPrimitive, true);

            return propertyHeight;
        }
        private void InitializeProperties()
        {
            m_nodeType = m_node.FindPropertyRelative(nameof(m_nodeType));
            m_transformMatrix = m_node.FindPropertyRelative(nameof(m_transformMatrix));
            m_noiseParameters = m_node.FindPropertyRelative(nameof(m_noiseParameters));
            m_csgOperator = m_node.FindPropertyRelative(nameof(m_csgOperator));
            m_csgPrimitive = m_node.FindPropertyRelative(nameof(m_csgPrimitive));
        }
    }
}
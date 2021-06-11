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
        private SerializedProperty m_dataIndex;

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
            m_dataIndex.intValue = EditorGUI.IntField(position, ObjectNames.NicifyVariableName(nameof(m_dataIndex)), m_dataIndex.intValue);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_node != property)
            {
                m_node = property;
                InitializeProperties();
            }
            int lineCount = 2;

            return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }
        private void InitializeProperties()
        {
            m_nodeType = m_node.FindPropertyRelative(nameof(m_nodeType));
            m_dataIndex = m_node.FindPropertyRelative(nameof(m_dataIndex));
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.CSG.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(GPUCSGOperator))]
    public class CSGOperatorPropertyDrawer : PropertyDrawer
    {
        private bool IsSmoothOperator
        {
            get
            {
                CSGOperatorIndex operatorIndex = (CSGOperatorIndex)m_operatorIndex.intValue;

                return operatorIndex == CSGOperatorIndex.SmoothUnion || operatorIndex == CSGOperatorIndex.SmoothIntersection || operatorIndex == CSGOperatorIndex.SmoothDifference;
            }
        }

        private SerializedProperty m_csgOperator;
        private SerializedProperty m_operatorIndex;
        private SerializedProperty m_smoothing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (m_csgOperator != property)
            {
                m_csgOperator = property;
                InitializeProperties();
            }

            EditorGUI.PropertyField(position, m_operatorIndex);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (IsSmoothOperator)
            {
                m_smoothing.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_smoothing)), m_smoothing.floatValue, 0.0f, 100.0f);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_csgOperator != property)
            {
                m_csgOperator = property;
                InitializeProperties();
            }

            int lineCount = IsSmoothOperator ? 2 : 1;

            return lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
        private void InitializeProperties()
        {
            m_operatorIndex = m_csgOperator.FindPropertyRelative(nameof(m_operatorIndex));
            m_smoothing = m_csgOperator.FindPropertyRelative(nameof(m_smoothing));
        }
    }
}
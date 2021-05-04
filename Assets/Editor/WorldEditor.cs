using Extensions;
using UnityEditor;
using UnityEngine;

namespace World
{
    [CustomEditor(typeof(World))]
    public class WorldEditor : Editor
    {
        SerializedProperty m_lodTreeDimensionsProperty;
        SerializedProperty m_numberOfLodTreesVisibleAlongAxisProperty;

        public void OnEnable()
        {
            m_lodTreeDimensionsProperty = serializedObject.FindProperty(World.LodTreeDimensionsPropertyName);
            m_numberOfLodTreesVisibleAlongAxisProperty = serializedObject.FindProperty(World.NumberOfLodTreesVisibleAlongAxisPropertyName);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            int numberOfLodTreesVisibleAlongAxis = m_numberOfLodTreesVisibleAlongAxisProperty.intValue;
            Vector3 lodTreeDimensions = m_lodTreeDimensionsProperty.GetFloat3Value();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Visible World Dimensions", (2.0f * numberOfLodTreesVisibleAlongAxis + 1.0f) * lodTreeDimensions);
            EditorGUI.EndDisabledGroup();
        }
    }
}
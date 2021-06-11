using Tuntenfisch.Editor;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Tuntenfisch.Voxels.Noise.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NoiseGraph))]
    public class NoiseGraphEditor : BaseEditor
    {
        private NoiseGraph NoiseGraph => (NoiseGraph)serializedObject.targetObject;

        private SerializedProperty m_nodes;
        private SerializedProperty m_noiseParameters;
        private SerializedProperty m_csgOperators;
        private SerializedProperty m_csgPrimitives;

        private void OnEnable()
        {
            m_nodes = serializedObject.FindProperty(nameof(m_nodes));
            m_noiseParameters = serializedObject.FindProperty(nameof(m_noiseParameters));
            m_csgOperators = serializedObject.FindProperty(nameof(m_csgOperators));
            m_csgPrimitives = serializedObject.FindProperty(nameof(m_csgPrimitives));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DisplayScriptHeader();

            if (GUILayout.Button("Edit"))
            {
                NodeEditorWindow.Open(NoiseGraph);
            }

            if (GUILayout.Button("Rebuild"))
            {
                NoiseGraph.Rebuild();
                EditorUtility.SetDirty(NoiseGraph);
            }

            EditorGUILayout.PropertyField(m_nodes);
            EditorGUILayout.PropertyField(m_noiseParameters);
            EditorGUILayout.PropertyField(m_csgOperators, new GUIContent("CSG Operators"));
            EditorGUILayout.PropertyField(m_csgPrimitives, new GUIContent("CSG Primitives"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
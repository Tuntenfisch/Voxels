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

        private void OnEnable()
        {
            m_nodes = serializedObject.FindProperty(nameof(m_nodes));
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
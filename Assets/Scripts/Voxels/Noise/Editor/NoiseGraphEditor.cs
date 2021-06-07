using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Tuntenfisch.Voxels.Noise.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NoiseGraph))]
    public class NoiseGraphEditor : GlobalGraphEditor
    {
        private NoiseGraph NoiseGraph => (NoiseGraph)serializedObject.targetObject;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Edit"))
            {
                NodeEditorWindow.Open(NoiseGraph);
            }

            if (GUILayout.Button("Apply"))
            {
                NoiseGraph.Rebuild();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NoiseConfig))]
    public class NoiseConfigEditor : UnityEditor.Editor
    {
        private NoiseConfig NoiseConfig => (NoiseConfig)target;

        private SerializedProperty m_noiseLayers;

        private void OnEnable()
        {
            m_noiseLayers = serializedObject.FindProperty(nameof(m_noiseLayers));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(NoiseConfig), typeof(NoiseConfig), false);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (GUILayout.Button("Apply"))
            {
                NoiseConfig.MakeDirty();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_noiseLayers);
        }
    }
}
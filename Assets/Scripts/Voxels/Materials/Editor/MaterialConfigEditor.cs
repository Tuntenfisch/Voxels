using System;
using Tuntenfisch.Editor;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.Materials.Editor
{
    [CustomEditor(typeof(MaterialConfig))]
    public class MaterialConfigEditor : BaseEditor
    {
        private static bool m_materialFoldout = true;

        private SerializedProperty m_renderMaterial;
        private SerializedProperty m_materialInfos;

        private void OnEnable()
        {
            m_renderMaterial = serializedObject.FindProperty(nameof(m_renderMaterial));
            m_materialInfos = serializedObject.FindProperty(nameof(m_materialInfos));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DisplayScriptHeader();

            EditorGUILayout.PropertyField(m_renderMaterial);

            if (m_materialFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_materialFoldout, ObjectNames.NicifyVariableName(nameof(m_materialInfos))))
            {
                EditorGUI.indentLevel++;
                int materialCount = Enum.GetNames(typeof(MaterialIndex)).Length;

                if (m_materialInfos.arraySize != materialCount)
                {
                    m_materialInfos.arraySize = materialCount;
                }

                for (int index = 0; index < m_materialInfos.arraySize; index++)
                {
                    EditorGUILayout.PropertyField(m_materialInfos.GetArrayElementAtIndex(index), new GUIContent($"{(MaterialIndex)index}"));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
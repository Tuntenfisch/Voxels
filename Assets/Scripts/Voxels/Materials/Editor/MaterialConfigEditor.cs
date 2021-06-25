using System;
using Tuntenfisch.Editor;
using UnityEditor;

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
                int materialCount = Enum.GetNames(typeof(MaterialIndex)).Length;

                if (m_materialInfos.arraySize != materialCount)
                {
                    m_materialInfos.arraySize = materialCount;
                }

                for (int index = 0; index < m_materialInfos.arraySize; index++)
                {
                    SerializedProperty m_material = m_materialInfos.GetArrayElementAtIndex(index);
                    SerializedProperty materialIndex = m_material.FindPropertyRelative("m_" + nameof(materialIndex));
                    SerializedProperty color = m_material.FindPropertyRelative("m_" + nameof(color));

                    materialIndex.intValue = index;

                    EditorGUILayout.LabelField($"{(MaterialIndex)materialIndex.intValue}", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(color);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
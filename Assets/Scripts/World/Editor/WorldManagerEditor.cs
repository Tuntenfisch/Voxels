using Tuntenfisch.Editor;
using Unity.Mathematics;
using UnityEditor;

namespace Tuntenfisch.World.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WorldManager))]
    public class WorldManagerEditor : BaseEditor
    {
        private static int MaxNumberOfLods => 5;

        private static bool s_showWorldOptions = true;
        private static bool s_showChunkOptions = true;
        private static bool s_showLevelOfDetailOptions = true;

        private SerializedProperty m_viewer;
        private SerializedProperty m_updateInterval;
        private SerializedProperty m_chunkPrefab;
        private SerializedProperty m_initialChunkPoolPopulation;
        private SerializedProperty m_lodDistances;

        private void OnEnable()
        {
            m_viewer = serializedObject.FindProperty(nameof(m_viewer));
            m_updateInterval = serializedObject.FindProperty(nameof(m_updateInterval));
            m_chunkPrefab = serializedObject.FindProperty(nameof(m_chunkPrefab));
            m_initialChunkPoolPopulation = serializedObject.FindProperty(nameof(m_initialChunkPoolPopulation));
            m_lodDistances = serializedObject.FindProperty(nameof(m_lodDistances));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DisplayScriptHeader();

            if (s_showWorldOptions = EditorGUILayout.BeginFoldoutHeaderGroup(s_showWorldOptions, "World"))
            {

                EditorGUILayout.ObjectField(m_viewer);
                m_updateInterval.floatValue = math.max(EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(m_updateInterval)), m_updateInterval.floatValue), 0.0f);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (s_showChunkOptions = EditorGUILayout.BeginFoldoutHeaderGroup(s_showChunkOptions, "Chunk"))
            {
                EditorGUILayout.ObjectField(m_chunkPrefab);
                m_initialChunkPoolPopulation.intValue = math.max(EditorGUILayout.IntField(ObjectNames.NicifyVariableName(nameof(m_initialChunkPoolPopulation)), m_initialChunkPoolPopulation.intValue), 0);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (s_showLevelOfDetailOptions = EditorGUILayout.BeginFoldoutHeaderGroup(s_showLevelOfDetailOptions, "Level Of Detail"))
            {
                int lods = EditorGUILayout.IntSlider("Levels Of Detail", m_lodDistances.arraySize, 1, MaxNumberOfLods);

                if (m_lodDistances.arraySize != lods)
                {
                    m_lodDistances.arraySize = lods;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Distances", EditorStyles.boldLabel);
                SerializedProperty lodDistance;
                float minValue = 0.0f;
                float maxValue;

                for (int index = 0; index < lods - 1; index++)
                {
                    maxValue = m_lodDistances.GetArrayElementAtIndex(index + 1).floatValue;
                    lodDistance = m_lodDistances.GetArrayElementAtIndex(index);
                    lodDistance.floatValue = EditorGUILayout.Slider($"Level Of Detail {index}", lodDistance.floatValue, 0.0f, m_lodDistances.GetArrayElementAtIndex(lods - 1).floatValue);
                    lodDistance.floatValue = math.clamp(lodDistance.floatValue, minValue, maxValue);
                    minValue = lodDistance.floatValue;
                }
                lodDistance = m_lodDistances.GetArrayElementAtIndex(lods - 1);
                lodDistance.floatValue = math.max(EditorGUILayout.FloatField($"Level Of Detail {lods - 1}", lodDistance.floatValue), minValue);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
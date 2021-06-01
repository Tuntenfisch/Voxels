using Unity.Mathematics;
using UnityEditor;

namespace Tuntenfisch.World
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(World))]
    public class WorldEditor : Editor
    {
        private static int MaxNumberOfLods => 5;

        private static bool s_showWorldOptions;
        private static bool s_showChunkOptions;
        private static bool s_showLevelOfDetailOptions;

        private SerializedProperty m_viewer;
        private SerializedProperty m_updateInterval;
        private SerializedProperty m_chunkPrefab;
        private SerializedProperty m_lodDistances;
        private SerializedProperty m_maxNumberOfChunksProcessedEachFrame;

        private void OnEnable()
        {
            m_viewer = serializedObject.FindProperty(nameof(m_viewer));
            m_updateInterval = serializedObject.FindProperty(nameof(m_updateInterval));
            m_chunkPrefab = serializedObject.FindProperty(nameof(m_chunkPrefab));
            m_lodDistances = serializedObject.FindProperty(nameof(m_lodDistances));
            m_maxNumberOfChunksProcessedEachFrame = serializedObject.FindProperty(nameof(m_maxNumberOfChunksProcessedEachFrame));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (s_showWorldOptions = EditorGUILayout.BeginFoldoutHeaderGroup(s_showWorldOptions, "World"))
            {

                EditorGUILayout.ObjectField(m_viewer);
                m_updateInterval.floatValue = math.max(EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(m_updateInterval)), m_updateInterval.floatValue), 0.0f);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (s_showChunkOptions = EditorGUILayout.BeginFoldoutHeaderGroup(s_showChunkOptions, "Chunk"))
            {
                EditorGUILayout.ObjectField(m_chunkPrefab);
                m_maxNumberOfChunksProcessedEachFrame.intValue = EditorGUILayout.IntSlider(ObjectNames.NicifyVariableName(nameof(m_maxNumberOfChunksProcessedEachFrame)), m_maxNumberOfChunksProcessedEachFrame.intValue, 10, 100);
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
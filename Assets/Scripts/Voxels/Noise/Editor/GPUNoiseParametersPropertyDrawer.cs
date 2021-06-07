using Tuntenfisch.Extensions;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Voxels.Noise.Editor
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(GPUNoiseParameters))]
    public class NoiseParametersPropertyDrawer : PropertyDrawer
    {

        private SerializedProperty m_noiseParameters;
        private SerializedProperty m_seed;
        private SerializedProperty m_noiseAxes;
        private SerializedProperty m_noiseType;
        private SerializedProperty m_numberOfOctaves;
        private SerializedProperty m_initialAmplitude;
        private SerializedProperty m_initialFrequency;
        private SerializedProperty m_persistence;
        private SerializedProperty m_lacunarity;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            if (m_noiseParameters != property)
            {
                m_noiseParameters = property;
                InitializeProperties();
            }

            EditorGUI.LabelField(position, "General", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_seed.intValue = EditorGUI.IntField(position, ObjectNames.NicifyVariableName(nameof(m_seed)), m_seed.intValue);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_noiseAxes);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, m_noiseType);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.LabelField(position, "Fractional Brownian Motion", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_numberOfOctaves.intValue = EditorGUI.IntSlider(position, ObjectNames.NicifyVariableName(nameof(m_numberOfOctaves)), m_numberOfOctaves.intValue, 1, 32);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_initialAmplitude.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_initialAmplitude)), m_initialAmplitude.floatValue, 0.0f, 500.0f);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_initialFrequency.SetFloat3Value(EditorGUIExtensions.Float3Slider(position, ObjectNames.NicifyVariableName(nameof(m_initialFrequency)), m_initialFrequency.GetFloat3Value(), 0.0f, 0.01f));
            position.y += 3.0f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            m_persistence.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_persistence)), m_persistence.floatValue, 0.0f, 2.0f);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_lacunarity.SetFloat3Value(EditorGUIExtensions.Float3Slider(position, ObjectNames.NicifyVariableName(nameof(m_lacunarity)), m_lacunarity.GetFloat3Value(), 0.25f, 4.0f));
            position.y += 3.0f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_noiseParameters != property)
            {
                m_noiseParameters = property;
                InitializeProperties();
            }
            int lineCount = 14;

            return lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
        private void InitializeProperties()
        {
            m_seed = m_noiseParameters.FindPropertyRelative(nameof(m_seed));
            m_noiseAxes = m_noiseParameters.FindPropertyRelative(nameof(m_noiseAxes));
            m_noiseType = m_noiseParameters.FindPropertyRelative(nameof(m_noiseType));
            m_numberOfOctaves = m_noiseParameters.FindPropertyRelative(nameof(m_numberOfOctaves));
            m_initialAmplitude = m_noiseParameters.FindPropertyRelative(nameof(m_initialAmplitude));
            m_initialFrequency = m_noiseParameters.FindPropertyRelative(nameof(m_initialFrequency));
            m_persistence = m_noiseParameters.FindPropertyRelative(nameof(m_persistence));
            m_lacunarity = m_noiseParameters.FindPropertyRelative(nameof(m_lacunarity));
        }
    }
}
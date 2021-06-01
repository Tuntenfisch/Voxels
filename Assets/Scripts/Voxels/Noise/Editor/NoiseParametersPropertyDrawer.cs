using System;
using UnityEditor;
using UnityEngine;
using Tuntenfisch.Voxels.ConstructiveSolidGeometry;

namespace Tuntenfisch.Voxels.Noise
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(NoiseParameters))]
    public class NoiseParametersPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty m_noiseParameters;
        private SerializedProperty m_seed;
        private SerializedProperty m_noiseDimensionality;
        private SerializedProperty m_noiseType;
        private SerializedProperty m_numberOfOctaves;
        private SerializedProperty m_initialAmplitude;
        private SerializedProperty m_initialFrequency;
        private SerializedProperty m_persistence;
        private SerializedProperty m_lacunarity;
        private SerializedProperty m_operatorIndex;
        private SerializedProperty m_smoothing;

        private readonly GUIStyle m_centeredBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_noiseParameters != property)
            {
                m_noiseParameters = property;
                InitializeProperties();
            }

            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, ObjectNames.NicifyVariableName(nameof(NoiseParameters)), m_centeredBoldLabelStyle);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(position, "General", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_seed.intValue = EditorGUI.IntField(position, ObjectNames.NicifyVariableName(nameof(m_seed)), m_seed.intValue);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_noiseDimensionality.intValue = Convert.ToInt32(EditorGUI.EnumPopup(position, ObjectNames.NicifyVariableName(nameof(m_noiseDimensionality)), (NoiseDimensionality)m_noiseDimensionality.intValue));
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_noiseType.intValue = Convert.ToInt32(EditorGUI.EnumPopup(position, ObjectNames.NicifyVariableName(nameof(m_noiseType)), (NoiseType)m_noiseType.intValue));

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(position, "Fractional Brownian Motion", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_numberOfOctaves.intValue = EditorGUI.IntSlider(position, ObjectNames.NicifyVariableName(nameof(m_numberOfOctaves)), m_numberOfOctaves.intValue, 1, 32);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_initialAmplitude.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_initialAmplitude)), m_initialAmplitude.floatValue, 0.0f, 500.0f);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_initialFrequency.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_initialFrequency)), m_initialFrequency.floatValue, 0.0f, 0.01f);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_persistence.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_persistence)), m_persistence.floatValue, 0.0f, 2.0f);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_lacunarity.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_lacunarity)), m_lacunarity.floatValue, 1.0f, 4.0f);

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(position, "Combine Operation", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_operatorIndex.intValue = Convert.ToInt32(EditorGUI.EnumPopup(position, ObjectNames.NicifyVariableName(nameof(m_operatorIndex)), (Operator)m_operatorIndex.intValue));
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_smoothing.floatValue = EditorGUI.Slider(position, ObjectNames.NicifyVariableName(nameof(m_smoothing)), m_smoothing.floatValue, 1.0f, 100.0f);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = property.CountInProperty() + 3;

            return EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);
        }

        private void InitializeProperties()
        {
            m_seed = m_noiseParameters.FindPropertyRelative(nameof(m_seed));
            m_noiseDimensionality = m_noiseParameters.FindPropertyRelative(nameof(m_noiseDimensionality));
            m_noiseType = m_noiseParameters.FindPropertyRelative(nameof(m_noiseType));
            m_numberOfOctaves = m_noiseParameters.FindPropertyRelative(nameof(m_numberOfOctaves));
            m_initialAmplitude = m_noiseParameters.FindPropertyRelative(nameof(m_initialAmplitude));
            m_initialFrequency = m_noiseParameters.FindPropertyRelative(nameof(m_initialFrequency));
            m_persistence = m_noiseParameters.FindPropertyRelative(nameof(m_persistence));
            m_lacunarity = m_noiseParameters.FindPropertyRelative(nameof(m_lacunarity));
            m_operatorIndex = m_noiseParameters.FindPropertyRelative(nameof(m_operatorIndex));
            m_smoothing = m_noiseParameters.FindPropertyRelative(nameof(m_smoothing));
        }
    }
}
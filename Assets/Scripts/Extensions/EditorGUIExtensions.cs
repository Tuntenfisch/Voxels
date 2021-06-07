using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Extensions
{
    public static class EditorGUIExtensions
    {
        public static float3 Float3Field(Rect position, string label, float3 value)
        {
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect sliderRect = new Rect(labelRect.xMax, labelRect.y, position.xMax - labelRect.xMax, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, label);

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 10.0f;

            // X-axis field.
            value.x = EditorGUI.FloatField(sliderRect, ObjectNames.NicifyVariableName(nameof(value.x)), value.x);
            sliderRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Y-axis field.
            value.y = EditorGUI.FloatField(sliderRect, ObjectNames.NicifyVariableName(nameof(value.y)), value.y);
            sliderRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Z-axis field.
            value.z = EditorGUI.FloatField(sliderRect, ObjectNames.NicifyVariableName(nameof(value.z)), value.z);
            sliderRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUIUtility.labelWidth = oldLabelWidth;

            return value;
        }

        public static float3 Float3Slider(Rect position, string label, float3 value, float3 min, float3 max)
        {
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect sliderRect = new Rect(labelRect.xMax, labelRect.y, position.xMax - labelRect.xMax, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, label);

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 10.0f;

            // X-axis slider.
            value.x = EditorGUI.Slider(sliderRect, ObjectNames.NicifyVariableName(nameof(value.x)), value.x, min.x, max.x);
            sliderRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Y-axis slider.
            value.y = EditorGUI.Slider(sliderRect, ObjectNames.NicifyVariableName(nameof(value.y)), value.y, min.y, max.y);
            sliderRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Z-axis slider.
            value.z = EditorGUI.Slider(sliderRect, ObjectNames.NicifyVariableName(nameof(value.z)), value.z, min.z, max.z);
            sliderRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUIUtility.labelWidth = oldLabelWidth;

            return value;
        }
    }
}
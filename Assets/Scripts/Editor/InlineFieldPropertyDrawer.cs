using Tuntenfisch.Attributes;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Editor
{
    [CustomPropertyDrawer(typeof(InlineFieldAttribute))]
    public class InlineFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren)
            {
                property.NextVisible(true);

                do
                {
                    position.height = EditorGUI.GetPropertyHeight(property);
                    EditorGUI.PropertyField(position, property, true);
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                }
                while (property.NextVisible(false));
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren)
            {
                float propertyHeight = 0.0f;

                property.NextVisible(true);

                do
                {
                    propertyHeight += EditorGUI.GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
                }
                while (property.NextVisible(false));

                return propertyHeight - EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                return EditorGUI.GetPropertyHeight(property, true);
            }
        }
    }
}
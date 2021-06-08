using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Editor
{
    public class BaseEditor : UnityEditor.Editor
    {
        protected void DisplayScriptHeader()
        {
            MonoScript script = null;

            if (serializedObject.targetObject is MonoBehaviour monoBehaviour)
            {
                script = MonoScript.FromMonoBehaviour(monoBehaviour);
            }
            else if (serializedObject.targetObject is ScriptableObject scriptableObject)
            {
                script = MonoScript.FromScriptableObject(scriptableObject);
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", script, serializedObject.targetObject.GetType(), false);
            EditorGUI.EndDisabledGroup();
        }
    }
}
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Tuntenfisch.Generics
{
    public abstract class ManagedScriptableObject : ScriptableObject
    {
        abstract protected void OnScriptableObjectAwake();
        abstract protected void OnScriptableObjectDestroy();

        protected void Awake()
        {
            OnScriptableObjectAwake();
        }

        protected void OnDestroy()
        {
            OnScriptableObjectDestroy();
        }

#if UNITY_EDITOR
        protected void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlayStateChanged;
        }

        protected void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayStateChanged;
        }

        private void PlayStateChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    OnScriptableObjectAwake();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    OnScriptableObjectDestroy();
                    break;
            }
        }
#endif
    }
}
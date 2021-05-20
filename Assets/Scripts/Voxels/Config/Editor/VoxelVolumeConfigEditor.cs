using System;
using UnityEditor;

namespace Tuntenfisch.Voxels.Config
{
    [CustomEditor(typeof(VoxelVolumeConfig))]
    public class VoxelVolumeConfigEditor : Editor
    {
        private static int[] PossibleNumberOfVoxelsAlongAxis => VoxelVolumeConfig.PossibleNumberOfVoxelsAlongAxis;

        private SerializedProperty m_compute;
        private SerializedProperty m_numberOfVoxelsAlongAxis;
        private SerializedProperty m_voxelSpacing;

        private void OnEnable()
        {
            m_compute = serializedObject.FindProperty(nameof(m_compute));
            m_numberOfVoxelsAlongAxis = serializedObject.FindProperty(nameof(m_numberOfVoxelsAlongAxis));
            m_voxelSpacing = serializedObject.FindProperty(nameof(m_voxelSpacing));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.ObjectField(m_compute);
            int currentIndex = Array.FindIndex(PossibleNumberOfVoxelsAlongAxis, (integer) => { return integer == m_numberOfVoxelsAlongAxis.intValue; });
            currentIndex = currentIndex == -1 ? 0 : currentIndex;
            int newindex = EditorGUILayout.Popup("Number Of Voxels Along Axis", currentIndex, Array.ConvertAll(PossibleNumberOfVoxelsAlongAxis, (integer) => { return integer.ToString(); }));
            m_numberOfVoxelsAlongAxis.intValue = PossibleNumberOfVoxelsAlongAxis[newindex];
            m_voxelSpacing.floatValue = EditorGUILayout.Slider("Voxel Spacing", m_voxelSpacing.floatValue, 0.25f, 2.0f);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
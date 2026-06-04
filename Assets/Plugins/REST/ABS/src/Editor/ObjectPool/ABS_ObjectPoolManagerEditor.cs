//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_ObjectPoolManager))]
    internal class ABS_ObjectPoolManagerEditor : ABS_EditorBase
    {
        private SerializedProperty m_MaximumElementCapacityProperty;
        private GUIContent m_MaximumElementCapacityGUIContent;
        private SerializedProperty m_TargetElementCapacityProperty;
        private GUIContent m_TargetElementCapacityGUIContent;

        private SerializedProperty m_BufferingProperty;
        private GUIContent m_BufferingGUIContent;
        private SerializedProperty m_BufferCreateTimerDefaultProperty;
        private GUIContent m_BufferCreateTimerDefaultGUIContent;

        private SerializedProperty m_ClearPoolTimerProperty;
        private GUIContent m_ClearPoolTimerGUIContent;

        protected override void OnEnableImpl()
        {
            m_MaximumElementCapacityProperty = serializedObject.FindProperty("m_MaximumElementCapacity");
            m_MaximumElementCapacityGUIContent = new GUIContent("Maximum Element Capacity", "Maximum element capacity of the pool");
            m_TargetElementCapacityProperty = serializedObject.FindProperty("m_TargetElementCapacity");
            m_TargetElementCapacityGUIContent = new GUIContent("Target Element Capacity", "Target element capacity of the Buffer");


            m_BufferingProperty = serializedObject.FindProperty("m_Buffering");
            m_BufferingGUIContent = new GUIContent("Buffering", "Enable the buffering capacity of the pool");
            m_BufferCreateTimerDefaultProperty = serializedObject.FindProperty("m_BufferCreateTimerDefault");
            m_BufferCreateTimerDefaultGUIContent = new GUIContent("Buffer Create Default Cycle Timer", "Element creation cycle timer of the buffering logic.");

            m_ClearPoolTimerProperty = serializedObject.FindProperty("m_ClearPoolTimer");
            m_ClearPoolTimerGUIContent = new GUIContent("Clear Pool Timer", "Automatic pool clear timer.");
        }

        protected override void OnInspectorGUIImpl()
        {
            ABS_EditorUtils.StartDisableDuringGame();
            {
                ABS_EditorUtils.AddPropertyField(m_MaximumElementCapacityProperty, m_MaximumElementCapacityGUIContent);
                ABS_EditorUtils.AddPropertyField(m_TargetElementCapacityProperty, m_TargetElementCapacityGUIContent);
                if (m_MaximumElementCapacityProperty.intValue < m_TargetElementCapacityProperty.intValue)
                {
                    EditorGUILayout.HelpBox("The target capacity should be smaller then the maximum capacity", MessageType.Error);
                }

                ABS_EditorUtils.Space();
                ABS_EditorUtils.AddPropertyField(m_BufferingProperty, m_BufferingGUIContent);
                if (m_BufferingProperty.boolValue)
                {
                    ABS_EditorUtils.AddPropertyField(m_BufferCreateTimerDefaultProperty, m_BufferCreateTimerDefaultGUIContent);
                }

                ABS_EditorUtils.Space();
                ABS_EditorUtils.AddPropertyField(m_ClearPoolTimerProperty, m_ClearPoolTimerGUIContent);
            }
            ABS_EditorUtils.EndDisableDuringGame();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "ObjectPool";
        }
    }
}

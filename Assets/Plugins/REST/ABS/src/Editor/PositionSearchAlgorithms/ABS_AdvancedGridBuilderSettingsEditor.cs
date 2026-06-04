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
    [CustomEditor(typeof(ABS_AdvancedGridBuilderSettings))]
    internal class ABS_AdvancedGridBuilderSettingsEditor : BuilderBaseSettingsEditor
    {
        //----------------------------------------------------------------------------------------------------------------------
        //Grid building
        private SerializedProperty m_GridSizeProperty;
        private GUIContent m_GridSizeGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Section
        private static bool m_PositionAlgorithmDetailsSectionVariable = false;

        public override void OnEnable()
        {
            base.OnEnable();

            //Grid building
            m_GridSizeProperty = serializedObject.FindProperty("m_GridSize");
            m_GridSizeGUIContent = new GUIContent("Grid Size", "The size of the grid. The grid can be different in every axis.");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void AddSpecialProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_GridSizeProperty, m_GridSizeGUIContent);
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Advanced Grid Building Settings";
        }

        public static new void DrawSettingsDetails(ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_BuilderBaseSettings p_Settings)
        {
            m_PositionAlgorithmDetailsSectionVariable = EditorGUILayout.BeginFoldoutHeaderGroup(m_PositionAlgorithmDetailsSectionVariable, "Details");
            if (m_PositionAlgorithmDetailsSectionVariable)
            {
                ABS_EditorUtils.BoxStart(p_EditorStyleContainer.DarkBoxStyle);
                {
                    BuilderBaseSettingsEditor.DrawSettingsDetails(p_EditorStyleContainer, p_Settings);
                    AddAdvancedGridBuildDetails(p_EditorStyleContainer, p_Settings as ABS_AdvancedGridBuilderSettings);
                }
                ABS_EditorUtils.BoxEnd();
                ABS_EditorUtils.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void AddAdvancedGridBuildDetails (ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("AdvancedGrid Building", p_EditorStyleContainer.HeadStyleSpecificProperties);
            ABS_EditorUtils.IndentIn();
            {
                EditorGUILayout.LabelField("GridSize  :  " + p_Settings.GridSize);
            }
            ABS_EditorUtils.IndentOut();
        }

    }
}

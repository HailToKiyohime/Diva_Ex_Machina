//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_SnapPointBasedBuilderSettings))]
    internal class ABS_SnapPointBasedBuilderSettingsEditor : BuilderBaseSettingsEditor
    {
        //----------------------------------------------------------------------------------------------------------------------
        //ABS_Building
        private SerializedProperty m_SnapRelationshipListProperty;
        private GUIContent m_SnapRelationshipListGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Section
        private static bool m_PositionAlgorithmDetailsSectionVariable = false;

        public override void OnEnable()
        {
            base.OnEnable();

            //----------------------------------------------------------------------------------------------------------------------
            //ABS_Building
            m_SnapRelationshipListProperty = serializedObject.FindProperty("m_SnapRelationshipList");
            m_SnapRelationshipListGUIContent = new GUIContent("Snap Relationship List", "The list of the snapping relationships between the elements.");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void AddSpecialProperties()
        {
            AddSnapPointBasedBuilderProperties();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "SnapPoint Based Building Settings";
        }

        public static new void DrawSettingsDetails(ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_BuilderBaseSettings p_Settings)
        {
            m_PositionAlgorithmDetailsSectionVariable = EditorGUILayout.BeginFoldoutHeaderGroup(m_PositionAlgorithmDetailsSectionVariable, "Details");
            if (m_PositionAlgorithmDetailsSectionVariable)
            {
                ABS_EditorUtils.BoxStart(p_EditorStyleContainer.DarkBoxStyle);
                {
                    BuilderBaseSettingsEditor.DrawSettingsDetails(p_EditorStyleContainer, p_Settings);
                    AddSnapPointBasedBuilderDetails(p_Settings as ABS_SnapPointBasedBuilderSettings);
                }
                ABS_EditorUtils.BoxEnd();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void AddSnapPointBasedBuilderDetails(in ABS_SnapPointBasedBuilderSettings p_Settings)
        {
            ABS_EditorUtils.IndentIn();
            {
                ABS_EditorUtils.AddObjectLinkLabel(p_Settings.ABS_SnapRelationshipList, 100);
            }
            ABS_EditorUtils.IndentOut();
        }

        private void AddSnapPointBasedBuilderProperties()
        {
            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("SnapPoint Based Building", m_EditorStyleContainer.HeadStyleSpecificProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_SnapRelationshipList>(
                    ref m_SnapRelationshipListProperty,
                    m_SnapRelationshipListGUIContent,
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    "Missing SnapRelationshipList",
                    "Add a path to the new SnapRelationshipList",
                    "NewSnapRelationshipList");

                ABS_EditorUtils.Space();
            }
            ABS_EditorUtils.BoxEnd();

        }
    }
}

//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_AdvancedGridBuilding))]
    [CanEditMultipleObjects]
    internal class ABS_AdvancedGridBuildingEditor : ABS_BuildingEditor
    {
        private SerializedProperty m_BuildingPositionModifierProperty;
        private GUIContent m_BuildingPositionModifierGUIContent;

        private SerializedProperty m_EnableStabilityProperty;
        private GUIContent m_EnableStabilityGUIContent;
        private SerializedProperty m_StabilityLevelProperty;
        private GUIContent m_StabilityLevelGUIContent;

        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
            m_BuildingPositionModifierProperty = serializedObject.FindProperty("m_BuildingPositionModifier");
            m_BuildingPositionModifierGUIContent = new GUIContent("Building Position Modifier", "The position transformation vector of the first element " +
                "to ensure that the grid is matching wathever element is used when the ABS_Building was built.");

            m_EnableStabilityProperty = serializedObject.FindProperty("m_EnableStability");
            m_EnableStabilityGUIContent = new GUIContent("Enable Stability Feature");
            m_StabilityLevelProperty = serializedObject.FindProperty("m_StabilityLevel");
            m_StabilityLevelGUIContent = new GUIContent("Stability Level", "The Stability Level for the Stability feature." +
                " Every element got a stability level and if it would go under 0 it will be destroyed");
        }

        protected override void OnInspectorGUIImpl()
        {
            List<ABS_AdvancedGridBuilding> targets = GetSelectedTargetsComponents<ABS_AdvancedGridBuilding>();
            ABS_EditorUtils.StartHorizontal();
            {
                ABS_EditorUtils.StartDisable(true);
                {
                    ABS_EditorUtils.AddPropertyField(m_BuildingPositionModifierProperty, m_BuildingPositionModifierGUIContent);
                }
                ABS_EditorUtils.EndDisable();

                ABS_EditorUtils.HorizontalSpace(10);

                bool buttonResult = GUILayout.Button("Refresh Modifier", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
                if (buttonResult)
                {
                    foreach (ABS_AdvancedGridBuilding target in targets)
                    {
                        target.RefreshPositionModifier();
                    }
                }
            }
            ABS_EditorUtils.EndHorizontal();

            ABS_EditorUtils.StartDisable(true);
            {
               ABS_EditorUtils.AddPropertyField(m_EnableStabilityProperty, m_EnableStabilityGUIContent);
               ABS_EditorUtils.AddPropertyField(m_StabilityLevelProperty, m_StabilityLevelGUIContent);
            }
            ABS_EditorUtils.EndDisable();

            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddSeparatorLine();
            base.OnInspectorGUIImpl();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Advanced Grid Building";
        }
    }

}

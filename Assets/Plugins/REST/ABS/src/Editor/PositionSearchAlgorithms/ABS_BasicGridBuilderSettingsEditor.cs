//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BasicGridBuilderSettings))]
    internal class ABS_BasicGridBuilderSettingsEditor : BuilderBaseSettingsEditor
    {
        //----------------------------------------------------------------------------------------------------------------------
        //Grid building

        private SerializedProperty m_GridSizeProperty;
        private GUIContent m_GridSizeGUIContent;
        
        private SerializedProperty m_VerticalGridPlacementProperty;

        private SerializedProperty m_VerticalGridFixedPositionProperty;

        //----------------------------------------------------------------------------------------------------------------------
        //Section
        private static bool m_PositionAlgorithmDetailsSectionVariable = false;

        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
            m_GridSizeProperty = serializedObject.FindProperty("m_GridSize");
            m_GridSizeGUIContent = new GUIContent("Grid Size", "The size of the grid. In case of BasicGrid the Vector2's Y value handled as Z in the 3D space.");

            m_VerticalGridPlacementProperty = serializedObject.FindProperty("m_VerticalGridPlacement");
            m_VerticalGridFixedPositionProperty = serializedObject.FindProperty("m_VerticalGridFixedPosition");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void AddSpecialProperties()
        {
            Vector2 value = ABS_EditorUtils.AddVector2Field(m_GridSizeGUIContent, new Vector2(m_GridSizeProperty.vector2Value.x, m_GridSizeProperty.vector2Value.y));
            m_GridSizeProperty.vector2Value = value;

            EditorGUILayout.PropertyField(m_VerticalGridPlacementProperty);
            switch (m_VerticalGridPlacementProperty.enumValueIndex)
            {
                case (int)VerticalGridPlacement.FixedPosition:
                    EditorGUILayout.PropertyField(m_VerticalGridFixedPositionProperty);
                    break;
                case (int)VerticalGridPlacement.RaycastPosition:
                case (int)VerticalGridPlacement.AlignToGround:
                    break;
            }
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Basic Grid Building Settings";
        }


        public static new void DrawSettingsDetails(ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_BuilderBaseSettings p_Settings)
        {
            m_PositionAlgorithmDetailsSectionVariable = EditorGUILayout.BeginFoldoutHeaderGroup(m_PositionAlgorithmDetailsSectionVariable, "Details");
            if (m_PositionAlgorithmDetailsSectionVariable)
            {
                ABS_EditorUtils.BoxStart(p_EditorStyleContainer.DarkBoxStyle);
                BuilderBaseSettingsEditor.DrawSettingsDetails(p_EditorStyleContainer, p_Settings);
                AddBasicGridSearchDetails(p_EditorStyleContainer, p_Settings as ABS_BasicGridBuilderSettings);
                ABS_EditorUtils.BoxEnd();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void AddBasicGridSearchDetails (ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_BasicGridBuilderSettings p_Settings)
        {
            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("BasicGrid Building", p_EditorStyleContainer.HeadStyleSpecificProperties);
            ABS_EditorUtils.IndentIn();
            {
                EditorGUILayout.LabelField("GridSize  :  " + p_Settings.GridSize);

                EditorGUILayout.LabelField("VerticalGridPlacement  :  " + p_Settings.VerticalGridPlacement);
                switch (p_Settings.VerticalGridPlacement)
                {
                    case VerticalGridPlacement.FixedPosition:
                        EditorGUILayout.LabelField("VerticalGridFixedPosition  :  " + p_Settings.VerticalGridFixedPosition);
                        break;
                    case VerticalGridPlacement.RaycastPosition:
                    case VerticalGridPlacement.AlignToGround:
                        break;
                }
            }
            ABS_EditorUtils.IndentOut();
        }
    }
}

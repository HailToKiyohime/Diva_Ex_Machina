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
    public class ABS_ProjectSettingsProvider : ABS_EditorSettingsProviderBase<ABS_ProjectSettings>
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private ABS_EditorStyleContainer m_EditorStyleContainer = null;

        private ABS_EditorTabView m_TabView = null;

        private SerializedProperty m_DragBuilding_AdvancedGridValidationProcessProperty;
        private GUIContent m_DragBuilding_AdvancedGridValidationProcessGUIContent;

        private SerializedProperty m_PositionSearchProcess_ResultProperty;
        private GUIContent m_PositionSearchProcess_ResultGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Raycast
        private SerializedProperty m_Raycast_LineProperty;
        private GUIContent m_Raycast_LineGUIContent;
        private SerializedProperty m_Raycast_LineColorProperty;

        private SerializedProperty m_Raycast_HitpointProperty;
        private GUIContent m_Raycast_HitpointGUIContent;
        private SerializedProperty m_Raycast_HitpointColorProperty;

        //----------------------------------------------------------------------------------------------------------------------
        //Position Search
        private SerializedProperty m_PositionSearch_SearchColliderProperty;
        private GUIContent m_PositionSearch_SearchColliderGUIContent;
        private SerializedProperty m_PositionSearch_SearchColliderColorProperty;

        private SerializedProperty m_PositionSearch_BuildColliderProperty;
        private GUIContent m_PositionSearch_BuildColliderGUIContent;
        private SerializedProperty m_PositionSearch_BuildColliderColorProperty;

        private SerializedProperty m_PositionSearch_CheckedBESnapPointsProperty;
        private GUIContent m_PositionSearch_CheckedBESnapPointsGUIContent;
        private SerializedProperty m_PositionSearch_CheckedBESnapPointsColorProperty;

        private SerializedProperty m_PositionSearch_SnapPointsAreaProperty;
        private GUIContent m_PositionSearch_SnapPointsAreaGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Position Validation
        private SerializedProperty m_PositionValidation_AirBuildingMaximumRangeProperty;
        private GUIContent m_PositionValidation_AirBuildingMaximumRangeGUIContent;
        private SerializedProperty m_PositionValidation_BuildableGroundProperty;
        private GUIContent m_PositionValidation_BuildableGroundGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Drag Building
        private SerializedProperty m_DragBuilding_IndexProperty;
        private GUIContent m_DragBuilding_IndexGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Building Element
        private SerializedProperty m_BuildingElement_StabilityProperty;
        private GUIContent m_BuildingElement_StabilityGUIContent;
        private SerializedProperty m_BuildingElement_StabilityWhenSelectedProperty;
        private GUIContent m_BuildingElement_StabilityWhenSelectedGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //BuildingArea
        private SerializedProperty m_BuildingArea_AreaColliderProperty;
        private GUIContent m_BuildingArea_AreaColliderGUIContent;
        private SerializedProperty m_BuildingArea_AreaColliderColorProperty;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Constructor
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ABS_ProjectSettingsProvider() : base("Project/Advanced Building System", SettingsScope.Project)
        {
            m_TabView = new ABS_EditorTabView(2);
            m_TabView.AddCallback("Gizmos", AddGizmosProperties);
            m_TabView.AddCallback("Debug", AddDebugProperties);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Constructor
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region ABS_EditorSettingsProviderBase Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [SettingsProvider]
        public static SettingsProvider CreateEditorSettingsProvider()
        {
            ABS_ProjectSettingsProvider provider = new ABS_ProjectSettingsProvider();
            provider.keywords = provider.GetKeywords();
            return provider;
        }

        protected override HashSet<string> GetKeywords()
        { 
            return new HashSet<string>(new[] { "REST", "Settings", "Custom", "ABS" });
        }

        protected override void OnActivateImpl(SerializedObject p_SerializedObject)
        {
            m_DragBuilding_AdvancedGridValidationProcessProperty = p_SerializedObject.FindProperty("m_DragBuilding_AdvancedGridValidationProcess");
            m_DragBuilding_AdvancedGridValidationProcessGUIContent = new GUIContent("Advanced Grid Validation Process");

            m_PositionSearchProcess_ResultProperty = p_SerializedObject.FindProperty("m_PositionSearchProcess_Result");
            m_PositionSearchProcess_ResultGUIContent = new GUIContent("Result");

            //Raycast
            m_Raycast_LineProperty = p_SerializedObject.FindProperty("m_Raycast_Line");
            m_Raycast_LineGUIContent = new GUIContent("Raycast Line", "Draw the Raycast's line");
            m_Raycast_LineColorProperty = p_SerializedObject.FindProperty("m_Raycast_LineColor");

            m_Raycast_HitpointProperty = p_SerializedObject.FindProperty("m_Raycast_Hitpoint");
            m_Raycast_HitpointGUIContent = new GUIContent("Raycast Hitpoint", "Draw the Raycast's hitpoint");
            m_Raycast_HitpointColorProperty = p_SerializedObject.FindProperty("m_Raycast_HitpointColor");

            //Position Search
            m_PositionSearch_SearchColliderProperty = p_SerializedObject.FindProperty("m_PositionSearch_SearchCollider");
            m_PositionSearch_SearchColliderGUIContent = new GUIContent("Gizmos Draw Search Collider", "Draw the Search radius in gizmos");
            m_PositionSearch_SearchColliderColorProperty = p_SerializedObject.FindProperty("m_PositionSearch_SearchColliderColor");

            m_PositionSearch_BuildColliderProperty = p_SerializedObject.FindProperty("m_PositionSearch_BuildCollider");
            m_PositionSearch_BuildColliderGUIContent = new GUIContent("Gizmos Draw Build Collider", "Draw the Build radius in gizmos");
            m_PositionSearch_BuildColliderColorProperty = p_SerializedObject.FindProperty("m_PositionSearch_BuildColliderColor");

            m_PositionSearch_CheckedBESnapPointsProperty = p_SerializedObject.FindProperty("m_PositionSearch_CheckedBESnapPoints");
            m_PositionSearch_CheckedBESnapPointsGUIContent = new GUIContent("Gizmos Draw Checked BE SnapPoints", "Draw the checked SnapPoints");
            m_PositionSearch_CheckedBESnapPointsColorProperty = p_SerializedObject.FindProperty("m_PositionSearch_CheckedBESnapPointsColor");
            m_PositionSearch_SnapPointsAreaProperty = p_SerializedObject.FindProperty("m_PositionSearch_SnapPointsArea");
            m_PositionSearch_SnapPointsAreaGUIContent = new GUIContent("SnapPoints Area", "Draw SnapPoints Area Property in %");

            //Position Validation
            m_PositionValidation_AirBuildingMaximumRangeProperty = p_SerializedObject.FindProperty("m_PositionValidation_AirBuildingMaximumRange");
            m_PositionValidation_AirBuildingMaximumRangeGUIContent = new GUIContent("Air Building Maximum Range",
                "If the building in the air is supported and it's maximum range is set then this gizmos will draw a line reprezenting the rajast check.");
            m_PositionValidation_BuildableGroundProperty = p_SerializedObject.FindProperty("m_PositionValidation_BuildableGround");
            m_PositionValidation_BuildableGroundGUIContent = new GUIContent("BuildableGround Raycasts");

            //Drag Building
            m_DragBuilding_IndexProperty = p_SerializedObject.FindProperty("m_DragBuilding_Index");
            m_DragBuilding_IndexGUIContent = new GUIContent("Drag Building Index");

            //Building Element
            m_BuildingElement_StabilityProperty = p_SerializedObject.FindProperty("m_BuildingElement_Stability");
            m_BuildingElement_StabilityGUIContent = new GUIContent("Show BuildingElement Stability");
            m_BuildingElement_StabilityWhenSelectedProperty = p_SerializedObject.FindProperty("m_BuildingElement_StabilityWhenSelected");
            m_BuildingElement_StabilityWhenSelectedGUIContent = new GUIContent("Only for selected Elements");

            //BuildingArea
            m_BuildingArea_AreaColliderProperty = p_SerializedObject.FindProperty("m_BuildingArea_AreaCollider");
            m_BuildingArea_AreaColliderGUIContent = new GUIContent("Building Area Collider", "Draw the Build Area Collider");
            m_BuildingArea_AreaColliderColorProperty = p_SerializedObject.FindProperty("m_BuildingArea_AreaColliderColor");
        }

        protected override ABS_ProjectSettings GetSettings()
        {
            return ABS_ProjectSettingsGetter.GetSettings();
        }

        protected override void OnGUIImpl(string searchContext)
        {
            if (m_EditorStyleContainer == null)
            {
                m_EditorStyleContainer = ScriptableObject.CreateInstance<ABS_EditorStyleContainer>();
                m_EditorStyleContainer.Init();
            }
            m_TabView.Show(m_EditorStyleContainer);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // ABS_EditorSettingsProviderBase Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Implementation of the GUI
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void AddDebugProperties()
        {
            EditorGUILayout.LabelField("Position Search Process", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_PositionSearchProcess_ResultProperty,
                    m_PositionSearchProcess_ResultGUIContent,
                    230);
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("DragBuilding Process", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_DragBuilding_AdvancedGridValidationProcessProperty,
                    m_DragBuilding_AdvancedGridValidationProcessGUIContent,
                    230);
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void AddGizmosProperties()
        {
            RaycastGizmos();
            ABS_EditorUtils.Space(10);

            SearchProcessGizmos();
            ABS_EditorUtils.Space(10);

            ValidationProcessGizmos();
            ABS_EditorUtils.Space(10);

            DragBuildingGizmos();
            ABS_EditorUtils.Space(10);

            BuildingElementGizmos();
            ABS_EditorUtils.Space(10);

            BuildingAreaGizmos();
        }

        private void RaycastGizmos()
        {
            EditorGUILayout.LabelField("Raycast Gizmos", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_BooleanWithColor(
                    m_Raycast_LineProperty,
                    m_Raycast_LineGUIContent,
                    m_Raycast_LineColorProperty,
                    230);

                ABS_EditorUtilsSpecial.AddPropertyField_BooleanWithColor(
                    m_Raycast_HitpointProperty,
                    m_Raycast_HitpointGUIContent,
                    m_Raycast_HitpointColorProperty,
                    230);
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void SearchProcessGizmos()
        {
            EditorGUILayout.LabelField("Position Search Process Gizmos", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_BooleanWithColor(
                    m_PositionSearch_SearchColliderProperty,
                    m_PositionSearch_SearchColliderGUIContent,
                    m_PositionSearch_SearchColliderColorProperty,
                    230);

                ABS_EditorUtilsSpecial.AddPropertyField_BooleanWithColor(
                    m_PositionSearch_BuildColliderProperty,
                    m_PositionSearch_BuildColliderGUIContent,
                    m_PositionSearch_BuildColliderColorProperty,
                    230);

                if (m_PositionSearch_BuildColliderProperty.boolValue)
                {
                    ABS_EditorUtilsSpecial.AddPropertyField_BooleanWithColor(
                        m_PositionSearch_CheckedBESnapPointsProperty,
                        m_PositionSearch_CheckedBESnapPointsGUIContent,
                        m_PositionSearch_CheckedBESnapPointsColorProperty,
                        230);

                    if (m_PositionSearch_CheckedBESnapPointsProperty.boolValue)
                    {
                        ABS_EditorUtils.AddPropertyField(m_PositionSearch_SnapPointsAreaProperty, m_PositionSearch_SnapPointsAreaGUIContent);
                    }
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void ValidationProcessGizmos()
        {
            EditorGUILayout.LabelField("Position Validation Process Gizmos", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_PositionValidation_AirBuildingMaximumRangeProperty,
                    m_PositionValidation_AirBuildingMaximumRangeGUIContent,
                    230);

                ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_PositionValidation_BuildableGroundProperty,
                    m_PositionValidation_BuildableGroundGUIContent,
                    230);
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void DragBuildingGizmos()
        {
            EditorGUILayout.LabelField("DragBuilding Gizmos", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_DragBuilding_IndexProperty,
                    m_DragBuilding_IndexGUIContent,
                    230);
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void BuildingElementGizmos()
        {
            EditorGUILayout.LabelField("BuildingElement Gizmos", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_BuildingElement_StabilityProperty,
                    m_BuildingElement_StabilityGUIContent,
                    230);

                if (m_BuildingElement_StabilityProperty.boolValue)
                {
                    ABS_EditorUtilsSpecial.AddPropertyField_Boolean(
                    m_BuildingElement_StabilityWhenSelectedProperty,
                    m_BuildingElement_StabilityWhenSelectedGUIContent,
                    230);
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void BuildingAreaGizmos()
        {
            EditorGUILayout.LabelField("BuildingArea Gizmos", m_EditorStyleContainer.HeadStyleGizmos);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtilsSpecial.AddPropertyField_BooleanWithColor(
                    m_BuildingArea_AreaColliderProperty,
                    m_BuildingArea_AreaColliderGUIContent,
                    m_BuildingArea_AreaColliderColorProperty,
                    230);

            }
            ABS_EditorUtils.BoxEnd();
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Implementation of the GUI
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
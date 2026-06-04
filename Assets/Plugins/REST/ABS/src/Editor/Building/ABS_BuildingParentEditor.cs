//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.AdvancedBuildSystem;
using System.Collections.Generic;
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingParent))]
    internal class ABS_BuildingParentEditor : ABS_EditorBase
    {
        private SerializedProperty m_PrefabGuidProperty;
        private GUIContent m_PrefabGuidGUIContent;
        private SerializedProperty m_InstanceGuidProperty;
        private GUIContent m_InstanceGuidGUIContent;
        private SerializedProperty m_FixedInstanceGuidProperty;
        private GUIContent m_FixedInstanceGuidGUIContent;

        private SerializedProperty m_GlobalBasicGridParentProperty;
        private GUIContent m_GlobalBasicGridParentGUIContent;
        private SerializedProperty m_GlobalFreeParentProperty;
        private GUIContent m_GlobalFreeParentGUIContent;

        private SerializedProperty m_DefaultBuildingNameProperty;
        private GUIContent m_DefaultBuildingNameGUIContent;
        private SerializedProperty m_EnableCacheProperty;
        private GUIContent m_EnableCacheGUIContent;

        private SerializedProperty m_AdvancedGridStabilityEnabledProperty;
        private GUIContent m_AdvancedGridStabilityEnabledGUIContent;
        private SerializedProperty m_AdvancedGridStabilityLevelProperty;
        private GUIContent m_AdvancedGridStabilityLevelGUIContent;

        private SerializedProperty m_MaximumElementFreeBuildingProperty;
        private GUIContent m_MaximumElementFreeBuildingGUIContent;
        private SerializedProperty m_MaximumElementBasicGridBuildingProperty;
        private GUIContent m_MaximumElementBasicGridBuildingGUIContent;
        private SerializedProperty m_MaximumElementAdvancedGridBuildingProperty;
        private GUIContent m_MaximumElementAdvancedGridBuildingGUIContent;
        private SerializedProperty m_MaximumElementSnapPointBasedBuidlingProperty;
        private GUIContent m_MaximumElementSnapPointBasedBuidlingGUIContent;

        private SerializedProperty m_UpperRangeLimitProperty;
        private SerializedProperty m_UnderRangeLimitProperty;
        private SerializedProperty m_SideRangetLimitProperty;
        private SerializedProperty m_UpperRangeLimitEnabledProperty;
        private SerializedProperty m_UnderRangeLimitEnabledProperty;
        private SerializedProperty m_SideRangetLimitEnabledProperty;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  EditorBase Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Building Parent";
        }

        protected override void OnEnableImpl()
        {
            m_PrefabGuidProperty = serializedObject.FindProperty("m_PrefabGuid");
            m_PrefabGuidGUIContent = new GUIContent("Prefab Guid",
                "A unique identifier of the prefab. Two BuildingParents are equal from the POV of the AdvancedBuildingSystem if their Prefab Guids are equal.");
            m_InstanceGuidProperty = serializedObject.FindProperty("m_InstanceGuid");
            m_InstanceGuidGUIContent = new GUIContent("Instance Guid",
                "A unique identifier of the BuildingParent's instance.");
            m_FixedInstanceGuidProperty = serializedObject.FindProperty("m_FixedInstanceGuid");
            m_FixedInstanceGuidGUIContent = new GUIContent("Fixed InstanceGuid",
                "If the booldean is true then the InstanceGuide will be fixed and not generated." +
                " If the boolean is false then the IntanceGuide Will be generated.");

            m_GlobalBasicGridParentProperty = serializedObject.FindProperty("m_GlobalBasicGridParent");
            m_GlobalBasicGridParentGUIContent = new GUIContent("Global BasicGrid Parent", "The Basic Grid Building algorihtm using a global parent. " +
                "All of tha palced element will be that building's child.");
            m_GlobalFreeParentProperty = serializedObject.FindProperty("m_GlobalFreeParent");
            m_GlobalFreeParentGUIContent = new GUIContent("Global Free Parent", "The Free algorihtm using a global parent. " +
                "All of tha palced element will be that building's child.");

            m_DefaultBuildingNameProperty = serializedObject.FindProperty("m_DefaultBuildingName");
            m_DefaultBuildingNameGUIContent = new GUIContent("New Buildings Default Name", "Every new building will got this name.");
            m_EnableCacheProperty = serializedObject.FindProperty("m_EnableCache");
            m_EnableCacheGUIContent = new GUIContent("Enable Cache", "Automatically enable or disable the caching of the newly created Buidlings.");

            m_AdvancedGridStabilityEnabledProperty = serializedObject.FindProperty("m_AdvancedGridStabilityEnabled");
            m_AdvancedGridStabilityEnabledGUIContent = new GUIContent("Advanced Grid Stability Feature Enabled", "Enable the Stability feature for the AdvancedGridBuilding");
            m_AdvancedGridStabilityLevelProperty = serializedObject.FindProperty("m_AdvancedGridStabilityLevel");
            m_AdvancedGridStabilityLevelGUIContent = new GUIContent("Advanced Grid Stability Level", "The Stability level for the Stability feature of the AdvancedGridBuilding");

            m_MaximumElementFreeBuildingProperty = serializedObject.FindProperty("m_MaximumElementFreeBuilding");
            m_MaximumElementFreeBuildingGUIContent = new GUIContent("Maximum Element For FreeBuilding", "The Maximum BuildingElement count for FreeBuilding Buildings.");
            m_MaximumElementBasicGridBuildingProperty = serializedObject.FindProperty("m_MaximumElementBasicGridBuilding");
            m_MaximumElementBasicGridBuildingGUIContent = new GUIContent("Maximum Element For BasicGridBuilding", "The Maximum BuildingElement count for BasicGridBuilding Buildings.");
            m_MaximumElementAdvancedGridBuildingProperty = serializedObject.FindProperty("m_MaximumElementAdvancedGridBuilding");
            m_MaximumElementAdvancedGridBuildingGUIContent = new GUIContent("Maximum Element For AdvancedGridBuilding", "The Maximum BuildingElement count for AdvancedGridBuilding Buildings.");
            m_MaximumElementSnapPointBasedBuidlingProperty = serializedObject.FindProperty("m_MaximumElementSnapPointBasedBuidling");
            m_MaximumElementSnapPointBasedBuidlingGUIContent = new GUIContent("Maximum Element For SnapPointBasedBuidling", "The Maximum BuildingElement count for SnapPointBasedBuidling Buildings.");

            m_UpperRangeLimitProperty = serializedObject.FindProperty("m_UpperRangeLimit");
            m_UnderRangeLimitProperty = serializedObject.FindProperty("m_UnderRangeLimit");
            m_SideRangetLimitProperty = serializedObject.FindProperty("m_SideRangetLimit");
            m_UpperRangeLimitEnabledProperty = serializedObject.FindProperty("m_UpperRangeLimitEnabled");
            m_UnderRangeLimitEnabledProperty = serializedObject.FindProperty("m_UnderRangeLimitEnabled");
            m_SideRangetLimitEnabledProperty = serializedObject.FindProperty("m_SideRangetLimitEnabled");
        }

        protected override void OnInspectorGUIImpl()
        {
            List<ABS_BuildingParent> targets = GetSelectedTargetsComponents<ABS_BuildingParent>();

            ABS_EditorUtils.AddGuidFieldWithCreateButton<ABS_BuildingParent>(
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "New Prefab Guid",
                m_PrefabGuidProperty,
                m_PrefabGuidGUIContent,
                targets,
                GuidSetterForPrefab);

            if (targets.Count > 1)
            {
                EditorGUILayout.LabelField($"{m_FixedInstanceGuidGUIContent}  :  Multiply target, can't show the Guid!");
            }
            else
            {
                ABS_EditorUtils.AddPropertyField(m_FixedInstanceGuidProperty, m_FixedInstanceGuidGUIContent);
                if (m_FixedInstanceGuidProperty.boolValue)
                {
                    ABS_EditorUtils.AddGuidFieldWithCreateButton<ABS_BuildingParent>(
                        m_EditorStyleContainer.SmallDarkButtonStyle,
                        "New Instance Guid",
                        m_InstanceGuidProperty,
                        m_InstanceGuidGUIContent,
                        targets,
                        GuidSetterForInstance);
                }
            }

            ABS_EditorUtils.AddSeparatorLine();
            AddDataSection();
            ABS_EditorUtils.AddSeparatorLine();
            AddActionSection();
        }

        private void GuidSetterForPrefab(string p_Guid, ABS_BuildingParent p_Target)
        {
            p_Target.PrefabGuid = p_Guid;
            REST_Logging.Info("ABS_BuildingParentEditor", $"New Prefab Guid for {target.name} : {p_Guid}");
        }

        private void GuidSetterForInstance(string p_Guid, ABS_BuildingParent p_Target)
        {
            p_Target.InstanceGuid = p_Guid;
            REST_Logging.Info("ABS_BuildingParentEditor", $"New Instance Guid for {target.name} : {p_Guid}");
        }

        private void AddDataSection ()
        {
            ABS_EditorUtils.AddPropertyField(m_GlobalBasicGridParentProperty, m_GlobalBasicGridParentGUIContent);
            ABS_EditorUtils.AddPropertyField(m_GlobalFreeParentProperty, m_GlobalFreeParentGUIContent);
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_DefaultBuildingNameProperty, m_DefaultBuildingNameGUIContent);
            ABS_EditorUtils.AddPropertyField(m_EnableCacheProperty, m_EnableCacheGUIContent);
            ABS_EditorUtils.AddSeparatorLine();
            ABS_EditorUtils.AddPropertyField(m_AdvancedGridStabilityEnabledProperty, m_AdvancedGridStabilityEnabledGUIContent);
            if (m_AdvancedGridStabilityEnabledProperty.boolValue)
            {
                ABS_EditorUtils.AddPropertyField(m_AdvancedGridStabilityLevelProperty, m_AdvancedGridStabilityLevelGUIContent);
            }
            ABS_EditorUtils.AddSeparatorLine();
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_MaximumElementFreeBuildingProperty, m_MaximumElementFreeBuildingGUIContent);
            ABS_EditorUtils.AddPropertyField(m_MaximumElementBasicGridBuildingProperty, m_MaximumElementBasicGridBuildingGUIContent);
            ABS_EditorUtils.AddPropertyField(m_MaximumElementAdvancedGridBuildingProperty, m_MaximumElementAdvancedGridBuildingGUIContent);
            ABS_EditorUtils.AddPropertyField(m_MaximumElementSnapPointBasedBuidlingProperty, m_MaximumElementSnapPointBasedBuidlingGUIContent);
            ABS_EditorUtils.Space();

            EditorGUILayout.PropertyField(m_UpperRangeLimitEnabledProperty);
            if (m_UpperRangeLimitEnabledProperty.boolValue)
            {
                EditorGUILayout.PropertyField(m_UpperRangeLimitProperty);
                if (m_UpperRangeLimitProperty.floatValue < 0f)
                {
                    EditorGUILayout.HelpBox("The Upper Range should be above zero", MessageType.Error);
                }
            }
            EditorGUILayout.PropertyField(m_UnderRangeLimitEnabledProperty);
            if (m_UnderRangeLimitEnabledProperty.boolValue)
            {
                EditorGUILayout.PropertyField(m_UnderRangeLimitProperty);
                if (m_UnderRangeLimitProperty.floatValue > 0f)
                {
                    EditorGUILayout.HelpBox("The Under Range should be under zero", MessageType.Error);
                }
            }
            EditorGUILayout.PropertyField(m_SideRangetLimitEnabledProperty);
            if (m_SideRangetLimitEnabledProperty.boolValue)
            {
                EditorGUILayout.PropertyField(m_SideRangetLimitProperty);
                if (m_SideRangetLimitProperty.floatValue < 0f)
                {
                    EditorGUILayout.HelpBox("The Side Range should be above zero", MessageType.Error);
                }
            }
        }

        private void AddActionSection ()
        {
            ABS_EditorUtils.StartHorizontal();
            ABS_EditorUtils.FlexibleSpace();
            {
                ABS_BuildingParent buildingParent = target as ABS_BuildingParent;
                bool buttonResult = GUILayout.Button("Init Free Building Parent", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(200));
                if (buttonResult)
                {
                    ABS_Building building = buildingParent.GetFreeBuildingParent();
                    ABS_EditorUtils.Dirty(building);
                }

                ABS_EditorUtils.HorizontalSpace(20);

                buttonResult = GUILayout.Button("Init BasicGrid Building Parent", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(200));
                if (buttonResult)
                {
                    ABS_Building building = buildingParent.GetBasicGridParent();
                    ABS_EditorUtils.Dirty(building);
                }
            }
            ABS_EditorUtils.FlexibleSpace();
            ABS_EditorUtils.EndHorizontal();

            ABS_EditorUtils.Space(5);

            ABS_EditorUtils.StartHorizontal();
            ABS_EditorUtils.FlexibleSpace();
            {
                AddSaveToFileButton();
                ABS_EditorUtils.HorizontalSpace(20);
                SaveToPrefabButton();
            }
            ABS_EditorUtils.FlexibleSpace();
            ABS_EditorUtils.EndHorizontal();
        }

        private void AddSaveToFileButton()
        {
            bool buttonResult = GUILayout.Button("Save To File", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                foreach (ABS_BuildingParent target in GetSelectedTargetsComponents<ABS_BuildingParent>())
                {
                    string dataToSave = target.GetPersistedData().ToJSON(false);
                    string defaultName = target.name;
                    ABS_EditorStorageManager.SavePersistedDataFile(defaultName, dataToSave);
                }
            }
        }

        private void SaveToPrefabButton()
        {
            bool buttonResult = GUILayout.Button("Save To Prefab", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                foreach (ABS_BuildingParent target in GetSelectedTargetsComponents<ABS_BuildingParent>())
                {
                    if (EditorApplication.isPlaying)
                    {
                        //target.SetMaterialToDefault();
                    }

                    //ABS_EditorStorageManager.SaveObjectAsPrefab(target.gameObject, target.name);

                    if (EditorApplication.isPlaying)
                    {
                       // target.SetMaterialBasedOnState();
                    }
                }
            }
        }

        public static ABS_BuildingParent CreateBuildingParent()
        {
            GameObject buildingParentObj = new GameObject("ABS_BuildingParent");
            ABS_BuildingParent parent = buildingParentObj.AddComponent<ABS_BuildingParent>();
            parent.GenerateNewPrefabGuid();
            parent.FixedInstanceGuid = true;
            parent.GenerateNewInstanceGuid();

            ABS_BuildingParentEditor.CreateGlobalFreeBuilding(parent);
            ABS_BuildingParentEditor.CreateGlobalBasicGridBuilding(parent);

            ABS_EditorUtils.Dirty(parent);

            return parent;
        }

        public static ABS_FreeBuilding CreateGlobalFreeBuilding (in ABS_BuildingParent p_Parent)
        {
            ABS_FreeBuilding globalFreeParent = p_Parent.GetFreeBuildingParent();
            globalFreeParent.GenerateNewPrefabGuid();
            globalFreeParent.FixedInstanceGuid = true;
            globalFreeParent.GenerateNewInstanceGuid();

            ABS_EditorUtils.Dirty(globalFreeParent);
            ABS_EditorUtils.Dirty(p_Parent);

            return globalFreeParent;
        }

        public static ABS_BasicGridBuilding CreateGlobalBasicGridBuilding(in ABS_BuildingParent p_Parent)
        {
            ABS_BasicGridBuilding globalBasicGridParent = p_Parent.GetBasicGridParent();
            globalBasicGridParent.GenerateNewPrefabGuid();
            globalBasicGridParent.FixedInstanceGuid = true;
            globalBasicGridParent.GenerateNewInstanceGuid();

            ABS_EditorUtils.Dirty(globalBasicGridParent);
            ABS_EditorUtils.Dirty(p_Parent);

            return globalBasicGridParent;
        }
    }
}
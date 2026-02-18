//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;
using NUnit.Framework.Internal;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_Building))]
    [CanEditMultipleObjects]
    internal abstract class ABS_BuildingEditor : ABS_EditorBase
    {
        private SerializedProperty m_PrefabGuidProperty;
        private GUIContent m_PrefabGuidGUIContent;
        private SerializedProperty m_InstanceGuidProperty;
        private GUIContent m_InstanceGuidGUIContent;
        private SerializedProperty m_FixedInstanceGuidProperty;
        private GUIContent m_FixedInstanceGuidGUIContent;

        private SerializedProperty m_MaximumElementCountProperty;
        private GUIContent m_MaximumElementCountGUIContent;

        private SerializedProperty m_EnableCacheProperty;
        private GUIContent m_EnableCacheGUIContent;

        private ABS_BuildingElement m_FindAndReplaceElements_FromElement = null;
        private ABS_BuildingElement m_FindAndReplaceElements_ToElement = null;
        private bool m_FindAndReplaceElementsSectionVariable = false;
        private bool m_FindAndReplaceElementsTypeBasedSectionVariable = false;

        private SerializedProperty m_UpperRangeLimitProperty;
        private SerializedProperty m_UnderRangeLimitProperty;
        private SerializedProperty m_SideRangetLimitProperty;
        private SerializedProperty m_UpperRangeLimitEnabledProperty;
        private SerializedProperty m_UnderRangeLimitEnabledProperty;
        private SerializedProperty m_SideRangetLimitEnabledProperty;

        protected override void OnEnableImpl()
        {
            m_PrefabGuidProperty = serializedObject.FindProperty("m_PrefabGuid");
            m_PrefabGuidGUIContent = new GUIContent("Prefab Guid",
                "A unique identifier of the prefab. Two Building are equal from the POV of the AdvancedBuildingSystem if their Prefab Guids are equal.");
            m_InstanceGuidProperty = serializedObject.FindProperty("m_InstanceGuid");
            m_InstanceGuidGUIContent = new GUIContent("Instance Guid",
                "A unique identifier of the Building's instance.");
            m_FixedInstanceGuidProperty = serializedObject.FindProperty("m_FixedInstanceGuid");
            m_FixedInstanceGuidGUIContent = new GUIContent("Fixed InstanceGuid",
                "If the booldean is true then the InstanceGuide will be fixed and not generated." +
                " If the boolean is false then the IntanceGuide Will be generated.");

            m_MaximumElementCountProperty = serializedObject.FindProperty("m_MaximumElementCount");
            m_MaximumElementCountGUIContent = new GUIContent("Maximum Element Count", "The Maximum ABS_BuildingElement Count of the Building.");

            m_EnableCacheProperty = serializedObject.FindProperty("m_EnableCache");
            m_EnableCacheGUIContent = new GUIContent("Use Cache", "Enable or disable the cacheing mechanism of the Building.");

            m_UpperRangeLimitProperty = serializedObject.FindProperty("m_UpperRangeLimit");
            m_UnderRangeLimitProperty = serializedObject.FindProperty("m_UnderRangeLimit");
            m_SideRangetLimitProperty = serializedObject.FindProperty("m_SideRangetLimit");
            m_UpperRangeLimitEnabledProperty = serializedObject.FindProperty("m_UpperRangeLimitEnabled");
            m_UnderRangeLimitEnabledProperty = serializedObject.FindProperty("m_UnderRangeLimitEnabled");
            m_SideRangetLimitEnabledProperty = serializedObject.FindProperty("m_SideRangetLimitEnabled");
        }

        protected override void OnInspectorGUIImpl()
        {
            List<ABS_Building> targets = GetSelectedTargetsComponents<ABS_Building>();

            ABS_EditorUtils.AddGuidFieldWithCreateButton<ABS_Building>(
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "New Prefab Guid",
                m_PrefabGuidProperty,
                m_PrefabGuidGUIContent,
                targets,
                GuidSetterForPrefab);

            ABS_EditorUtils.AddPropertyField(m_FixedInstanceGuidProperty, m_FixedInstanceGuidGUIContent);
            if (targets.Count > 1)
            {
                EditorGUILayout.LabelField($"{m_FixedInstanceGuidGUIContent}  :  Multiply target, can't show the Guid!");
            }
            else
            {
                if (m_FixedInstanceGuidProperty.boolValue)
                {
                    ABS_EditorUtils.AddGuidFieldWithCreateButton<ABS_Building>(
                        m_EditorStyleContainer.SmallDarkButtonStyle,
                        "New Instance Guid",
                        m_InstanceGuidProperty,
                        m_InstanceGuidGUIContent,
                        targets,
                        GuidSetterForInstance);
                }
            }

            ABS_EditorUtils.AddSeparatorLine();

            ABS_EditorUtils.AddPropertyField(m_MaximumElementCountProperty, m_MaximumElementCountGUIContent);
            ABS_EditorUtils.Space(5);

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

            ABS_EditorUtils.Space(5);
            ABS_EditorUtils.AddSeparatorLine();

            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Cache", m_EditorStyleContainer.HeadStyleSectionGroup, GUILayout.Width(150));

                AddMakeCacheButton();
                ABS_EditorUtils.Space(10);
                AddCheckCacheSizeButton();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            ABS_EditorUtils.AddPropertyField(m_EnableCacheProperty, m_EnableCacheGUIContent);
            ABS_EditorUtils.Space(5);

            /*GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("PreBuilt", m_EditorStyleContainer.HeadStyleSectionGroup, GUILayout.Width(150));

                AddMakePreBuiltButton();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();*/
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Modifications", m_EditorStyleContainer.HeadStyleSectionGroup, GUILayout.Width(150));

                AddFindAndReplaceButton();

                ABS_EditorUtils.Space(10);

                ABS_PositionSearchAlgorithm algorithType = (targets[0]).PositionSearchAlgorithmType;
                if (algorithType == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    AddFindAndReplaceTypeBasedButton();
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            if (m_FindAndReplaceElementsSectionVariable)
            {
                ABS_EditorUtils.Space();
                FindAllAndReplaceElements();
                ABS_EditorUtils.Space(10);
            }

            if (m_FindAndReplaceElementsTypeBasedSectionVariable)
            {
                ABS_EditorUtils.Space();
                FindAllAndReplaceElementsTypeBased();
                ABS_EditorUtils.Space(10);
            }

            ABS_EditorUtils.Space();
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Persistency", m_EditorStyleContainer.HeadStyleSectionGroup, GUILayout.Width(150));

                AddSaveToFileButton();
                GUILayout.Space(10);
                SaveToPrefabButton();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }
        
        private void GuidSetterForPrefab(string p_Guid, ABS_Building p_Target)
        {
            p_Target.PrefabGuid = p_Guid;
            REST_Logging.Info("ABS_BuildingEditor", $"New Prefab Guid for {target.name} : {p_Guid}");
        }

        private void GuidSetterForInstance(string p_Guid, ABS_Building p_Target)
        {
            p_Target.InstanceGuid = p_Guid;
            REST_Logging.Info("ABS_BuildingEditor", $"New Instance Guid for {target.name} : {p_Guid}");
        }

        private void SaveToPrefabButton()
        {
            bool buttonResult = GUILayout.Button("Save To Prefab", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                {
                    if (EditorApplication.isPlaying)
                    {
                        target.SetMaterialToDefault();
                    }

                    ABS_EditorStorageManager.SaveObjectAsPrefab(target.gameObject, target.name);

                    if (EditorApplication.isPlaying)
                    {
                        target.SetMaterialBasedOnState();
                    }
                }
            }
        }

        private void AddMakePreBuiltButton()
        {
            bool buttonResult = GUILayout.Button("Make PreBuilt", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                if (ShowAffirmation("Would you like to change every BuildingElement to PreBuilt?"))
                {
                    foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                    {
                        target.MakePreBuilt();
                        ABS_EditorUtils.Dirty(target);
                    }
                }
            }

            GUILayout.Space(10);

            buttonResult = GUILayout.Button("Remove PreBuilt", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                if (ShowAffirmation("Would you like to remove the PreBuilt state from every BuildingElement?"))
                {
                    foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                    {
                        target.RemovePreBuilt();
                        ABS_EditorUtils.Dirty(target);
                    }
                }
            }
        }

        private void AddSaveToFileButton ()
        {
            bool buttonResult = GUILayout.Button("Save To File", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                {
                    string dataToSave = target.ToJSON(false);
                    string defaultName = target.name;
                    ABS_EditorStorageManager.SavePersistedDataFile(defaultName, dataToSave);
                }
            }
        }        

        private void AddMakeCacheButton()
        {
            bool buttonResult = GUILayout.Button("Clear Cache", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                {
                    target.ClearCache();
                }
            }
        } 
        
        private void AddCheckCacheSizeButton()
        {
            bool buttonResult = GUILayout.Button("Check Cache Size", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
            if (buttonResult)
            {
                foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                {
                    target.CheckCacheSize();
                }
            }
        }

        private void AddFindAndReplaceButton()
        {
            bool buttonResult = GUILayout.Button(
                "Find All And Replace",
                m_FindAndReplaceElementsSectionVariable ? m_EditorStyleContainer.SmallBlueButtonStyle : m_EditorStyleContainer.SmallDarkButtonStyle, 
                GUILayout.Width(120));
            if (buttonResult)
            {
                m_FindAndReplaceElementsSectionVariable = !m_FindAndReplaceElementsSectionVariable;
                m_FindAndReplaceElementsTypeBasedSectionVariable = false;
            }
        }

        private void AddFindAndReplaceTypeBasedButton()
        {
            bool buttonResult = GUILayout.Button(
                "Find All And Replace Type Based",
                m_FindAndReplaceElementsTypeBasedSectionVariable ? m_EditorStyleContainer.SmallBlueButtonStyle : m_EditorStyleContainer.SmallDarkButtonStyle, 
                GUILayout.Width(180));
            if (buttonResult)
            {
                m_FindAndReplaceElementsTypeBasedSectionVariable = !m_FindAndReplaceElementsTypeBasedSectionVariable;
                m_FindAndReplaceElementsSectionVariable = false;
            }
        }

        private void FindAllAndReplaceElements ()
        {
            m_FindAndReplaceElements_FromElement = ABS_EditorUtils.AddObjectField<ABS_BuildingElement>("From :", m_FindAndReplaceElements_FromElement, true);
            m_FindAndReplaceElements_ToElement = ABS_EditorUtils.AddObjectField<ABS_BuildingElement>("To :", m_FindAndReplaceElements_ToElement, true);

            List<ABS_Building> targets = GetSelectedTargetsComponents<ABS_Building>();
            ABS_PositionSearchAlgorithm algorithType = (targets[0]).PositionSearchAlgorithmType;

            foreach (ABS_Building target in targets)
            {
                if (algorithType != target.PositionSearchAlgorithmType)
                {
                    EditorGUILayout.HelpBox("Not every ABS_Building has the same Type", MessageType.Error);
                    return;
                }
            }

            bool issue = false;
            if (m_FindAndReplaceElements_FromElement == null)
            {
                EditorGUILayout.HelpBox("Missing \"From\" Element!", MessageType.Error);
                issue = true;
            }

            if (m_FindAndReplaceElements_ToElement == null)
            {
                EditorGUILayout.HelpBox("Missing \"To\" Element!", MessageType.Error);
                issue = true;
            }

            if (issue)
            {
                return;
            }

            if (m_FindAndReplaceElements_FromElement.PositionSearchAlgorithm != algorithType)
            {
                EditorGUILayout.HelpBox("The \"From\" Element has different main algorithm type as the Buidling!", MessageType.Error);
                issue = true;
            }

            if (m_FindAndReplaceElements_ToElement.PositionSearchAlgorithm != algorithType)
            {
                EditorGUILayout.HelpBox("The \"To\" Element has different main algorithm type as the Buidling!", MessageType.Error);
                issue = true;
            }

            if (algorithType == ABS_PositionSearchAlgorithm.AdvancedGrid)
            {
                ABS_AdvancedGridType fromType = m_FindAndReplaceElements_FromElement.AdvancedGridType;
                ABS_AdvancedGridType toType = m_FindAndReplaceElements_ToElement.AdvancedGridType;
                if (fromType != toType)
                {
                    EditorGUILayout.HelpBox($"The Advanced Grid Types are not matching. From : {fromType}, To : {toType}", MessageType.Error);
                    issue = true;
                } 

                if (fromType == ABS_AdvancedGridType.Wall || fromType == ABS_AdvancedGridType.EdgeHorizontal)
                {
                    ABS_AdvancedGridAxisType fromAxisType = m_FindAndReplaceElements_FromElement.AdvancedGridAxisType;
                    ABS_AdvancedGridAxisType toAxisType = m_FindAndReplaceElements_ToElement.AdvancedGridAxisType;
                    if (fromAxisType != toAxisType)
                    {
                        EditorGUILayout.HelpBox($"The Advanced Grid Axis Types are not matching. From : {fromAxisType}, To : {toAxisType}", MessageType.Error);
                        issue = true;
                    }
                }
            }

            if (issue)
            {
                return;
            }

            ABS_EditorUtils.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            {
                bool buttonResult = GUILayout.Button("Execute",m_EditorStyleContainer.SmallGreenButtonStyle, GUILayout.Width(120));
                if (buttonResult)
                {
                    foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                    {
                        List<(ABS_BuildingElement, ABS_BuildingElement)> res = target.FindAllAndReplaceElements(m_FindAndReplaceElements_FromElement, m_FindAndReplaceElements_ToElement, true);
                        if (res == null)
                        {
                            REST_Logging.Info("ABS_BuildingEditor", "FindAllAndReplaceElements failed!");
                        }
                        else if (res.Count == 0)
                        {
                            REST_Logging.Info("ABS_BuildingEditor", "No element had been replaced!");
                        }
                        else
                        {
                            REST_Logging.Info("ABS_BuildingEditor", $"Replaced element count : {res.Count}");
                        }
                        ABS_EditorUtils.Dirty(target);
                    }
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        private void FindAllAndReplaceElementsTypeBased()
        {
            m_FindAndReplaceElements_ToElement = ABS_EditorUtils.AddObjectField<ABS_BuildingElement>("Replalce target element :", m_FindAndReplaceElements_ToElement, true);

            List<ABS_Building> targets = GetSelectedTargetsComponents<ABS_Building>();
            ABS_PositionSearchAlgorithm algorithType = (targets[0]).PositionSearchAlgorithmType;

            foreach (ABS_Building target in targets)
            {
                if (algorithType != target.PositionSearchAlgorithmType)
                {
                    EditorGUILayout.HelpBox("Not every ABS_Building has the same Type", MessageType.Error);
                    return;
                }
            }

            if (m_FindAndReplaceElements_ToElement == null)
            {
                EditorGUILayout.HelpBox("Missing Element!", MessageType.Error);
                return;
            }

            if (m_FindAndReplaceElements_ToElement.PositionSearchAlgorithm != algorithType)
            {
                EditorGUILayout.HelpBox("The Element has different main algorithm type as the Buidling!", MessageType.Error);
                return;
            }

            ABS_EditorUtils.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            {
                bool buttonResult = GUILayout.Button("Execute", m_EditorStyleContainer.SmallGreenButtonStyle, GUILayout.Width(120));
                if (buttonResult)
                {
                    foreach (ABS_Building target in GetSelectedTargetsComponents<ABS_Building>())
                    {
                        ABS_AdvancedGridBuilding advancedGridBuidlingTarget = target as ABS_AdvancedGridBuilding;
                        List<(ABS_BuildingElement, ABS_BuildingElement)> res = advancedGridBuidlingTarget.FindAllAndReplaceElementsTypeBased(m_FindAndReplaceElements_ToElement, true);
                        if (res == null)
                        {
                            REST_Logging.Info("ABS_BuildingEditor", "FindAllAndReplaceElements failed!");
                        }
                        else if (res.Count == 0)
                        {
                            REST_Logging.Info("ABS_BuildingEditor", "No element had been replaced!");
                        }
                        else
                        {
                            REST_Logging.Info("ABS_BuildingEditor", $"Replaced element count : {res.Count}");
                        }
                        ABS_EditorUtils.Dirty(target);
                    }
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static BuildingType CreateBuilding<BuildingType>(in string p_BuildingName)
            where BuildingType : ABS_Building
        {
            GameObject buildingManager = new GameObject(p_BuildingName);
            BuildingType component = buildingManager.AddComponent<BuildingType>();
            component.GenerateNewPrefabGuid();
            component.FixedInstanceGuid = true;
            component.GenerateNewInstanceGuid();
            return component;
        }
    }
}
//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingAreaRuleset))]
    internal class ABS_BuildingAreaRulesetEditor : ABS_EntityListEditorBase<ABS_BuildingAreaRuleset, ABS_BuildingAreaRule>
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private System.Enum m_PermissionType = ABS_BuildingAreaRule.PermissionType.Allow;
        public List<System.Enum> m_BEAreaTypes = new List<System.Enum>();
        public List<int> m_BEAreaTypesForDelete = new List<int>();

        public List<int> m_BuildingElementObjectsForDelete = new List<int>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "BuildingArea Ruleset";
        }

        protected override void AddEntityEditorSection()
        {
            ABS_BuildingAreaRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_BuildingAreaRule;
            if (rule == null)
            {
                REST_Logging.Error("BuildingAreaRulesetEditor", 
                    $"The rule was null! Wrong index! ListSize: {m_EntityListHolder.Rules.Count} | Index: {m_EntityIndexForEdit}");
                m_State = State.EditMode;
                return;
            }

            rule.m_Name = EditorGUILayout.TextField("Name", rule.m_Name);
            ABS_EditorUtils.Space();

            m_PermissionType = EditorGUILayout.EnumPopup("PermissionType", rule.m_PermissionType);
            if (m_PermissionType.CompareTo(ABS_BuildingAreaRule.PermissionType.Allow) == 0)
            {
                rule.m_PermissionType = ABS_BuildingAreaRule.PermissionType.Allow;
            }
            else if (m_PermissionType.CompareTo(ABS_BuildingAreaRule.PermissionType.Deny) == 0)
            {
                rule.m_PermissionType = ABS_BuildingAreaRule.PermissionType.Deny;
            }

            ABS_EditorUtils.Space();
            rule.m_ScreeningType = ABS_EditorUtils.LayoutEnumPopup<ABS_BuildingAreaRule.ScreeningType>("ScreeningType", rule.m_ScreeningType);
            ABS_EditorUtils.Space();
            if (rule.m_ScreeningType == ABS_BuildingAreaRule.ScreeningType.AreaType)
            {
                AddEntityEditorAreaTypeSection(rule);
            }
            else if (rule.m_ScreeningType == ABS_BuildingAreaRule.ScreeningType.Object)
            {
                AddEntityEditorObjectSection(rule);
            }
        }

        private void AddEntityEditorObjectSection(ABS_BuildingAreaRule p_Rule)
        {
            GUILayout.BeginHorizontal();
            {
                bool NewButtonResult = GUILayout.Button(
                    "New",
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    GUILayout.Width(120)
                );
                if (NewButtonResult)
                {
                    p_Rule.m_BuildingElementObjects.Add(null);
                    EditorUtility.SetDirty(target);
                }
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < p_Rule.m_BuildingElementObjects.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                {
                    p_Rule.m_BuildingElementObjects[i] = ABS_EditorUtils.AddObjectField("BuildingElement Object", p_Rule.m_BuildingElementObjects[i], false);

                    bool deleteButtonResult = GUILayout.Button(
                        "Delete",
                        m_EditorStyleContainer.SmallRedButtonStyle,
                        GUILayout.Width(120)
                    );
                    if (deleteButtonResult)
                    {
                        m_BuildingElementObjectsForDelete.Add(i);
                    }

                }
                GUILayout.EndHorizontal();
            }
            foreach (int idx in m_BuildingElementObjectsForDelete)
            {
                p_Rule.m_BuildingElementObjects.RemoveAt(idx);
                EditorUtility.SetDirty(target);
            }
            m_BuildingElementObjectsForDelete.Clear();
        }

        private void AddEntityEditorAreaTypeSection (ABS_BuildingAreaRule p_Rule)
        {
            GUILayout.BeginHorizontal();
            {
                bool NewButtonResult = GUILayout.Button(
                    "New",
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    GUILayout.Width(120)
                );
                if (NewButtonResult)
                {
                    p_Rule.m_BEAreaTypes.Add(0);
                }
            }
            GUILayout.EndHorizontal();

            m_BEAreaTypes.Clear();
            for (int i = 0; i < p_Rule.m_BEAreaTypes.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                {
                    m_BEAreaTypes.Add(EditorGUILayout.EnumPopup("BuildingElement AreaType", p_Rule.m_BEAreaTypes[i]));

                    ABS_BuildingElementAreaType[] values = (ABS_BuildingElementAreaType[])Enum.GetValues(typeof(ABS_BuildingElementAreaType));
                    foreach (ABS_BuildingElementAreaType type in values)
                    {
                        if (m_BEAreaTypes[i].CompareTo(type) == 0)
                        {
                            p_Rule.m_BEAreaTypes[i] = type;
                            break;
                        }
                    }

                    bool deleteButtonResult = GUILayout.Button(
                        "Delete",
                        m_EditorStyleContainer.SmallRedButtonStyle,
                        GUILayout.Width(120)
                    );
                    if (deleteButtonResult)
                    {
                        m_BEAreaTypesForDelete.Add(i);
                    }

                }
                GUILayout.EndHorizontal();
            }
            foreach (int idx in m_BEAreaTypesForDelete)
            {
                p_Rule.m_BEAreaTypes.RemoveAt(idx);
                EditorUtility.SetDirty(target);
            }
            m_BEAreaTypesForDelete.Clear();
        }

        protected override void AddEntityDataSection(int p_EntityIdx)
        {
            ABS_BuildingAreaRule rule = m_EntityList.GetEntity(p_EntityIdx) as ABS_BuildingAreaRule;

            GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
            string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                ref coloredTextStyle,
                (rule.m_PermissionType == ABS_BuildingAreaRule.PermissionType.Allow ? "Allow" : "Deny"),
                (rule.m_PermissionType == ABS_BuildingAreaRule.PermissionType.Allow ? ABS_EditorStyleContainer.s_GreenColor : ABS_EditorStyleContainer.s_RedColor));
            EditorGUILayout.LabelField($"PermissionType  :  {colorizesString}", coloredTextStyle);

            EditorGUILayout.LabelField($"CollectionType  :  {rule.m_ScreeningType}");

            if (rule.m_ScreeningType == ABS_BuildingAreaRule.ScreeningType.AreaType)
            {
                ABS_EditorUtils.IndentIn();
                {
                    if (rule.m_BEAreaTypes.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Empty Rule!", MessageType.Error);
                    }
                    else
                    {
                        int i = 0;
                        foreach (ABS_BuildingElementAreaType type in rule.m_BEAreaTypes)
                        {
                            EditorGUILayout.LabelField($"({++i})  {type}");
                        }
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
            else if (rule.m_ScreeningType == ABS_BuildingAreaRule.ScreeningType.Object)
            {
                ABS_EditorUtils.IndentIn();
                {
                    if (rule.m_BuildingElementObjects.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Empty Rule!", MessageType.Error);
                    }
                    else
                    {
                        int i = 0;
                        foreach (ABS_BuildingElement element in rule.m_BuildingElementObjects)
                        {
                            if (element != null)
                            {
                                ABS_EditorUtils.AddBuildingElementDataLine(element.gameObject, element.PrefabGuid, $"    {i++} : ");
                            }
                        }
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
        }

        protected override void AddBaseSection ()
        {
            bool permission = m_EntityListHolder.m_BasePermissionType == ABS_BuildingAreaRule.PermissionType.Allow;
            GUILayout.BeginHorizontal();
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                coloredTextStyle.stretchWidth = false;
                coloredTextStyle.alignment = TextAnchor.MiddleLeft;
                coloredTextStyle.fixedWidth = 200;

                string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                    ref coloredTextStyle,
                    (permission ? "Allow" : "Deny"),
                    (permission ? ABS_EditorStyleContainer.s_GreenColor : ABS_EditorStyleContainer.s_RedColor));
                EditorGUILayout.LabelField(
                    $"Base Permission :  {colorizesString}", 
                    coloredTextStyle, 
                    GUILayout.Width(200),
                    GUILayout.MaxWidth(200));

                if(m_State == State.EditMode)
                {
                    if (permission)
                    {
                        bool denyButtonResult = GUILayout.Button(
                            "Deny",
                            m_EditorStyleContainer.SmallRedButtonStyle,
                            GUILayout.Width(40)
                        );
                        if (denyButtonResult)
                        {
                            m_EntityListHolder.m_BasePermissionType = ABS_BuildingAreaRule.PermissionType.Deny;
                            EditorUtility.SetDirty(target);
                        }
                    }
                    else
                    {
                        bool allowButtonResult = GUILayout.Button(
                            "Allow",
                            m_EditorStyleContainer.SmallGreenButtonStyle,
                            GUILayout.Width(40)
                        );
                        if (allowButtonResult)
                        {
                            m_EntityListHolder.m_BasePermissionType = ABS_BuildingAreaRule.PermissionType.Allow;
                            EditorUtility.SetDirty(target);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
            ABS_EditorUtils.IndentIn();
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                    ref coloredTextStyle,
                    (permission ? "Allow Everything" : "Deny Everything"),
                    (permission ? ABS_EditorStyleContainer.s_GreenColor : ABS_EditorStyleContainer.s_RedColor));
                EditorGUILayout.LabelField(colorizesString, coloredTextStyle);
            }
            ABS_EditorUtils.IndentOut();
        }
        protected override bool IsStatic()
        {
            return false;
        }
    }
}
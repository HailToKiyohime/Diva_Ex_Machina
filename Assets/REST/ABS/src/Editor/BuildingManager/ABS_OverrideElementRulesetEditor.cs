//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_OverrideElementRuleset))]
    internal class ABS_OverrideElementRulesetEditor : ABS_EntityListEditorBase<ABS_OverrideElementRuleset, ABS_OverrideElementRule>
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<int> m_BuildingElementObjectsForDelete = new List<int>();

        private GUIContent m_RelationTypeGUIContent;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  EditBase Implementation 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "OverrideElement Ruleset";
        }

        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
            m_RelationTypeGUIContent = new GUIContent("Relation Type");
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_EntityListEditorBase Implementation 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void AddBaseSection() { }
        protected override void AddEntityEditorSection()
        {
            ABS_OverrideElementRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_OverrideElementRule;
            if (rule == null)
            {
                REST_Logging.Error("OverrideElementRulesetEditor", "AddEntityEditorSection", 
                    $"The rule was null! Wrong index! ListSize: {m_EntityListHolder.Rules.Count} | Index: {m_EntityIndexForEdit}");
                m_State = State.EditMode;
                return;
            }

            rule.Name = EditorGUILayout.TextField("Name", rule.Name);
            rule.Type = ABS_EditorUtils.AddEnumPopup(m_RelationTypeGUIContent, rule.Type);

            ABS_EditorUtils.AddSeparatorLine();
            switch (rule.Type)
            {
                case ABS_OverrideElementRule.RelationType.OneToSet:
                    AddEntityEditorDataSection(rule, true, "The target element can be overridden by the following elements.");
                    break;
                case ABS_OverrideElementRule.RelationType.SetToOne:
                    AddEntityEditorDataSection(rule, true, "The following elements can be overridden by the target element.");
                    break;
                case ABS_OverrideElementRule.RelationType.BothWay:
                    AddEntityEditorDataSection(rule, true, 
                        "The target element can be overriden by the following elements." +
                        " or the following elements can be overridden by the target element" +
                        " but the following element can not override eachother");
                    break;
                case ABS_OverrideElementRule.RelationType.Set:
                    AddEntityEditorDataSection(rule, false, "All of the elements can override each other.");
                    break;

            }
        }

        private void AddEntityEditorDataSection(ABS_OverrideElementRule p_Rule, bool p_AddTargetObject, in string Message)
        {
            if (p_AddTargetObject)
            {
                p_Rule.OverrideTarget = ABS_EditorUtils.AddObjectField("Override Target Object", p_Rule.OverrideTarget, false);
                ABS_EditorUtils.Space();
            }

            ABS_EditorUtils.HelpBox(MessageType.Info, Message);
            ABS_EditorUtils.Space();

            GUILayout.BeginHorizontal();
            {
                bool NewButtonResult = GUILayout.Button(
                    "Add element",
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    GUILayout.Width(120)
                );
                if (NewButtonResult)
                {
                    p_Rule.BuildingElementsForChange.Add(null);
                    EditorUtility.SetDirty(target);
                }
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < p_Rule.BuildingElementsForChange.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                {
                    p_Rule.BuildingElementsForChange[i] = ABS_EditorUtils.AddObjectField(
                        "BuildingElement Object", 
                        p_Rule.BuildingElementsForChange[i], 
                        false);

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
                p_Rule.BuildingElementsForChange.RemoveAt(idx);
                EditorUtility.SetDirty(target);
            }
            m_BuildingElementObjectsForDelete.Clear();

            ABS_EditorUtils.AddSeparatorLine();
            Validate(p_Rule);
        }

        private void Validate(ABS_OverrideElementRule p_Rule)
        {
            if (p_Rule.Type != ABS_OverrideElementRule.RelationType.Set)
            {
                if (p_Rule.OverrideTarget == null)
                {
                    EditorGUILayout.HelpBox("Missing Target", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.LabelField($"Override Target Object  :");
                    ABS_EditorUtils.AddBuildingElementDataLine(p_Rule.OverrideTarget.gameObject, p_Rule.OverrideTarget.name, p_Rule.OverrideTarget.PrefabGuid);
                }
            }

            if (p_Rule.BuildingElementsForChange.Count == 0)
            {
                EditorGUILayout.HelpBox("Empty Override Set", MessageType.Error);
            }
            else
            {
                ABS_PositionSearchAlgorithm targetAlg = ABS_PositionSearchAlgorithm.Free; 
                if (p_Rule.Type != ABS_OverrideElementRule.RelationType.Set && p_Rule.OverrideTarget != null)
                {
                    targetAlg = p_Rule.OverrideTarget.PositionSearchAlgorithm;
                }

                foreach (ABS_BuildingElement be in p_Rule.BuildingElementsForChange)
                {
                    if (be == null)
                    {
                        EditorGUILayout.HelpBox("Null Element", MessageType.Error);
                    }
                    else
                    {
                        ABS_EditorUtils.AddBuildingElementDataLine(be.gameObject, be.name, be.PrefabGuid);

                        if (p_Rule.Type != ABS_OverrideElementRule.RelationType.Set 
                            && p_Rule.OverrideTarget != null)
                        {
                            if (be.PositionSearchAlgorithm != targetAlg)
                            {
                                EditorGUILayout.HelpBox("Different algorithm type then the Target", MessageType.Error);
                            }
                        }

                        ABS_EditorUtils.Space();
                    }
                }

                foreach (ABS_BuildingElement be in p_Rule.BuildingElementsForChange)
                {
                    if (be != null)
                    {
                        ABS_PositionSearchAlgorithm setAlg = be.PositionSearchAlgorithm;
                        foreach (ABS_BuildingElement beInt in p_Rule.BuildingElementsForChange)
                        {
                            if (beInt != null && beInt.PositionSearchAlgorithm != setAlg)
                            {
                                EditorGUILayout.HelpBox("Mixed algorihtm type in the set", MessageType.Error);
                                break;
                            }
                        }
                        break;
                    }
                }

                if (p_Rule.BuildingElementsForChange.Count == 1 && p_Rule.Type == ABS_OverrideElementRule.RelationType.Set)
                {
                    EditorGUILayout.HelpBox("Only one element is provided", MessageType.Error);
                }
            }
        }

        protected override void AddEntityDataSection(int p_EntityIdx)
        {
            ABS_OverrideElementRule rule = m_EntityList.GetEntity(p_EntityIdx) as ABS_OverrideElementRule;
            Validate(rule);
        }

        protected override bool IsStatic()
        {
            return false;
        }
    }
}
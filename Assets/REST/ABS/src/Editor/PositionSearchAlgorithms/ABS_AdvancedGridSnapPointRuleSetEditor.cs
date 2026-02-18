//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.Utils;
using log4net.Util;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_AdvancedGridSnapPointRuleSet))]
    internal class ABS_AdvancedGridSnapPointRuleSetEditor 
        : ABS_DrawableScriptableObjectEditor<ABS_AdvancedGridSnapPointRuleSet, ABS_AdvancedGridSnapPointRule>
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum Template_Floor : ushort
        {
            BlockedCorner = 0,
            BlockTwoSide = 1,
        }

        public enum Template_Wall : ushort
        {
            BlockWallSnapping = 0,
        }

        public enum Template_EdgeHorizontal : ushort
        {
            None
        }

        public enum Template_EdgeVertical : ushort
        {
            None
        }

        public enum Template_Corner : ushort
        {
            None
        }

        public enum Template_Center : ushort
        {
            None
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private System.Enum m_BaseType = ABS_AdvancedGridType.Floor;
        private System.Enum m_Permission = ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
        private ABS_AdvancedGridSnapPointRuleSet m_RuleSet = null;

        private ABS_BuildingElement m_TempTargetElement = null;
        private ABS_BuildingElement m_TempTargetElementInstance = null;
        private ABS_AdvancedGridBuilderSettings m_Settings = null;

        private ABS_AdvancedGridSnapPointRule.SnapPoint m_ShowSnapPointTarget = null;

        private bool m_TemplateSectionIsOpened = false;
        private System.Enum m_Template_Floor = Template_Floor.BlockedCorner;
        private System.Enum m_Template_Wall = Template_Wall.BlockWallSnapping;
        private System.Enum m_Template_EdgeHorizontal = Template_EdgeHorizontal.None;
        private System.Enum m_Template_EdgeVertical = Template_EdgeVertical.None;
        private System.Enum m_Template_Corner = Template_Corner.None;
        private System.Enum m_Template_Center = Template_Center.None;

        private bool m_PermissionSectionIsOpened = false;

        private bool m_TransformSectionIsOpened = false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_EditorBase Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public new void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "AdvancedGridSnapPointRuleSet";
        }

        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
            m_RuleSet = m_EntityListHolder as ABS_AdvancedGridSnapPointRuleSet;
        }

        protected override void AddEntityEditorSection()
        {
            ABS_AdvancedGridSnapPointRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule;

            int i = 0;
            foreach (ABS_AdvancedGridSnapPointRule.SnapPoint snappoint in rule.SnapPoints)
            {
                ABS_EditorUtils.StartHorizontal();
                {
                    EditorGUILayout.LabelField($"Position  ({++i})  :  {snappoint.m_AdvancedGridSnapPoint.m_Position}", GUILayout.MaxWidth(220));

                    GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                    string colorizesString = "Permission";
                    switch (snappoint.m_Permisson)
                    {
                        case ABS_AdvancedGridSnapPointRule.PermissionType.Allow:
                            colorizesString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "Permission", ABS_EditorStyleContainer.s_GreenColor);
                            break;
                        case ABS_AdvancedGridSnapPointRule.PermissionType.Block:
                            colorizesString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "Permission", ABS_EditorStyleContainer.s_BlueColor);
                            break;
                        case ABS_AdvancedGridSnapPointRule.PermissionType.Deny:
                            colorizesString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "Permission", ABS_EditorStyleContainer.s_RedColor);
                            break;
                    }

                    EditorGUILayout.LabelField ($"{colorizesString} : ",
                                                coloredTextStyle,  
                                                GUILayout.MaxWidth(80));

                    m_Permission = EditorGUILayout.EnumPopup(
                        snappoint.m_Permisson,
                        GUILayout.Width(100),
                        GUILayout.MaxWidth(100));

                    ABS_EditorUtils.HorizontalSpace();

                    bool buttonResult = GUILayout.Button(
                       "Show",
                       snappoint == m_ShowSnapPointTarget 
                           ? m_EditorStyleContainer.SmallGreenButtonStyle 
                           : m_EditorStyleContainer.SmallDarkButtonStyle,
                       GUILayout.Width(50)
                    );
                    if (buttonResult)
                    {
                        if(m_ShowSnapPointTarget == snappoint)
                        {
                            m_ShowSnapPointTarget = null;
                        }
                        else
                        {
                            m_ShowSnapPointTarget = snappoint;
                        }
                    }

                    ABS_EditorUtils.FlexibleSpace();

                    CheckPermission(snappoint);
                }
                ABS_EditorUtils.EndHorizontal();
            }
        }

        private void SetAllPermissionForRule(ABS_AdvancedGridSnapPointRule p_Rule, ABS_AdvancedGridSnapPointRule.PermissionType p_Permission)
        {
            foreach (ABS_AdvancedGridSnapPointRule.SnapPoint snappoint in p_Rule.SnapPoints)
            {
                snappoint.m_Permisson = p_Permission;
            }
            ABS_EditorUtils.Dirty(m_RuleSet);
        }

        private void CheckPermission(ABS_AdvancedGridSnapPointRule.SnapPoint p_Snappoint)
        {
            if (m_Permission.CompareTo(ABS_AdvancedGridSnapPointRule.PermissionType.Allow) == 0)
            {
                if (p_Snappoint.m_Permisson != ABS_AdvancedGridSnapPointRule.PermissionType.Allow)
                {
                    p_Snappoint.m_Permisson = ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                    ABS_EditorUtils.Dirty(m_RuleSet);
                }
            }
            else if (m_Permission.CompareTo(ABS_AdvancedGridSnapPointRule.PermissionType.Block) == 0)
            {
                if (p_Snappoint.m_Permisson != ABS_AdvancedGridSnapPointRule.PermissionType.Block)
                {
                    p_Snappoint.m_Permisson = ABS_AdvancedGridSnapPointRule.PermissionType.Block;
                    ABS_EditorUtils.Dirty(m_RuleSet);
                }
            }
            else if (m_Permission.CompareTo(ABS_AdvancedGridSnapPointRule.PermissionType.Deny) == 0)
            {
                if (p_Snappoint.m_Permisson != ABS_AdvancedGridSnapPointRule.PermissionType.Deny)
                {
                    p_Snappoint.m_Permisson = ABS_AdvancedGridSnapPointRule.PermissionType.Deny;
                    ABS_EditorUtils.Dirty(m_RuleSet);
                }
            }
        }

        private void GetRuleSetType()
        {
            m_BaseType = EditorGUILayout.EnumPopup("Target Element Type", m_RuleSet.Type);

            if (CheckRuleSetType(ABS_AdvancedGridType.Floor)) return;
            if (CheckRuleSetType(ABS_AdvancedGridType.Wall)) return;
            if (CheckRuleSetType(ABS_AdvancedGridType.EdgeHorizontal)) return;
            if (CheckRuleSetType(ABS_AdvancedGridType.EdgeVertical)) return;
            if (CheckRuleSetType(ABS_AdvancedGridType.Corner)) return;
            if (CheckRuleSetType(ABS_AdvancedGridType.Center)) return;
        }

        private bool CheckRuleSetType(ABS_AdvancedGridType p_NewType)
        {
            if (m_BaseType.CompareTo(p_NewType) == 0)
            {
                if (m_RuleSet.Type != p_NewType)
                {
                    m_RuleSet.Type = p_NewType;
                    ResetRulesByTargetType(p_NewType);
                    return true;
                }
            }
            return false;
        }

        private void ResetRulesByTargetType(ABS_AdvancedGridType p_NewType)
        {
            foreach (ABS_AdvancedGridSnapPointRule rule in m_EntityList.EntityList)
            {
                rule.SetupByTargetType(p_NewType);
            }
        }

        protected override void OnDrawGizmosImpl()
        {
            if (m_State != State.EntityEditMode
                || m_TempTargetElementInstance == null
                || m_Settings == null)
            {
                return;
            }


            ABS_AdvancedGridSnapPointRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule;

            if (m_Settings == null)
            {
                return;
            }

            int i = 0;
            Vector3 gridSize = m_Settings.GridSize;
            foreach (ABS_AdvancedGridSnapPointRule.SnapPoint snappoint in rule.SnapPoints)
            {
                UnityEngine.Color color = ABS_EditorStyleContainer.s_GreenColor;
                if (snappoint.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Block)
                {
                    color = ABS_EditorStyleContainer.s_BlueColor;
                }
                else if (snappoint.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Deny)
                {
                    color = ABS_EditorStyleContainer.s_RedColor;
                }

                Vector3 alignedSnapPointPosition = new Vector3(
                    gridSize.x * snappoint.m_AdvancedGridSnapPoint.m_Position.x,
                    gridSize.y * snappoint.m_AdvancedGridSnapPoint.m_Position.y,
                    gridSize.z * snappoint.m_AdvancedGridSnapPoint.m_Position.z);

                REST_GizmosUtils.DrawSphere(
                    alignedSnapPointPosition,
                    snappoint == m_ShowSnapPointTarget ? 0.3f : 0.1f, 
                    color);

                REST_GizmosUtils.DrawText(alignedSnapPointPosition, $"Position  ({++i})", color);
            }
        }

        protected override void AddEntityDataSection(int p_EntityIdx)
        {
            ABS_AdvancedGridSnapPointRule rule = m_EntityList.GetEntity(p_EntityIdx) as ABS_AdvancedGridSnapPointRule;

            int i = 0;
            foreach(ABS_AdvancedGridSnapPointRule.SnapPoint snappoint in rule.SnapPoints)
            {
                ABS_EditorUtils.StartHorizontal();
                {
                    EditorGUILayout.LabelField($"Position  ({i++})  :  {snappoint.m_AdvancedGridSnapPoint.m_Position}", GUILayout.MaxWidth(220));

                    ShowColorizedPermissionValue(snappoint);

                    ABS_EditorUtils.FlexibleSpace();
                }
                ABS_EditorUtils.EndHorizontal();
            }
        }

        private void ShowColorizedPermissionValue (ABS_AdvancedGridSnapPointRule.SnapPoint p_Snappoint)
        {
            GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
            coloredTextStyle.stretchWidth = false;
            coloredTextStyle.alignment = TextAnchor.MiddleLeft;
            coloredTextStyle.fixedWidth = 200;

            string colorizesString = p_Snappoint.m_Permisson.ToString();
            
            switch (p_Snappoint.m_Permisson)
            {
                case ABS_AdvancedGridSnapPointRule.PermissionType.Allow:
                    colorizesString= ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "Allow", ABS_EditorStyleContainer.s_GreenColor);
                    break;
                case ABS_AdvancedGridSnapPointRule.PermissionType.Block:
                    colorizesString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "Block", ABS_EditorStyleContainer.s_BlueColor);
                    break;
                case ABS_AdvancedGridSnapPointRule.PermissionType.Deny:
                    colorizesString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "Deny", ABS_EditorStyleContainer.s_RedColor);
                    break;
            }

            EditorGUILayout.LabelField(
                $"Permission :  {colorizesString}",
                coloredTextStyle,
                GUILayout.Width(200),
                GUILayout.MaxWidth(200));
        }

        protected override void AddBaseSection()
        {
            if (m_State == State.EditMode)
            {
                GetRuleSetType();
                ABS_EditorUtils.Space(10);
                AddTempTarget();
                ABS_EditorUtils.Space(10);
                TemplateSection();
                ABS_EditorUtils.Space(5);
                TransformSection(true);
                ABS_EditorUtils.Space(5);
                PermissonSection(true);
            }
            else if (m_State == State.EntityEditMode)
            {
                EditorGUILayout.LabelField($"Target Element Type  :  {m_RuleSet.Type}");

                ABS_EditorUtils.Space(10);

                AddTempTarget();

                ABS_EditorUtils.Space(10);
                TransformSection(false);

                ABS_EditorUtils.Space(5);
                PermissonSection(false);

            }
            else
            {
                EditorGUILayout.LabelField($"Target Element Type  :  {m_RuleSet.Type}");
                m_ShowSnapPointTarget = null;
            }
        }

        private void AddTempTarget()
        {
            bool tempWasNull = m_TempTargetElement == null;

            m_TempTargetElement = ABS_EditorUtils.AddObjectField("Temp Target Element", m_TempTargetElement, false);

            if (tempWasNull && m_TempTargetElement != null)
            {
                m_TempTargetElementInstance = AddDrawnElement(m_TempTargetElement, "GhostElement_Target");
                m_Settings = m_TempTargetElement.PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings;
            }
            else if (!tempWasNull
                && m_TempTargetElement != null
                && m_TempTargetElementInstance != null
                && m_TempTargetElementInstance.PrefabGuid != m_TempTargetElement.PrefabGuid)
            {
                RemoveDrawnElement(m_TempTargetElementInstance);
                m_TempTargetElementInstance = AddDrawnElement(m_TempTargetElement, "GhostElement_Target");
                m_Settings = m_TempTargetElement.PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings;
            }
            else if (!tempWasNull && m_TempTargetElement == null)
            {
                RemoveDrawnElement(m_TempTargetElementInstance);
                m_TempTargetElement = null;
                m_TempTargetElementInstance = null;
                m_Settings = null;
            }
        }

        private void PermissonSection(bool p_RotateAll)
        {
            m_PermissionSectionIsOpened = EditorGUILayout.BeginFoldoutHeaderGroup(m_PermissionSectionIsOpened, "Permissions");
            {
                if (m_PermissionSectionIsOpened)
                {
                    ABS_EditorUtils.IndentIn();
                    {
                        ABS_EditorUtils.StartHorizontal();
                        {
                            bool buttonResult = GUILayout.Button(
                               "Set all to Allow",
                               m_EditorStyleContainer.SmallGreenButtonStyle,
                               GUILayout.Width(100)
                            );
                            if (buttonResult)
                            {
                                if (p_RotateAll)
                                {
                                    foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
                                    {
                                        SetAllPermissionForRule(rule, ABS_AdvancedGridSnapPointRule.PermissionType.Allow);
                                    }
                                }
                                else
                                {
                                    ABS_AdvancedGridSnapPointRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule;
                                    SetAllPermissionForRule(rule, ABS_AdvancedGridSnapPointRule.PermissionType.Allow);
                                }
                            }

                            buttonResult = GUILayout.Button(
                               "Set all to Block",
                               m_EditorStyleContainer.SmallBlueButtonStyle,
                               GUILayout.Width(100)
                            );
                            if (buttonResult)
                            {
                                if (p_RotateAll)
                                {
                                    foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
                                    {
                                        SetAllPermissionForRule(rule, ABS_AdvancedGridSnapPointRule.PermissionType.Block);
                                    }
                                }
                                else
                                {
                                    ABS_AdvancedGridSnapPointRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule;
                                    SetAllPermissionForRule(rule, ABS_AdvancedGridSnapPointRule.PermissionType.Block);
                                }
                            }

                            buttonResult = GUILayout.Button(
                               "Set all to Deny",
                               m_EditorStyleContainer.SmallRedButtonStyle,
                               GUILayout.Width(100)
                            );
                            if (buttonResult)
                            {
                                if (p_RotateAll)
                                {
                                    foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
                                    {
                                        SetAllPermissionForRule(rule, ABS_AdvancedGridSnapPointRule.PermissionType.Deny);
                                    }
                                }
                                else
                                {
                                    ABS_AdvancedGridSnapPointRule rule = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule;
                                    SetAllPermissionForRule(rule, ABS_AdvancedGridSnapPointRule.PermissionType.Deny);
                                }
                            }
                        }
                        ABS_EditorUtils.EndHorizontal();
                    }
                    ABS_EditorUtils.IndentOut();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void TransformSection(bool p_AffectOnAll)
        {
            m_TransformSectionIsOpened = EditorGUILayout.BeginFoldoutHeaderGroup(m_TransformSectionIsOpened, "Transform");
            if (m_TransformSectionIsOpened)
            {
                ABS_EditorUtils.IndentIn();
                {
                    ABS_EditorUtils.StartHorizontal();
                    {
                        bool buttonResult = GUILayout.Button(
                           p_AffectOnAll ? "Mirror All X" : "Mirror X",
                           m_EditorStyleContainer.SmallDarkButtonStyle,
                           GUILayout.Width(100)
                        );
                        if (buttonResult)
                        {
                            if (p_AffectOnAll)
                            {
                                foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
                                {
                                    MirrorX(rule);
                                }
                            }
                            else
                            {
                                MirrorX(m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule);
                            }
                            ABS_EditorUtils.Dirty(m_RuleSet);
                        }

                        buttonResult = GUILayout.Button(
                           p_AffectOnAll ? "Mirror All Y" : "Mirror Y",
                           m_EditorStyleContainer.SmallDarkButtonStyle,
                           GUILayout.Width(100)
                        );
                        if (buttonResult)
                        {
                            if (p_AffectOnAll)
                            {
                                foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
                                {
                                    MirrorY(rule);
                                }
                            }
                            else
                            {
                                MirrorY(m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule);
                            }
                            ABS_EditorUtils.Dirty(m_RuleSet);
                        }

                        buttonResult = GUILayout.Button(
                           p_AffectOnAll ? "Mirror All Z" : "Mirror Z",
                           m_EditorStyleContainer.SmallDarkButtonStyle,
                           GUILayout.Width(100)
                        );
                        if (buttonResult)
                        {
                            if (p_AffectOnAll)
                            {
                                foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
                                {
                                    MirrorZ(rule);
                                }
                            }
                            else
                            {
                                MirrorZ(m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_AdvancedGridSnapPointRule);
                            }
                            ABS_EditorUtils.Dirty(m_RuleSet);
                        }
                    }
                    ABS_EditorUtils.EndHorizontal();
                }
                ABS_EditorUtils.IndentOut();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void TemplateSection ()
        {
            m_TemplateSectionIsOpened = EditorGUILayout.BeginFoldoutHeaderGroup(m_TemplateSectionIsOpened, "Template");
            if (m_TemplateSectionIsOpened)
            {
                if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Floor
                    || (ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Wall)
                {
                    ABS_EditorUtils.IndentIn();
                    {
                        switch ((ABS_AdvancedGridType)m_BaseType)
                        {
                            case ABS_AdvancedGridType.Floor:
                                m_Template_Floor = EditorGUILayout.EnumPopup("Template type", m_Template_Floor);
                                break;
                            case ABS_AdvancedGridType.Wall:
                                m_Template_Wall = EditorGUILayout.EnumPopup("Template type", m_Template_Wall);
                                break;
                            case ABS_AdvancedGridType.EdgeHorizontal:
                                m_Template_EdgeHorizontal = EditorGUILayout.EnumPopup("Template type", m_Template_EdgeHorizontal);
                                break;
                            case ABS_AdvancedGridType.EdgeVertical:
                                m_Template_EdgeVertical = EditorGUILayout.EnumPopup("Template type", m_Template_EdgeVertical);
                                break;
                            case ABS_AdvancedGridType.Corner:
                                m_Template_Corner = EditorGUILayout.EnumPopup("Template type", m_Template_Corner);
                                break;
                            case ABS_AdvancedGridType.Center:
                                m_Template_Center = EditorGUILayout.EnumPopup("Template type", m_Template_Center);
                                break;
                        }

                        bool buttonResult = GUILayout.Button(
                           "Apply",
                           m_EditorStyleContainer.SmallGreenButtonStyle,
                           GUILayout.Width(100)
                        );
                        if (buttonResult)
                        {
                            SetupByTemplate();
                            ABS_EditorUtils.Dirty(m_RuleSet);
                        }
                    }
                    ABS_EditorUtils.IndentOut();
                }
                else
                {
                    EditorGUILayout.LabelField("No template is available for this type of element.", GUILayout.MaxWidth(220));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void SetupByTemplate ()
        {
            foreach (ABS_AdvancedGridSnapPointRule rule in m_RuleSet.Rules)
            {
                List<ABS_AdvancedGridSnapPointRule.SnapPoint> snapPoint = rule.SnapPoints;
                ABS_AdvancedGridSnapPointRule.PermissionType[] permissions = 
                    ABS_AdvancedGridSnapPointRuleSetTemplates.GetTemplatePermissions(rule.Type, MapTemplate());

                for (int i = 0; i < permissions.Length; ++i)
                {
                    snapPoint[i].m_Permisson = permissions[i];
                }
            }
        }

        private ABS_AdvancedGridSnapPointRuleSetTemplates.RuleTemplate MapTemplate ()
        {
            switch ((ABS_AdvancedGridType)m_BaseType)
            {
                case ABS_AdvancedGridType.Floor:
                    {
                        switch ((Template_Floor)m_Template_Floor)
                        {
                            case Template_Floor.BlockedCorner: return ABS_AdvancedGridSnapPointRuleSetTemplates.RuleTemplate.Floor_BlockedCorner;
                            case Template_Floor.BlockTwoSide: return ABS_AdvancedGridSnapPointRuleSetTemplates.RuleTemplate.Floor_BlockTwoSide;
                        }
                    }
                    break;
                case ABS_AdvancedGridType.Wall:
                    {
                        switch ((Template_Wall)m_Template_Wall)
                        {
                            case Template_Wall.BlockWallSnapping: return ABS_AdvancedGridSnapPointRuleSetTemplates.RuleTemplate.Wall_BlockWallSnapping;
                        }
                    }
                    break;
            }
            return ABS_AdvancedGridSnapPointRuleSetTemplates.RuleTemplate.Floor_BlockedCorner;
        }

        protected override bool IsStatic()
        {
            return true;
        }

        private void MirrorX(ABS_AdvancedGridSnapPointRule p_Rule)
        {
            List<ABS_AdvancedGridSnapPointRule.SnapPoint> snapPoint = p_Rule.SnapPoints;
            if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Floor)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[4], snapPoint[5]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[6]);
                        Swap(snapPoint[5], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Wall)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[6]);
                        Swap(snapPoint[5], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[5]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Center:
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.EdgeHorizontal)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[3], snapPoint[4]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Center:
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.EdgeVertical)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[6]);
                        Swap(snapPoint[5], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[4], snapPoint[5]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Corner)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[5], snapPoint[6]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[6]);
                        Swap(snapPoint[5], snapPoint[7]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Center)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[4], snapPoint[5]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[6]);
                        Swap(snapPoint[5], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    default:
                        break;
                }
            }
        }

        private void MirrorY(ABS_AdvancedGridSnapPointRule p_Rule)
        {
            List<ABS_AdvancedGridSnapPointRule.SnapPoint> snapPoint = p_Rule.SnapPoints;
            if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Floor)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Wall)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[2], snapPoint[4]);
                        Swap(snapPoint[3], snapPoint[5]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.EdgeHorizontal)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[2]);
                        Swap(snapPoint[1], snapPoint[3]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.EdgeVertical)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Corner)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Center)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[4]);
                        Swap(snapPoint[1], snapPoint[5]);
                        Swap(snapPoint[2], snapPoint[6]);
                        Swap(snapPoint[3], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[4], snapPoint[5]);
                        break;
                    default:
                        break;
                }
            }
        }

        private void MirrorZ(ABS_AdvancedGridSnapPointRule p_Rule)
        {
            List<ABS_AdvancedGridSnapPointRule.SnapPoint> snapPoint = p_Rule.SnapPoints;
            if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Floor)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[5]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Wall)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[4], snapPoint[5]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.EdgeHorizontal)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[1]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[2], snapPoint[4]);
                        Swap(snapPoint[3], snapPoint[5]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.EdgeVertical)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[5]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Corner)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        break;
                    case ABS_AdvancedGridType.Corner:
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[5]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    default:
                        break;
                }
            }
            else if ((ABS_AdvancedGridType)m_BaseType == ABS_AdvancedGridType.Center)
            {
                switch (p_Rule.Type)
                {
                    case ABS_AdvancedGridType.Floor:
                        break;
                    case ABS_AdvancedGridType.Wall:
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.EdgeHorizontal:
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.EdgeVertical:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    case ABS_AdvancedGridType.Corner:
                        Swap(snapPoint[0], snapPoint[1]);
                        Swap(snapPoint[2], snapPoint[3]);
                        Swap(snapPoint[4], snapPoint[5]);
                        Swap(snapPoint[6], snapPoint[7]);
                        break;
                    case ABS_AdvancedGridType.Center:
                        Swap(snapPoint[2], snapPoint[3]);
                        break;
                    default:
                        break;
                }
            }
        }

        private void Swap(ABS_AdvancedGridSnapPointRule.SnapPoint p_SnapPoint1, ABS_AdvancedGridSnapPointRule.SnapPoint p_SnapPoint2)
        {
            ABS_AdvancedGridSnapPointRule.PermissionType tmp = p_SnapPoint1.m_Permisson;
            p_SnapPoint1.m_Permisson = p_SnapPoint2.m_Permisson;
            p_SnapPoint2.m_Permisson = tmp;
        }
    }
}
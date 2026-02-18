//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_SnapRelationship))]
    internal class ABS_SnapRelationshipEditor : ABS_DrawableScriptableObjectEditor<ABS_SnapRelationship, ABS_SnapRelationship.SnapPosition>
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private int m_VisualizationOriginalTargetIndex = 0;
        private ABS_BuildingElement m_ElementA = null;
        private ABS_BuildingElement m_ElementB = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
            if (ABS_ScriptableObjectDrawer.Instance != null)
            {
                if (m_EntityListHolder.ElementA)
                {
                    m_ElementA = AddDrawnElement(m_EntityListHolder.ElementA, "GhostElement_A");
                }

                if (m_EntityListHolder.ElementB)
                {
                    m_ElementB = AddDrawnElement(m_EntityListHolder.ElementA, "GhostElement_B");
                }
            }
        }

        public new void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "SnapRelationship";
        }

        protected override void AddEntityEditorSection()
        {
            ABS_SnapRelationship.SnapPosition snapPosition = m_EntityList.GetEntity(m_EntityIndexForEdit) as ABS_SnapRelationship.SnapPosition;
            if (snapPosition == null)
            {
                REST_Logging.Error("SnapRelationshipEditor", $"The snapPosition was null! Wrong index! ListSize: {m_EntityList.EntityList.Count} | Index: {m_EntityIndexForEdit}");
                m_State = State.EditMode;
                return;
            }

            snapPosition.m_Name = EditorGUILayout.TextField("Name", snapPosition.m_Name);

            ABS_EditorUtils.Space();
            snapPosition.m_RelationType = ABS_EditorUtils.LayoutEnumPopup<ABS_SnapRelationship.RelationType>("RelationType  :  ", snapPosition.m_RelationType);

            ABS_EditorUtils.Space();
            snapPosition.m_Position = EditorGUILayout.Vector3Field("Position  :  ", snapPosition.m_Position);
            snapPosition.m_Rotation = EditorGUILayout.Vector3Field("Rotation  :  ", snapPosition.m_Rotation);
        }

        protected override void AddEntityDataSection(int p_EntityIdx)
        {
            ABS_SnapRelationship.SnapPosition snapPosition = m_EntityList.GetEntity(p_EntityIdx) as ABS_SnapRelationship.SnapPosition;

            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField($"RelationType  :  {snapPosition.m_RelationType.ToString()}");

            ABS_EditorUtils.Space();
            if (m_EntityListHolder.ElementA != null)
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string name = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, m_EntityListHolder.ElementA.name, UnityEngine.Color.white);
                EditorGUILayout.LabelField($"Element A  :  {name}  |  Prefab Guid : {m_EntityListHolder.ElementA.PrefabGuid}", coloredTextStyle);
            }
            else
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string nullString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "null", ABS_EditorStyleContainer.s_RedColor);
                EditorGUILayout.LabelField($"Element A  :  {nullString}", coloredTextStyle);
            }

            if (m_EntityListHolder.ElementB != null)
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string name = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, m_EntityListHolder.ElementB.name, UnityEngine.Color.white);
                EditorGUILayout.LabelField($"Element B  :  {name}  |  Prefab Guid : {m_EntityListHolder.ElementB.PrefabGuid}", coloredTextStyle);
            }
            else
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string nullString = ABS_EditorStyleContainer.ColorizeText(ref coloredTextStyle, "null", ABS_EditorStyleContainer.s_RedColor);
                EditorGUILayout.LabelField($"Element B  :  {nullString}", coloredTextStyle);
            }

            ABS_EditorUtils.IndentIn();
            {
                EditorGUILayout.LabelField($"Position  :  {snapPosition.m_Position}");
                EditorGUILayout.LabelField($"Rotation  :  {snapPosition.m_Rotation}");
            }
            ABS_EditorUtils.IndentOut();
        }

        protected override void AddBaseSection()
        {
            if (m_State == State.EditMode)
            {
                m_EntityListHolder.ElementA = ABS_EditorUtils.AddObjectField("BuildingElement A", m_EntityListHolder.ElementA, false);
                if (m_EntityListHolder.ElementA != null)
                {
                    ABS_EditorUtils.IndentIn();
                    {
                        ABS_EditorUtils.AddBuildingElementDataLine(m_EntityListHolder.ElementA.gameObject, m_EntityListHolder.ElementA.PrefabGuid, "Element A : ");
                    }
                    ABS_EditorUtils.IndentOut();
                }
                else
                {
                    EditorGUILayout.HelpBox("Element A is null!", MessageType.Error);
                }

                m_EntityListHolder.ElementB = ABS_EditorUtils.AddObjectField("BuildingElement B", m_EntityListHolder.ElementB, false);
                if (m_EntityListHolder.ElementB != null)
                {
                    ABS_EditorUtils.IndentIn();
                    {
                        ABS_EditorUtils.AddBuildingElementDataLine(m_EntityListHolder.ElementB.gameObject, m_EntityListHolder.ElementB.PrefabGuid, "Element B : ");
                    }
                    ABS_EditorUtils.IndentOut();
                }
                else
                {
                    EditorGUILayout.HelpBox("Element B is null!", MessageType.Error);
                }
            }
            else
            {
                if (m_EntityListHolder.ElementA != null)
                {
                    ABS_EditorUtils.AddBuildingElementDataLine(m_EntityListHolder.ElementA.gameObject, m_EntityListHolder.ElementA.PrefabGuid, "Element A : ");
                }
                else
                {
                    EditorGUILayout.HelpBox("Element A is null!", MessageType.Error);
                }

                if (m_EntityListHolder.ElementB != null)
                {
                    ABS_EditorUtils.AddBuildingElementDataLine(m_EntityListHolder.ElementB.gameObject, m_EntityListHolder.ElementB.PrefabGuid, "Element B : ");
                }
                else
                {
                    EditorGUILayout.HelpBox("Element B is null!", MessageType.Error);
                }
            }

            if (m_EntityList.EntityList.Count > 0 && m_State != State.EntityEditMode)
            {
                ABS_EditorUtils.Space();
                List<string> nameList = new List<string>();
                int i = 0;
                foreach (ABS_SnapRelationship.SnapPosition snapPosition in m_EntityList.EntityList)
                {
                    nameList.Add($"{++i} {snapPosition.Name}");
                }

                m_VisualizationOriginalTargetIndex = EditorGUILayout.Popup(m_VisualizationOriginalTargetIndex, nameList.ToArray());
                ABS_EditorUtils.Space();
            }
        }

        protected override void OnDrawGizmosImpl()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (m_State == State.EditMode)
            {
                foreach (ABS_SnapRelationship.SnapPosition snapPosition in m_EntityList.EntityList)
                {
                    DrawSnapPosition(snapPosition);
                }
            }
            else if (m_State == State.EntityEditMode)
            {
                m_VisualizationOriginalTargetIndex = m_EntityIndexForEdit;
                ABS_SnapRelationship.SnapPosition snapPointBaseForEdit = m_EntityList.EntityList[m_EntityIndexForEdit];
                DrawSnapPosition(snapPointBaseForEdit);
            }

            if (m_EntityList.EntityList.Count > 0)
            {
                PlaceElementToSetup(m_EntityList.EntityList[m_VisualizationOriginalTargetIndex]);
            }
        }

        private void PlaceElementToSetup (ABS_SnapRelationship.SnapPosition p_SnapPosition)
        {
            if (m_ElementA != null && m_ElementB != null)
            {
                if (p_SnapPosition.m_RelationType == ABS_SnapRelationship.RelationType.AToB)
                {
                    m_ElementA.transform.localPosition = p_SnapPosition.m_Position;
                    m_ElementA.transform.localEulerAngles = p_SnapPosition.m_Rotation;

                    m_ElementB.transform.localPosition = Vector3.zero;
                    m_ElementB.transform.localEulerAngles = Vector3.zero;
                }
                else if (p_SnapPosition.m_RelationType == ABS_SnapRelationship.RelationType.BToA)
                {
                    m_ElementB.transform.localPosition = p_SnapPosition.m_Position;
                    m_ElementB.transform.localEulerAngles = p_SnapPosition.m_Rotation;

                    m_ElementA.transform.localPosition = Vector3.zero;
                    m_ElementA.transform.localEulerAngles = Vector3.zero;
                }
            }

        }

        private void DrawSnapPosition(ABS_SnapRelationship.SnapPosition p_SnapPosition)
        {
            if (p_SnapPosition == null)
            {
                REST_Logging.Error("SnapRelationshipEditor", $"The p_SnapPosition was null! Wrong index! | Index: {m_EntityIndexForEdit}");
                m_State = State.EditMode;
                return;
            }

            UnityEngine.Color color = Color.blue;
            if (p_SnapPosition.m_RelationType == ABS_SnapRelationship.RelationType.AToB)
            {
                color = Color.red;
            }
            REST_GizmosUtils.DrawSphere(p_SnapPosition.m_Position, 0.05f, color);
            Quaternion myRotation = Quaternion.Euler(p_SnapPosition.m_Rotation);
            REST_GizmosUtils.DrawArrow(p_SnapPosition.m_Position, Vector3.forward, 0.5f, myRotation, color);


            REST_GizmosUtils.DrawSphere(Vector3.zero, 0.05f, Color.green);
            REST_GizmosUtils.DrawArrow(Vector3.zero, Vector3.forward, 0.5f, Quaternion.identity, color);
        }
        protected override bool IsStatic()
        {
            return false;
        }
    }
}
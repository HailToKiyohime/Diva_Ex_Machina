//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingElementList))]
    internal class ABS_BuildingElementListEditor : ABS_EditorBase
    {
        private SerializedProperty m_BuildingElementsProperty;

        bool m_Validated = false;
        string m_InvalidIndexMessage = string.Empty;
        Dictionary<string, List<(int, ABS_BuildingElement)>> m_ElementsWithSamePrefabGuid = new Dictionary<string, List<(int, ABS_BuildingElement)>>();
        List<(int, ABS_BuildingElement)> m_ElementsWithoutSettings = new List<(int, ABS_BuildingElement)>();
        List<(int, ABS_BuildingElement)> m_ElementsWithBlockedDragBuilding = new List<(int, ABS_BuildingElement)>();
        List<(int, ABS_BuildingElement)> m_ElementsWithWrongColliderSetup = new List<(int, ABS_BuildingElement)>();

        //FreeBuilding Attach problems
        List<(int, ABS_BuildingElement)> m_IndestructibleAttachmentElement = new List<(int, ABS_BuildingElement)>();
        List<(int, ABS_BuildingElement)> m_AttachmentElementWithoutValdiation = new List<(int, ABS_BuildingElement)>();
        List<(int, ABS_BuildingElement)> m_AttachmentElementWithoutBlockedbyValidation = new List<(int, ABS_BuildingElement)>();

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "BuildingElementList";
        }
        protected override void OnEnableImpl()
        {
            m_Validated = false;
            m_BuildingElementsProperty = serializedObject.FindProperty("m_BuildingElements");
        }

        protected override void OnInspectorGUIImpl()
        {
            EditorGUILayout.PropertyField(m_BuildingElementsProperty, new GUIContent("BuildingElements"));

            bool buttonResult = GUILayout.Button(
                "Validate",
                m_EditorStyleContainer.SmallDarkButtonStyle,
                GUILayout.Width(120));

            if (buttonResult)
            {
                Validate();
            }


            if (m_Validated)
            {

                //Errors------------------------------------------------------
                bool ok = true;
                if (!string.IsNullOrEmpty(m_InvalidIndexMessage))
                {
                    ok &= false;
                    EditorGUILayout.HelpBox(m_InvalidIndexMessage, MessageType.Error);
                }
                EditorGUILayout.Space(10);

                if (m_ElementsWithSamePrefabGuid.Count > 0)
                {
                    ok &= false;
                    EditorGUILayout.HelpBox("The following elements are sharing the same PrefabGuid!", MessageType.Error);
                    foreach ((string guid, List<(int, ABS_BuildingElement)> wrongElementList) in m_ElementsWithSamePrefabGuid)
                    {
                        EditorGUILayout.LabelField($"PrefabGuid: {guid}");
                        WriteWrongElementDataOnlyMSG(wrongElementList);
                    }
                }
                EditorGUILayout.Space(10);

                ok &= WriteWrongElementData(m_ElementsWithoutSettings, MessageType.Error,
                    "The following elements hasn't Position Algorithm Settings");

                ok &= WriteWrongElementData(m_ElementsWithBlockedDragBuilding, MessageType.Error,
                    "The following elements' drag building blocked on both axis");

                ok &= WriteWrongElementData(m_ElementsWithWrongColliderSetup, MessageType.Error,
                    "The following elements' dimension set as ColliderBased but it has a null Dimension Collider");

                ok &= WriteWrongElementData(m_AttachmentElementWithoutValdiation, MessageType.Error,
                    "The following FreeBuidling elements has enabled the attachment featue and the attachment is needed meanwhile " +
                        "the BuildOnTopOfElement validation has been disabled");

                ok &= WriteWrongElementData(m_AttachmentElementWithoutBlockedbyValidation, MessageType.Error,
                    "The following FreeBuidling elements has enabled the attachment featue and the attachment is needed meanwhile " +
                        "the BuildOnTopOfElement validation has been set as blocked");
                //Warrnings------------------------------------------------------

                ok &= WriteWrongElementData(m_IndestructibleAttachmentElement, MessageType.Warning,
                    "The following FreeBuidling elements has enabled the attachment featue meanwhile they are Indestructible");

                //OK------------------------------------------------------
                if (ok)
                {
                    EditorGUILayout.HelpBox("Everything is fine!", MessageType.Info);
                }
            }
        }


        private bool WriteWrongElementData(List<(int, ABS_BuildingElement)> p_List, UnityEditor.MessageType p_MSGType, in string p_MSG)
        {
            if (p_List.Count == 0)
            {
                return true;
            }

            EditorGUILayout.HelpBox(p_MSG, MessageType.Warning);
            WriteWrongElementDataOnlyMSG(p_List);

            return false;
        }

        private void WriteWrongElementDataOnlyMSG(List<(int, ABS_BuildingElement)> p_List)
        {
            ABS_EditorUtils.IndentIn();
            foreach ((int idx, ABS_BuildingElement element) in p_List)
            {
                if (element != null)
                {
                    ABS_EditorUtils.AddBuildingElementDataLine(element.gameObject, idx.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField($"{idx}      Missing Prefab", GUILayout.Height(50), GUILayout.Width(70));
                }
            }
            ABS_EditorUtils.IndentOut();
        }

        private void Validate()
        {
            m_ElementsWithSamePrefabGuid.Clear();
            m_ElementsWithoutSettings.Clear();
            m_ElementsWithBlockedDragBuilding.Clear();
            m_ElementsWithWrongColliderSetup.Clear();
            m_IndestructibleAttachmentElement.Clear();
            m_AttachmentElementWithoutValdiation.Clear();
            m_AttachmentElementWithoutBlockedbyValidation.Clear();

            List<int> invalideElementIndexes = new List<int>();

            for (int i = 0; i < m_BuildingElementsProperty.arraySize; i++)
            {
                SerializedProperty elementProperty = m_BuildingElementsProperty.GetArrayElementAtIndex(i);
                ABS_BuildingElement buildingElement = elementProperty.objectReferenceValue as ABS_BuildingElement;
                if (buildingElement == null)
                {
                    invalideElementIndexes.Add(i);
                    continue;
                }

                //Check PrefabGuid
                List<(int, ABS_BuildingElement)> elementsTMP = null;
                if (!m_ElementsWithSamePrefabGuid.TryGetValue(buildingElement.PrefabGuid, out elementsTMP) || elementsTMP == null)
                {
                    elementsTMP = new List<(int, ABS_BuildingElement)>();
                    m_ElementsWithSamePrefabGuid[buildingElement.PrefabGuid] = elementsTMP;
                }
                elementsTMP.Add((i, buildingElement));

                //Check Elements WithoutSettings
                if (buildingElement.PositionAlgorithmSettings == null)
                {
                    m_ElementsWithoutSettings.Add((i, buildingElement));
                }
                else
                {
                    //Check AttachmentLogic
                    if (buildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.Free)
                    {
                        ABS_FreeBuilderSettings algorithmSettings = buildingElement.PositionAlgorithmSettings as ABS_FreeBuilderSettings;
                        if (algorithmSettings.EnableAttachementConnection == true
                            && buildingElement.Indestructible)
                        {
                            m_IndestructibleAttachmentElement.Add((i, buildingElement));
                        }

                        if (algorithmSettings.EnableAttachementConnection == true
                            && buildingElement.ShouldAttached)
                        {
                            if (algorithmSettings.BuildOnTopOfElement)
                            {
                                if (algorithmSettings.BuildOnTopOfElementResultHandling == ABS_m_BuildOnTopOfElementResultHandling.Block)
                                {
                                    m_AttachmentElementWithoutBlockedbyValidation.Add((i, buildingElement));
                                }
                            }
                            else
                            {
                                m_AttachmentElementWithoutValdiation.Add((i, buildingElement));
                            }
                        }
                    }
                }

                //Check DragBuilding
                if (!buildingElement.EnabledDragBuildingX && !buildingElement.EnabledDragBuildingZ)
                {
                    m_ElementsWithBlockedDragBuilding.Add((i, buildingElement));
                }

                if (buildingElement.BuildingElementDimensionType == ABS_BuildingElementDimensionType.ColliderBased
                    && buildingElement.DimensionCollider == null)
                {
                    m_ElementsWithWrongColliderSetup.Add((i, buildingElement));
                }
            }

            //Null elements in the list
            if (invalideElementIndexes.Count > 0)
            {
                string msg = "Invalid Object at : ";
                for (int i = 0; i < invalideElementIndexes.Count; i++)
                {
                    if (i + 1 == invalideElementIndexes.Count)
                    {
                        msg += $"{invalideElementIndexes[i]}";
                    }
                    else
                    {
                        msg += $"{invalideElementIndexes[i]}, ";
                    }
                }
                m_InvalidIndexMessage = msg;
            }

            //Elements with the same prefabGuid
            List<string> guidsForRemove = new List<string>();
            foreach ((string guid, List<(int, ABS_BuildingElement)> wrongElements) in m_ElementsWithSamePrefabGuid)
            {
                if (wrongElements.Count < 2)
                {
                    guidsForRemove.Add(guid);
                }
            }
            foreach (string guid in guidsForRemove)
            {
                m_ElementsWithSamePrefabGuid.Remove(guid);
            }


            m_Validated = true;
        }
    }
}

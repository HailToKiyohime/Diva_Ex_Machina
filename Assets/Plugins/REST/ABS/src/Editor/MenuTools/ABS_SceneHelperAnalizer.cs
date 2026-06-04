//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_SceneHelperAnalizer
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private delegate void FixingIssueDelegate<Type>(List<Type> p_Targets) where Type : class;
        private delegate void FixingMissingObjectIssueDelegate();
        private delegate void FixingIssueDictionaryDelegate<Type>(Dictionary<string, List<Type>> p_Targets) where Type : class;

        private bool m_Analized = false;
        private bool m_ReanalizationNeeded = false;
        private Vector2 m_ScrollPos;

        private ABS_EditorStyleContainer m_EditorStyleContainer = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Script problems

        private List<GameObject> m_ScriptConflict = new List<GameObject>();
        private bool m_ScriptConflictDetails = false;

        private List<GameObject> m_ScriptDuplication = new List<GameObject>();
        private bool m_ScriptDuplicationDetails = false;

        private List<GameObject> m_BuildingParent_Under_BuildingParent = new List<GameObject>();
        private List<GameObject> m_BuildingParent_Under_Building = new List<GameObject>();
        private List<GameObject> m_BuildingParent_Under_BuildingElement = new List<GameObject>();
        private List<GameObject> m_BuildingParent_Under_BuildingElementLink = new List<GameObject>();
        private List<GameObject> m_Building_NotUnder_BuildingParent = new List<GameObject>();
        private List<GameObject> m_Building_Under_Building = new List<GameObject>();
        private List<GameObject> m_Building_Under_BuildingElement = new List<GameObject>();
        private List<GameObject> m_Building_Under_BuildingElementLink = new List<GameObject>();
        private List<GameObject> m_BuildingElement_Under_BuildingParent = new List<GameObject>();
        private List<GameObject> m_BuildingElement_NotUnder_Building = new List<GameObject>();
        private List<GameObject> m_BuildingElement_Under_BuildingElement = new List<GameObject>();
        private List<GameObject> m_BuildingElement_Under_BuildingElementLink = new List<GameObject>();
        private List<GameObject> m_BuildingElementLink_Under_BuildingParent = new List<GameObject>();
        private List<GameObject> m_BuildingElementLink_Under_Building = new List<GameObject>();
        private List<GameObject> m_BuildingElementLink_NotUnder_BuildingElement = new List<GameObject>();
        private List<GameObject> m_OtherElementsUnderBuildingParent = new List<GameObject>();
        private List<GameObject> m_OtherElementsUnderBuilding = new List<GameObject>();
        private bool m_ParentingErrorDetails = false;
        private int m_ParentingErrorCounter = 0;

        #endregion Script problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Guid problems

        private Dictionary<string, List<ABS_SaveableMonobehaviour>> m_InstanceGuids = new Dictionary<string, List<ABS_SaveableMonobehaviour>>();
        private bool m_DuplicatedInstaceGuidDetails = false;

        private List<ABS_SaveableMonobehaviour> m_ObjectNotFixedInstanceGuid = new List<ABS_SaveableMonobehaviour>();
        private bool m_ObjectNotFixedInstanceGuidDetails = false;

        private List<ABS_SaveableMonobehaviour> m_ObjectWithNullInstanceGuid = new List<ABS_SaveableMonobehaviour>();
        private bool m_ObjectWithNullInstanceGuidDetails = false;

        private List<ABS_SaveableMonobehaviour> m_ObjectWithNullPrefabGuid = new List<ABS_SaveableMonobehaviour>();
        private bool m_ObjectWithNullPrefabGuidDetails = false;
        private bool m_AddClonePostfix = false;
        private ABS_SaveableMonobehaviour m_ABSObject = null;

        #endregion Guid problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingElement Highlight problems

        private List<ABS_BuildingElement> m_ElementMissingHighlight = new List<ABS_BuildingElement>();
        private bool m_ElementMissingHighlightDetails = false;
        private static ABS_BuildingElementHighlightCollection s_HighlightCollection = null;
        private GUIContent m_HighlightCollectionGUIContent;

        private List<ABS_BuildingElement> m_ElementMissingRenderers = new List<ABS_BuildingElement>();
        private bool m_ElementMissingRenderersDetails = false;

        #endregion BuildingElement Highlight problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingElement Algorithm problems

        private List<ABS_BuildingElement> m_ElementsUnderWrongTypeBuilding = new List<ABS_BuildingElement>();
        private List<ABS_BuildingElement> m_ElementsUnderWrongTypeBuildingMultiple = new List<ABS_BuildingElement>();
        private bool m_ElementsUnderWrongTypeBuildingDetails = false;

        private List<ABS_BuildingElement> m_ElementsWithWrongAlgorithmSettings = new List<ABS_BuildingElement>();
        private bool m_ElementsWithWrongAlgorithmSettingsDetails = false;
        private static ABS_FreeBuilderSettings s_FreeBuilderSettings = null;
        private static ABS_BasicGridBuilderSettings s_BasicGridBuilderSettings = null;
        private static ABS_AdvancedGridBuilderSettings s_AdvancedGridBuilderSettings = null;
        private static ABS_SnapPointBasedBuilderSettings s_SnapPointBasedBuilderSettings = null;

        #endregion BuildingElement Algorithm problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingElement AdvancedGridBuilding problems

        private List<ABS_BuildingElement> m_AdvancedGridBuildingWrongGridPosition = new List<ABS_BuildingElement>();
        private bool m_AdvancedGridBuildingWrongGridPositionDetails = false;

        private List<ABS_BuildingElement> m_AdvancedGridBuildingWrongGridRotation = new List<ABS_BuildingElement>();
        private bool m_AdvancedGridBuildingWrongGridRotationDetails = false;

        #endregion BuildingElement AdvancedGridBuilding problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingElement BasicGridBuilding problems

        private List<ABS_BuildingElement> m_BasicGridBuildingWrongGridPosition = new List<ABS_BuildingElement>();
        private bool m_BasicGridBuildingWrongGridPositionDetails = false;

        #endregion BuildingElement BasicGridBuilding problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingElementLink problems

        private List<ABS_BuildingElementLink> m_ElementLinkWithNullTarget = new List<ABS_BuildingElementLink>();
        private bool m_ElementLinkWithNullTargetGuidDetails = false;

        #endregion BuildingElementLink problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingParent problems

        private bool m_BuildingParentHasFound = false;
        private bool m_BuildingParentHasFoundDetails = false;

        private List<ABS_BuildingParent> m_ParentWithMissingGlobalFreeBuilding = new List<ABS_BuildingParent>();
        private List<ABS_BuildingParent> m_ParentWithMissingGlobalGridBuilding = new List<ABS_BuildingParent>();
        private bool m_ParentWithMissingGlobalFreeBuildingDetails = false;
        private bool m_ParentWithMissingGlobalGridBuildingDetails = false;

        #endregion BuildingParent problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingManager problems

        private bool m_BuildingManagerHasFound = false;
        private bool m_BuildingManagerHasFoundDetails = false;

        private List<ABS_BuildingManager> m_ManagerWithoutBuildingElementList = new List<ABS_BuildingManager>();
        private bool m_ManagerWithoutBuildingElementListDetails = false;

        private List<ABS_BuildingManager> m_ManagerWithoutBuildingParent = new List<ABS_BuildingManager>();
        private bool m_ManagerWithoutBuildingParentDetails = false;

        #endregion BuildingParent problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingArea problems

        private List<ABS_BuildingArea> m_AreaWithoutAreaRuleset = new List<ABS_BuildingArea>();
        private bool m_AreaWithoutAreaRulesetDetails = false;

        private List<ABS_BuildingArea> m_AreaWithoutLayerCollection = new List<ABS_BuildingArea>();
        private bool m_AreaWithoutLayerCollectionDetails = false;

        #endregion BuildingArea problems
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region View
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Init (in ABS_EditorStyleContainer p_EditorStyleContainer)
        {
            Clear();

            m_Analized = false;
            m_HighlightCollectionGUIContent = new GUIContent("Highlight Collection",
                "The HighlightCollection what will be added to the elements for fixing the missing collection error.");
        }

        public void AnalyzeView(in ABS_EditorStyleContainer p_EditorStyleContainer)
        {
            m_EditorStyleContainer = p_EditorStyleContainer;
            bool buttonResult = GUILayout.Button(
                    "Analyze",
                    m_EditorStyleContainer.DarkButtonStyle,
                    GUILayout.ExpandWidth(true)
                );

            if (m_ReanalizationNeeded)
            {
                EditorGUILayout.HelpBox("Reanalization Needed!", MessageType.Warning);
            }

            m_ScrollPos = ABS_EditorUtils.StartScrollView(m_ScrollPos);
            {
                if (buttonResult)
                {
                    m_Analized = true;
                    Clear();

                    Scene currentScene = SceneManager.GetActiveScene();
                    GameObject[] rootGameObjects = currentScene.GetRootGameObjects();

                    foreach (GameObject rootGo in rootGameObjects)
                    {
                        SearchGameObjects(rootGo);
                    }
                }

                if (m_Analized)
                {
                    Report();
                }
            }
            ABS_EditorUtils.EndScrollView();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion View
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Clear
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Clear()
        {
            m_ReanalizationNeeded = false;

            m_ScriptDuplication.Clear();
            m_ScriptConflictDetails = false;

            m_ScriptConflict.Clear();
            m_ScriptDuplicationDetails = false;

            m_BuildingParent_Under_BuildingParent.Clear();
            m_BuildingParent_Under_Building.Clear();
            m_BuildingParent_Under_BuildingElement.Clear();
            m_BuildingParent_Under_BuildingElementLink.Clear();
            m_Building_NotUnder_BuildingParent.Clear();
            m_Building_Under_Building.Clear();
            m_Building_Under_BuildingElement.Clear();
            m_Building_Under_BuildingElementLink.Clear();
            m_BuildingElement_Under_BuildingParent.Clear();
            m_BuildingElement_NotUnder_Building.Clear();
            m_BuildingElement_Under_BuildingElement.Clear();
            m_BuildingElement_Under_BuildingElementLink.Clear();
            m_BuildingElementLink_Under_BuildingParent.Clear();
            m_BuildingElementLink_Under_Building.Clear();
            m_BuildingElementLink_NotUnder_BuildingElement.Clear();
            m_OtherElementsUnderBuildingParent.Clear();
            m_OtherElementsUnderBuilding.Clear();
            m_ParentingErrorCounter = 0;
            m_ParentingErrorDetails = false;

            m_ElementMissingHighlight.Clear();
            m_ElementMissingHighlightDetails = false;

            m_ElementMissingRenderers.Clear();
            m_ElementMissingRenderersDetails = false;

            m_ObjectWithNullInstanceGuid.Clear();
            m_ObjectWithNullInstanceGuidDetails = false;
            m_ObjectWithNullPrefabGuid.Clear();
            m_ObjectWithNullPrefabGuidDetails = false;

            m_ObjectNotFixedInstanceGuid.Clear();
            m_ObjectNotFixedInstanceGuidDetails = false;

            m_InstanceGuids.Clear();
            m_DuplicatedInstaceGuidDetails = false;

            m_ElementLinkWithNullTarget.Clear();
            m_ElementLinkWithNullTargetGuidDetails = false;

            m_ElementsUnderWrongTypeBuilding.Clear();
            m_ElementsUnderWrongTypeBuildingMultiple.Clear();
            m_ElementsUnderWrongTypeBuildingDetails = false;

            m_ElementsWithWrongAlgorithmSettings.Clear();
            m_ElementsWithWrongAlgorithmSettingsDetails = false;

            //BuildingElement AdvancedGrid issues
            m_AdvancedGridBuildingWrongGridPosition.Clear();
            m_AdvancedGridBuildingWrongGridPositionDetails = false;

            m_AdvancedGridBuildingWrongGridRotation.Clear();
            m_AdvancedGridBuildingWrongGridRotationDetails = false;

            //BuildingElement BasicGrid issues
            m_BasicGridBuildingWrongGridPosition.Clear();
            m_BasicGridBuildingWrongGridPositionDetails = false;

            //BuildingParent
            m_BuildingParentHasFound = false;
            m_BuildingParentHasFoundDetails = false;
            m_ParentWithMissingGlobalFreeBuilding.Clear();
            m_ParentWithMissingGlobalGridBuilding.Clear();
            m_ParentWithMissingGlobalFreeBuildingDetails = false;
            m_ParentWithMissingGlobalGridBuildingDetails = false;

            //BuildingManager
            m_BuildingManagerHasFound = false;
            m_BuildingManagerHasFoundDetails = false;
            m_ManagerWithoutBuildingElementList.Clear();
            m_ManagerWithoutBuildingElementListDetails = false;
            m_ManagerWithoutBuildingParent.Clear();
            m_ManagerWithoutBuildingParentDetails = false;

            //Building Area
            m_AreaWithoutAreaRuleset.Clear();
            m_AreaWithoutAreaRulesetDetails = false;
            m_AreaWithoutLayerCollection.Clear();
            m_AreaWithoutLayerCollectionDetails = false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Clear
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Check Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SearchGameObjects(GameObject p_SearchTarget)
        {
            bool buildingParent = false, link = false;
            ABS_Building[] buildingComponents = null;
            ABS_BuildingElement[] buildingElementComponents = null;
            AnalizeOne(p_SearchTarget, out buildingComponents, out buildingElementComponents, out buildingParent, out link, false, null);
            bool building = buildingComponents.Length > 0;
            bool element = buildingElementComponents.Length > 0;

            for (int i = 0; i < p_SearchTarget.transform.childCount; i++)
            {
                GameObject child = p_SearchTarget.transform.GetChild(i).gameObject;
                SearchGameObjectsReq(child, buildingComponents, buildingParent, building, buildingParent, building, element, link);
            }
        }
        private void SearchGameObjectsReq(GameObject p_SearchTarget,
                                          ABS_Building[] p_ParentBuildings,
                                          in bool p_DirectUnderBuildingParent,
                                          in bool p_DirectUnderBuilding,
                                          in bool p_UnderBuildingParent,
                                          in bool p_UnderBuilding,
                                          in bool p_UnderBuildingElement,
                                          in bool p_UnderBuildingElementLink)
        {
            bool buildingParent = false, link = false;
            ABS_Building[] buildingComponents = null;
            ABS_BuildingElement[] buildingElementComponents = null;
            AnalizeOne(p_SearchTarget,
                        out buildingComponents,
                        out buildingElementComponents,
                        out buildingParent,
                        out link,
                        p_DirectUnderBuilding,
                        p_ParentBuildings);
            bool building = buildingComponents.Length > 0;
            bool element = buildingElementComponents.Length > 0;

            CheckParentingIssues(
                    p_SearchTarget: p_SearchTarget,
                    p_BuildingParent: buildingParent,
                    p_Building: building,
                    p_BuildingElement: element,
                    p_BuildingElementLink: link,
                    p_DirectUnderBuildingParent: p_DirectUnderBuildingParent,
                    p_DirectUnderBuilding: p_DirectUnderBuilding,
                    p_UnderBuildingParent: p_UnderBuildingParent,
                    p_UnderBuilding: p_UnderBuilding,
                    p_UnderBuildingElement: p_UnderBuildingElement,
                    p_UnderBuildingElementLink: p_UnderBuildingElementLink);

            CheckBuildingElementType(p_ParentBuildings, buildingElementComponents);

            for (int i = 0; i < p_SearchTarget.transform.childCount; i++)
            {
                GameObject child = p_SearchTarget.transform.GetChild(i).gameObject;
                SearchGameObjectsReq(
                    p_SearchTarget: child,
                    p_ParentBuildings: buildingComponents,
                    p_DirectUnderBuildingParent: buildingParent,
                    p_DirectUnderBuilding: building,
                    p_UnderBuildingParent: buildingParent || p_UnderBuildingParent,
                    p_UnderBuilding: building || p_UnderBuilding,
                    p_UnderBuildingElement: element || p_UnderBuildingElement,
                    p_UnderBuildingElementLink: link || p_UnderBuildingElementLink);
            }
        }

        private void CheckBuildingElementType(
            ABS_Building[] p_ParentBuildingComponents,
            ABS_BuildingElement[] buildingElementComponents)
        {
            if (p_ParentBuildingComponents == null
                || p_ParentBuildingComponents.Length == 0
                || buildingElementComponents == null
                || buildingElementComponents.Length == 0)
            {
                return;
            }

            foreach (ABS_BuildingElement element in buildingElementComponents)
            {
                foreach (ABS_Building building in p_ParentBuildingComponents)
                {
                    if (element.PositionSearchAlgorithm != building.PositionSearchAlgorithmType)
                    {
                        if (p_ParentBuildingComponents.Length > 1)
                        {
                            m_ElementsUnderWrongTypeBuildingMultiple.Add(element);
                        }
                        else
                        {
                            m_ElementsUnderWrongTypeBuilding.Add(element);
                        }
                    }
                }

                if (element.PositionAlgorithmSettings == null)
                {
                    m_ElementsWithWrongAlgorithmSettings.Add(element);
                }
                else
                {
                    switch (element.PositionSearchAlgorithm)
                    {
                        case ABS_PositionSearchAlgorithm.Free:
                            if (element.PositionAlgorithmSettings is not ABS_FreeBuilderSettings)
                            {
                                m_ElementsWithWrongAlgorithmSettings.Add(element);
                            }
                            break;
                        case ABS_PositionSearchAlgorithm.BasicGrid:
                            if (element.PositionAlgorithmSettings is not ABS_BasicGridBuilderSettings)
                            {
                                m_ElementsWithWrongAlgorithmSettings.Add(element);
                            }
                            break;
                        case ABS_PositionSearchAlgorithm.AdvancedGrid:
                            if (element.PositionAlgorithmSettings is not ABS_AdvancedGridBuilderSettings)
                            {
                                m_ElementsWithWrongAlgorithmSettings.Add(element);
                            }
                            break;
                        case ABS_PositionSearchAlgorithm.SnapPointBased:
                            if (element.PositionAlgorithmSettings is not ABS_SnapPointBasedBuilderSettings)
                            {
                                m_ElementsWithWrongAlgorithmSettings.Add(element);
                            }
                            break;
                    }
                }
            }
        }

        private void CheckParentingIssues(GameObject p_SearchTarget,
                                           in bool p_BuildingParent,
                                           in bool p_Building,
                                           in bool p_BuildingElement,
                                           in bool p_BuildingElementLink,
                                           in bool p_DirectUnderBuildingParent,
                                           in bool p_DirectUnderBuilding,
                                           in bool p_UnderBuildingParent,
                                           in bool p_UnderBuilding,
                                           in bool p_UnderBuildingElement,
                                           in bool p_UnderBuildingElementLink)
        {
            if (p_BuildingParent)
            {
                if (p_UnderBuildingParent)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingParent_Under_BuildingParent.Add(p_SearchTarget);
                }
                if (p_UnderBuilding)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingParent_Under_Building.Add(p_SearchTarget);
                }
                if (p_UnderBuildingElement)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingParent_Under_BuildingElement.Add(p_SearchTarget);
                }
                if (p_UnderBuildingElementLink)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingParent_Under_BuildingElementLink.Add(p_SearchTarget);
                }
            }

            if (p_Building)
            {
                if (!p_DirectUnderBuildingParent)
                {
                    ++m_ParentingErrorCounter;
                    m_Building_NotUnder_BuildingParent.Add(p_SearchTarget);
                }
                if (p_UnderBuilding)
                {
                    ++m_ParentingErrorCounter;
                    m_Building_Under_Building.Add(p_SearchTarget);
                }
                if (p_UnderBuildingElement)
                {
                    ++m_ParentingErrorCounter;
                    m_Building_Under_BuildingElement.Add(p_SearchTarget);
                }
                if (p_UnderBuildingElementLink)
                {
                    ++m_ParentingErrorCounter;
                    m_Building_Under_BuildingElementLink.Add(p_SearchTarget);
                }
            }

            if (p_BuildingElement)
            {
                if (p_DirectUnderBuildingParent)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElement_Under_BuildingParent.Add(p_SearchTarget);
                }
                if (!p_DirectUnderBuilding)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElement_NotUnder_Building.Add(p_SearchTarget);
                }
                if (p_UnderBuildingElement)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElement_Under_BuildingElement.Add(p_SearchTarget);
                }
                if (p_UnderBuildingElementLink)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElement_Under_BuildingElementLink.Add(p_SearchTarget);
                }
            }

            if (p_BuildingElementLink)
            {
                if (p_DirectUnderBuildingParent)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElementLink_Under_BuildingParent.Add(p_SearchTarget);
                }
                if (p_DirectUnderBuilding)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElementLink_Under_Building.Add(p_SearchTarget);
                }
                if (!p_UnderBuildingElement)
                {
                    ++m_ParentingErrorCounter;
                    m_BuildingElementLink_NotUnder_BuildingElement.Add(p_SearchTarget);
                }
            }

            if (p_DirectUnderBuildingParent
                && !p_BuildingParent
                && !p_Building
                && !p_BuildingElement
                && !p_BuildingElementLink)
            {
                m_OtherElementsUnderBuildingParent.Add(p_SearchTarget);
            }

            if (p_DirectUnderBuilding
                && !p_BuildingParent
                && !p_Building
                && !p_BuildingElement
                && !p_BuildingElementLink)
            {
                m_OtherElementsUnderBuilding.Add(p_SearchTarget);
            }
        }

        private void AnalizeOne(
            GameObject p_AnalizeTarget,
            out ABS_Building[] p_Buildings,
            out ABS_BuildingElement[] p_BuildingElements,
            out bool p_BuildingParent,
            out bool p_BuildingElementLink,
            in bool p_DirectUnderBuilding,
            in ABS_Building[] p_ParentBuildings)
        {
            byte conflictCounter = 0;
            byte duplicationCounter = 0;

            ABS_BuildingParent[] buildingParents = p_AnalizeTarget.GetComponents<ABS_BuildingParent>();
            if (buildingParents != null && buildingParents.Length > 0)
            {
                p_BuildingParent = true;
                m_BuildingParentHasFound |= true;
                ++conflictCounter;
                if (buildingParents.Length > 1)
                {
                    ++duplicationCounter;
                }
                foreach (ABS_BuildingParent parent in buildingParents)
                {
                    CheckGuid(parent);
                    if (parent.GlobalBasicGridParent == null)
                    {
                        m_ParentWithMissingGlobalGridBuilding.Add(parent);
                    }
                    if (parent.GlobalFreeParent == null)
                    {
                        m_ParentWithMissingGlobalFreeBuilding.Add(parent);
                    }
                }
            }
            else
            {
                p_BuildingParent = false;
            }

            p_Buildings = p_AnalizeTarget.GetComponents<ABS_Building>();
            if (p_Buildings != null && p_Buildings.Length > 0)
            {
                ++conflictCounter;
                if (p_Buildings.Length > 1)
                {
                    ++duplicationCounter;
                }

                foreach (ABS_Building building in p_Buildings)
                {
                    CheckGuid(building);
                }
            }

            p_BuildingElements = p_AnalizeTarget.GetComponents<ABS_BuildingElement>();
            if (p_BuildingElements != null && p_BuildingElements.Length > 0)
            {
                ++conflictCounter;
                if (p_BuildingElements.Length > 1)
                {
                    ++duplicationCounter;
                }

                foreach (ABS_BuildingElement element in p_BuildingElements)
                {
                    AnalizeBuildingElement(element);
                    CheckGuid(element);
                    ;
                    if (!p_DirectUnderBuilding || p_ParentBuildings.Length != 1)
                    {
                        continue;
                    }
                    else if (element.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                    {
                        CheckAdvancedGridBuildingWrongGridPosition(element, p_ParentBuildings[0] as ABS_AdvancedGridBuilding);
                    }
                    else if (element.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.BasicGrid)
                    {
                        CheckBasicGridBuildingWrongGridPosition(element, p_ParentBuildings[0] as ABS_BasicGridBuilding);
                    }
                }
            }

            ABS_BuildingElementLink[] buildingElementLinks = p_AnalizeTarget.GetComponents<ABS_BuildingElementLink>();
            if (buildingElementLinks != null && buildingElementLinks.Length > 0)
            {
                p_BuildingElementLink = true;
                ++conflictCounter;
                if (buildingElementLinks.Length > 1)
                {
                    ++duplicationCounter;
                }

                foreach (ABS_BuildingElementLink link in buildingElementLinks)
                {
                    if (link.m_Element == null)
                    {
                        m_ElementLinkWithNullTarget.Add(link);
                    }
                }
            }
            else
            {
                p_BuildingElementLink = false;
            }

            ABS_BuildingArea[] areas = p_AnalizeTarget.GetComponents<ABS_BuildingArea>();
            if (areas != null && areas.Length > 0)
            {
                foreach (ABS_BuildingArea area in areas)
                {
                    if (area.LayerCollection == null)
                    {
                        m_AreaWithoutLayerCollection.Add(area);
                    }

                    if (area.Rules == null)
                    {
                        m_AreaWithoutAreaRuleset.Add(area);
                    }
                }
            }
            else
            {
                p_BuildingElementLink = false;
            }

            ABS_BuildingManager[] managers = p_AnalizeTarget.GetComponents<ABS_BuildingManager>();
            if (managers != null && managers.Length > 0)
            {
                m_BuildingManagerHasFound |= true;

                foreach (ABS_BuildingManager manager in managers)
                {
                    if (manager.ElementList == null)
                    {
                        m_ManagerWithoutBuildingElementList.Add(manager);
                    }
                    if (manager.BuildingParent == null)
                    {
                        m_ManagerWithoutBuildingParent.Add(manager);
                    }
                }
            }

            if (conflictCounter > 1)
            {
                m_ScriptConflict.Add(p_AnalizeTarget);
            }
            if (duplicationCounter > 0)
            {
                m_ScriptDuplication.Add(p_AnalizeTarget);
            }
        }

        private void CheckAdvancedGridBuildingWrongGridPosition(ABS_BuildingElement p_Element, ABS_AdvancedGridBuilding p_Building)
        {
            Vector3 nearestGridPosition = ABS_AdvancedGirdBuilderGridHelper.GetGridPosition(
                p_Element.transform.localPosition - p_Building.BuildingPositionModifier,
                p_Element
            );
            nearestGridPosition += p_Building.BuildingPositionModifier;

            if (!REST_Vector3EqualityComparer.Static_Equals(p_Element.transform.localPosition, nearestGridPosition))
            {
                m_AdvancedGridBuildingWrongGridPosition.Add(p_Element);
            }

            bool rotationIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(p_Element, p_Building);
            if (!REST_Vector3EqualityComparer.Static_Equals(
                    p_Element.transform.localEulerAngles,
                    rotationIsNeeded ? ABS_AdvancedGirdBuilderGridHelper.s_RotationModifier : Vector3.zero))
            {
                m_AdvancedGridBuildingWrongGridRotation.Add(p_Element);
            }
        }

        private void CheckBasicGridBuildingWrongGridPosition(ABS_BuildingElement p_Element, ABS_BasicGridBuilding p_Building)
        {
            bool aligned = false;
            Vector3 gridPosition = ABS_BasicGridBuilder.GetGridPosition(p_Element.transform.position, p_Element, ref aligned);

            if (!REST_Vector3EqualityComparer.Static_Equals(p_Element.transform.localPosition, gridPosition))
            {
                m_BasicGridBuildingWrongGridPosition.Add(p_Element);
            }
        }

        private void CheckGuid(ABS_SaveableMonobehaviour p_Target)
        {
            if (!p_Target.FixedInstanceGuid)
            {
                m_ObjectNotFixedInstanceGuid.Add(p_Target);
            }
            else
            {
                if (p_Target.InstanceGuid == null || string.IsNullOrEmpty(p_Target.InstanceGuid))
                {
                    m_ObjectWithNullInstanceGuid.Add(p_Target);
                }
            }

            if (p_Target.PrefabGuid == null || string.IsNullOrEmpty(p_Target.PrefabGuid))
            {
                m_ObjectWithNullPrefabGuid.Add(p_Target);
            }
        }

        private void AnalizeBuildingElement(ABS_BuildingElement p_Target)
        {
            if (p_Target.HighlightStrategy != ABS_HighlightStrategy.None)
            {
                if (p_Target.HighlightCollection == null)
                {
                    m_ElementMissingHighlight.Add(p_Target);
                }
            }

            if (p_Target.HighlightStrategy == ABS_HighlightStrategy.Custom)
            {
                if (p_Target.Renderers.Count == 0)
                {
                    m_ElementMissingRenderers.Add(p_Target);
                }
            }

            List<ABS_SaveableMonobehaviour> elements = null;
            if (m_InstanceGuids.TryGetValue(p_Target.InstanceGuid, out elements) && elements != null)
            {
                elements.Add(p_Target);
            }
            else
            {
                elements = new List<ABS_SaveableMonobehaviour>();
                elements.Add(p_Target);
                m_InstanceGuids.Add(p_Target.InstanceGuid, elements);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Check Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Fix functions
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void FixingMissingHighlightCollection(List<ABS_BuildingElement> p_Targets)
        {
            ABS_EditorUtils.StartHorizontal();
            {
                EditorGUI.BeginDisabledGroup(s_HighlightCollection == null);
                {
                    if (PutFixButton())
                    {
                        foreach (ABS_BuildingElement element in p_Targets)
                        {
                            element.HighlightCollection = s_HighlightCollection;
                            ABS_EditorUtils.Dirty(element);
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                ABS_EditorUtils.HorizontalSpace(20);

                s_HighlightCollection = ABS_EditorUtils.AddObjectField<ABS_BuildingElementHighlightCollection>(
                    m_HighlightCollectionGUIContent,
                    s_HighlightCollection,
                false);

            }
            ABS_EditorUtils.EndHorizontal();
        }

        private void FixingMissingRenderers(List<ABS_BuildingElement> p_Targets)
        {
            bool buttonResult = GUILayout.Button(
                            "Fix",
                            m_EditorStyleContainer.SmallDarkButtonStyle,
                            GUILayout.Width(50),
                            GUILayout.Height(40)
                        );
            if (PutFixButton())
            {
                foreach (ABS_BuildingElement element in p_Targets)
                {
                    ABS_BuildingElementEditor.CollectRenderers(element);
                }
            }
        }

        private void FixingNotFixedInstanceGuid(List<ABS_SaveableMonobehaviour> p_Targets)
        {
            if (PutFixButton())
            {
                foreach (ABS_SaveableMonobehaviour element in p_Targets)
                {
                    element.FixedInstanceGuid = true;
                    ABS_EditorUtils.Dirty(element);
                }
            }
        }

        private void FixingNullInstanceGuid(List<ABS_SaveableMonobehaviour> p_Targets)
        {
            if (PutFixButton())
            {
                foreach (ABS_SaveableMonobehaviour element in p_Targets)
                {
                    element.GenerateNewInstanceGuid();
                    ABS_EditorUtils.Dirty(element);
                }
            }
        }

        private void FixingNullPrefabGuid(List<ABS_SaveableMonobehaviour> p_Targets)
        {
            m_ABSObject = ABS_EditorUtils.AddObjectField<ABS_SaveableMonobehaviour>("Target Object", m_ABSObject, true);
            m_AddClonePostfix = ABS_EditorUtils.AddBooleanField("Add (Clone) postfix", m_AddClonePostfix);
            if (PutFixButton("Copy guid from target based on Name", 220))
            {
                System.Type targetType = m_ABSObject.GetType();
                foreach (ABS_SaveableMonobehaviour item in p_Targets)
                {
                    if (item.GetType() == targetType
                        && string.Compare(item.name, m_AddClonePostfix ? $"{m_ABSObject.name}(Clone)" : m_ABSObject.name) == 0)
                    {
                        item.PrefabGuid = m_ABSObject.PrefabGuid;
                        ABS_EditorUtils.Dirty(item);
                    }
                }
            }

            if (PutFixButton("Generate new PrefabGuid for all", 200))
            {
                foreach (ABS_SaveableMonobehaviour item in p_Targets)
                {
                    item.GenerateNewPrefabGuid();
                    ABS_EditorUtils.Dirty(item);
                }
            }
        }

        private void FixingDuplicatedInstanceGuid(Dictionary<string, List<ABS_SaveableMonobehaviour>> p_Targets)
        {
            if (PutFixButton())
            {
                foreach ((string guid, List<ABS_SaveableMonobehaviour> wrongObjects) in p_Targets)
                {
                    foreach (ABS_SaveableMonobehaviour item in wrongObjects)
                    {
                        item.GenerateNewInstanceGuid();
                        ABS_EditorUtils.Dirty(item);
                    }
                }
            }
        }

        private void FixingWrongTypeParent(List<ABS_BuildingElement> p_Targets)
        {
            if (PutFixButton())
            {
                foreach (ABS_BuildingElement element in p_Targets)
                {
                    ABS_PositionSearchAlgorithm parentType =
                        element.gameObject.transform.parent.gameObject.GetComponent<ABS_Building>().PositionSearchAlgorithmType;
                    element.PositionSearchAlgorithm = parentType;
                    ABS_EditorUtils.Dirty(element);
                }
            }
        }

        private void FixingWrongAdvancedGridPosition(List<ABS_BuildingElement> p_Targets)
        {
            if (PutFixButton())
            {
                foreach (ABS_BuildingElement element in p_Targets)
                {
                    ABS_AdvancedGridBuilding building = element.transform.parent.GetComponent<ABS_AdvancedGridBuilding>();
                    if (building == null)
                    {
                        REST_Logging.Error($"{this}", "Wrong BuildingType");
                        continue;
                    }

                    Vector3 nearestGridPosition = ABS_AdvancedGirdBuilderGridHelper.GetGridPosition(
                        element.transform.localPosition - building.BuildingPositionModifier,
                        element
                    );
                    nearestGridPosition += building.BuildingPositionModifier;

                    element.transform.localPosition = nearestGridPosition;
                    ABS_EditorUtils.Dirty(element);
                }
            }
        }

        private void FixingWrongAdvancedGridRotation(List<ABS_BuildingElement> p_Targets)
        {
            if (PutFixButton())
            {
                foreach (ABS_BuildingElement element in p_Targets)
                {
                    ABS_AdvancedGridBuilding building = element.transform.parent.GetComponent<ABS_AdvancedGridBuilding>();
                    if (building == null)
                    {
                        REST_Logging.Error($"{this}", "Wrong BuildingType");
                        continue;
                    }

                    bool rotationIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(element, building);
                    element.transform.localRotation = rotationIsNeeded
                        ? Quaternion.Euler(ABS_AdvancedGirdBuilderGridHelper.s_RotationModifier)
                        : Quaternion.identity;

                    ABS_EditorUtils.Dirty(element);
                }
            }
        }
        private void FixingWrongBasicGridPosition(List<ABS_BuildingElement> p_Targets)
        {
            if (PutFixButton())
            {
                foreach (ABS_BuildingElement element in p_Targets)
                {
                    bool aligned = false;
                    element.transform.position = ABS_BasicGridBuilder.GetGridPosition(element.transform.position, element, ref aligned);

                    ABS_EditorUtils.Dirty(element);
                }
            }
        }

        private void FixingWrongAlgorithmSettings(List<ABS_BuildingElement> p_Targets)
        {
            s_FreeBuilderSettings = ABS_EditorUtils.AddObjectField<ABS_FreeBuilderSettings>("FreeBuilding Settings", s_FreeBuilderSettings, false);
            s_BasicGridBuilderSettings = ABS_EditorUtils.AddObjectField<ABS_BasicGridBuilderSettings>("BasicGrid Settings", s_BasicGridBuilderSettings, false);
            s_AdvancedGridBuilderSettings = ABS_EditorUtils.AddObjectField<ABS_AdvancedGridBuilderSettings>("AdvancedGrid Settings", s_AdvancedGridBuilderSettings, false);
            s_SnapPointBasedBuilderSettings = ABS_EditorUtils.AddObjectField<ABS_SnapPointBasedBuilderSettings>("SnapPointBased Settings", s_SnapPointBasedBuilderSettings, false);

            if (PutFixButton())
            {
                foreach (ABS_BuildingElement element in p_Targets)
                {
                    switch (element.PositionSearchAlgorithm)
                    {
                        case ABS_PositionSearchAlgorithm.Free:
                            if (s_FreeBuilderSettings != null)
                            {
                                element.PositionAlgorithmSettings = s_FreeBuilderSettings;
                                ABS_EditorUtils.Dirty(element);
                            }
                            break;
                        case ABS_PositionSearchAlgorithm.BasicGrid:
                            if (s_BasicGridBuilderSettings != null)
                            {
                                element.PositionAlgorithmSettings = s_BasicGridBuilderSettings;
                                ABS_EditorUtils.Dirty(element);
                            }

                            break;
                        case ABS_PositionSearchAlgorithm.AdvancedGrid:
                            if (s_AdvancedGridBuilderSettings != null)
                            {
                                element.PositionAlgorithmSettings = s_AdvancedGridBuilderSettings;
                                ABS_EditorUtils.Dirty(element);
                            }

                            break;
                        case ABS_PositionSearchAlgorithm.SnapPointBased:
                            if (s_SnapPointBasedBuilderSettings != null)
                            {
                                element.PositionAlgorithmSettings = s_SnapPointBasedBuilderSettings;
                                ABS_EditorUtils.Dirty(element);
                            }

                            break;
                    }
                }
            }
        }

        private void FixingMissingGlobalFreeBuilding(List<ABS_BuildingParent> p_Parents)
        {
            if (PutFixButton("Create new global FreeBuildings", 200))
            {
                foreach (ABS_BuildingParent parent in p_Parents)
                {
                    ABS_BuildingParentEditor.CreateGlobalFreeBuilding(parent);
                }
            }
        }

        private void FixingMissingGlobalBasicGridBuilding(List<ABS_BuildingParent> p_Parents)
        {
            if (PutFixButton("Create new global BasicGridBuildings", 230))
            {
                foreach (ABS_BuildingParent parent in p_Parents)
                {
                    ABS_BuildingParentEditor.CreateGlobalBasicGridBuilding(parent);
                }
            }
        }

        private void FixingMissingBuildingParent()
        {
            if (PutFixButton("Create BuildingParent", 150))
            {
                ABS_BuildingParentEditor.CreateBuildingParent();
            }
        }

        private void FixingMissingBuildingManager()
        {
            if (PutFixButton("Create BuildingManager", 150))
            {
                ABS_BuildingManagerEditor.CreateBuildingManager();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Fix functions
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Report Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool PutFixButton(string p_Text = "Hotfix", int p_Width = 50)
        {
            bool buttonPRessed = GUILayout.Button(
                                    p_Text,
                                    m_EditorStyleContainer.SmallDarkButtonStyle,
                                    GUILayout.Width(p_Width),
                                    GUILayout.Height(40)
                                );

            m_ReanalizationNeeded |= true;
            return buttonPRessed;
        }

        private void Report()
        {
            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("Script problems", m_EditorStyleContainer.HeadStyleSection);

            ReportImpl(p_FixableReportTargets: m_ScriptConflict,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ScriptConflictDetails,
                       p_ProblemText: "Script Conflict",
                       p_IssueDetailText: "The following GameObjects has more them one from the following scripts : " +
                                           "\n    - ABS_BuildingParent" +
                                           "\n    - ABS_Building" +
                                           "\n    - ABS_BuildingElement" +
                                           "\n    - ABS_BuildingElementLink",
                       p_FixingDelegate: null);

            ReportImpl(p_FixableReportTargets: m_ScriptDuplication,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ScriptDuplicationDetails,
                       p_ProblemText: "Duplicated Script",
                       p_IssueDetailText: "The following GameObjects has duplicated script from the following scripts : " +
                                           "\n    - ABS_BuildingParent" +
                                           "\n    - ABS_Building" +
                                           "\n    - ABS_BuildingElement" +
                                           "\n    - ABS_BuildingElementLink",
                       p_FixingDelegate: null);

            ReportparentingIssues();

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("Guid problems", m_EditorStyleContainer.HeadStyleSection);

            ReportImpl(p_FixableReportTargets: m_ObjectWithNullPrefabGuid,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ObjectWithNullPrefabGuidDetails,
                       p_ProblemText: "Null PrefabGuid",
                       p_IssueDetailText: "The following GameObjects has ABS Component with null PrefabGuid",
                       p_FixingDelegate: FixingNullPrefabGuid);

            ReportImpl(p_FixableReportTargets: m_ObjectNotFixedInstanceGuid,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ObjectNotFixedInstanceGuidDetails,
                       p_ProblemText: "Not fixed InstanceGuid",
                       p_IssueDetailText: "The following GameObjects has BuildingElement Component with not fixed InstanceGuid",
                       p_FixingDelegate: FixingNotFixedInstanceGuid);

            ReportImpl(p_FixableReportTargets: m_ObjectWithNullInstanceGuid,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ObjectWithNullInstanceGuidDetails,
                       p_ProblemText: "Null InstanceGuid",
                       p_IssueDetailText: "The following GameObjects has ABS Component with Fixed InstanceGuid enabled but with null InstanceGuid",
                       p_FixingDelegate: FixingNullInstanceGuid);

            ReportImpl(p_ReportTargets: m_InstanceGuids,
                       p_TargetDetails: ref m_DuplicatedInstaceGuidDetails,
                       p_ProblemCount: 2,
                       p_ProblemText: "Duplicated InstanceGuid",
                       p_IssueDetailText: "The following GameObjects has an ABS Component with duplicated InstanceGuid",
                       p_FixingDelegate: FixingDuplicatedInstanceGuid);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("Building Element problems", m_EditorStyleContainer.HeadStyleSection);

            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("Highlight problems", m_EditorStyleContainer.HeadStyleBasicProperties);
            ReportImpl(p_FixableReportTargets: m_ElementMissingHighlight,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ElementMissingHighlightDetails,
                       p_ProblemText: "Missing Highlight Collection",
                       p_IssueDetailText: "The following GameObjects has BuildingElement Component with missing Highlight Collection ",
                       p_FixingDelegate: FixingMissingHighlightCollection);

            ReportImpl(p_FixableReportTargets: m_ElementMissingRenderers,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ElementMissingRenderersDetails,
                       p_ProblemText: "Missing Renderers",
                       p_IssueDetailText: "The following GameObjects has BuildingElement component with custom higlight strategy but empty renderes list",
                       p_FixingDelegate: FixingMissingRenderers);

            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("Algorithm problems", m_EditorStyleContainer.HeadStyleBasicProperties);
            ReportImpl(p_FixableReportTargets: m_ElementsUnderWrongTypeBuilding,
                       p_NotFixableReportTargets: m_ElementsUnderWrongTypeBuildingMultiple,
                       p_TargetDetails: ref m_ElementsUnderWrongTypeBuildingDetails,
                       p_ProblemText: "BuildingElement under wrong typed Building",
                       p_IssueDetailText: "The following GameObjects has BuildingElement component with not matching algorithm type as it's parent building",
                       p_FixingDelegate: FixingWrongTypeParent);

            ReportImpl(p_FixableReportTargets: m_ElementsWithWrongAlgorithmSettings,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ElementsWithWrongAlgorithmSettingsDetails,
                       p_ProblemText: "BuildingElement wrong algorithm settings",
                       p_IssueDetailText: "The following GameObjects has BuildingElement component with null or not matching algorithm settings with it's algorithm type",
                       p_FixingDelegate: FixingWrongAlgorithmSettings);

            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("AdvanceGrid problems", m_EditorStyleContainer.HeadStyleBasicProperties);
            ReportImpl(p_FixableReportTargets: m_AdvancedGridBuildingWrongGridPosition,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_AdvancedGridBuildingWrongGridPositionDetails,
                       p_ProblemText: "Wrong grid position",
                       p_IssueDetailText: "The following GameObjects has BuildingElement component with AdvancedGridBuilding wrong grid position",
                       p_FixingDelegate: FixingWrongAdvancedGridPosition);

            ReportImpl(p_FixableReportTargets: m_AdvancedGridBuildingWrongGridRotation,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_AdvancedGridBuildingWrongGridRotationDetails,
                       p_ProblemText: "Wrong grid rotation",
                       p_IssueDetailText: "The following GameObjects has BuildingElement component with AdvancedGridBuilding wrong grid rotation",
                       p_FixingDelegate: FixingWrongAdvancedGridRotation);

            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("BasicGrid problems", m_EditorStyleContainer.HeadStyleBasicProperties);
            ReportImpl(p_FixableReportTargets: m_BasicGridBuildingWrongGridPosition,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_BasicGridBuildingWrongGridPositionDetails,
                       p_ProblemText: "Wrong grid position",
                       p_IssueDetailText: "The following GameObjects has BuildingElement component with BasicGridBuilding wrong grid position",
                       p_FixingDelegate: FixingWrongBasicGridPosition);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("Building Element Link problems", m_EditorStyleContainer.HeadStyleSection);

            //TODO add fix function
            ReportImpl(p_FixableReportTargets: m_ElementLinkWithNullTarget,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ElementLinkWithNullTargetGuidDetails,
                       p_ProblemText: "BuildingElementLink with null target",
                       p_IssueDetailText: "The following GameObjects has BuildingElementLink component with null target element",
                       p_FixingDelegate: null);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("BuildingParent problems", m_EditorStyleContainer.HeadStyleSection);

            ReportMissingObject<ABS_BuildingParent>(
                p_ObjectHasFound: m_BuildingParentHasFound,
                p_TargetDetails: ref m_BuildingParentHasFoundDetails,
                p_ObjectName: "BuildingParent",
                p_FixingDelegate: FixingMissingBuildingParent);


            ReportImpl(p_FixableReportTargets: m_ParentWithMissingGlobalFreeBuilding,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ParentWithMissingGlobalFreeBuildingDetails,
                       p_ProblemText: "BuildingParent with missing Global FreeBuilding",
                       p_IssueDetailText: "The following GameObjects has BuildingParent component with null Global FreeBuilding",
                       p_FixingDelegate: FixingMissingGlobalFreeBuilding);

            ReportImpl(p_FixableReportTargets: m_ParentWithMissingGlobalGridBuilding,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_ParentWithMissingGlobalGridBuildingDetails,
                       p_ProblemText: "BuildingParent with missing Globa GridBuilding",
                       p_IssueDetailText: "The following GameObjects has BuildingParent component with null Globa GridBuilding",
                       p_FixingDelegate: FixingMissingGlobalBasicGridBuilding);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("BuildingManager problems", m_EditorStyleContainer.HeadStyleSection);

            ReportMissingObject<ABS_BuildingManager>(
                p_ObjectHasFound: m_BuildingManagerHasFound,
                p_TargetDetails: ref m_BuildingManagerHasFoundDetails,
                p_ObjectName: "BuildingManager",
                p_FixingDelegate: FixingMissingBuildingManager);

            ReportImpl(p_FixableReportTargets: null,
                       p_NotFixableReportTargets: m_ManagerWithoutBuildingElementList,
                       p_TargetDetails: ref m_ManagerWithoutBuildingElementListDetails,
                       p_ProblemText: "BuildingManager with missing BuildingElementList",
                       p_IssueDetailText: "The following GameObjects has BuildingManager component with null BuildingElementList",
                       p_FixingDelegate: null);

            ReportImpl(p_FixableReportTargets: null,
                       p_NotFixableReportTargets: m_ManagerWithoutBuildingParent,
                       p_TargetDetails: ref m_ManagerWithoutBuildingParentDetails,
                       p_ProblemText: "BuildingManager with missing BuildingParent",
                       p_IssueDetailText: "The following GameObjects has BuildingManager component with null BuildingParent",
                       p_FixingDelegate: null);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("BuildingArea problems", m_EditorStyleContainer.HeadStyleSection);
            ReportImpl(p_FixableReportTargets: m_AreaWithoutAreaRuleset,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_AreaWithoutAreaRulesetDetails,
                       p_ProblemText: "BuildingArea with without Ruleset",
                       p_IssueDetailText: "The following GameObjects has BuildingArea component with null Ruleset",
                       p_FixingDelegate: null);

            ReportImpl(p_FixableReportTargets: m_AreaWithoutLayerCollection,
                       p_NotFixableReportTargets: null,
                       p_TargetDetails: ref m_AreaWithoutLayerCollectionDetails,
                       p_ProblemText: "BuildingArea without LayerCollection",
                       p_IssueDetailText: "The following GameObjects has BuildingArea component with null LayerCollection",
                       p_FixingDelegate: null);

        }

        private void ReportMissingObject<Type>(
            in bool p_ObjectHasFound,
            ref bool p_TargetDetails,
            in string p_ObjectName,
            in FixingMissingObjectIssueDelegate p_FixingDelegate)
            where Type : class
        {
            if (p_ObjectHasFound)
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                    ref coloredTextStyle,
                    $"{p_ObjectName} : Has found",
                    ABS_EditorStyleContainer.s_GreenColor);
                EditorGUILayout.LabelField(colorizesString, coloredTextStyle);
            }
            else
            {
                p_TargetDetails = EditorGUILayout.BeginFoldoutHeaderGroup(
                    foldout: p_TargetDetails,
                    content: $"{p_ObjectName} : Missing",
                    style: m_EditorStyleContainer.ColoredHeaderStyle_Red);

                if (p_TargetDetails)
                {
                    ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                    {
                        EditorGUILayout.HelpBox($"The Following required object is missing from the scene: {p_ObjectName}", MessageType.Error);

                        if (p_FixingDelegate != null)
                        {
                            ABS_EditorUtils.Space();
                            p_FixingDelegate();
                        }
                    }
                    ABS_EditorUtils.BoxEnd();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void ReportImpl<Type>(
            in List<Type> p_FixableReportTargets,
            in List<Type> p_NotFixableReportTargets,
            ref bool p_TargetDetails,
            in string p_ProblemText,
            in string p_IssueDetailText,
            in FixingIssueDelegate<Type> p_FixingDelegate)
            where Type : class
        {
            if (p_FixableReportTargets != null)
            {
                RemoveNullItems(p_FixableReportTargets);
            }
            if (p_NotFixableReportTargets != null)
            {
                RemoveNullItems(p_NotFixableReportTargets);
            }

            if ((p_FixableReportTargets == null || p_FixableReportTargets.Count == 0)
                && (p_NotFixableReportTargets == null || p_NotFixableReportTargets.Count == 0))
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                    ref coloredTextStyle,
                    $"{p_ProblemText} : No {p_ProblemText} was found",
                    ABS_EditorStyleContainer.s_GreenColor);
                EditorGUILayout.LabelField(colorizesString, coloredTextStyle);
            }
            else
            {
                int count = (p_FixableReportTargets == null ? 0 : p_FixableReportTargets.Count);
                count += (p_NotFixableReportTargets == null ? 0 : p_NotFixableReportTargets.Count);
                p_TargetDetails = EditorGUILayout.BeginFoldoutHeaderGroup(
                    foldout: p_TargetDetails,
                    content: $"{p_ProblemText} : {count} {p_ProblemText} was found",
                    style: m_EditorStyleContainer.ColoredHeaderStyle_Red);

                if (p_TargetDetails)
                {
                    ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                    {
                        EditorGUILayout.HelpBox(p_IssueDetailText, MessageType.Error);

                        if (p_FixableReportTargets != null)
                        {
                            if (p_FixingDelegate != null)
                            {
                                ABS_EditorUtils.Space();
                                p_FixingDelegate(p_FixableReportTargets);
                            }

                            foreach (Type go in p_FixableReportTargets)
                            {
                                ABS_EditorUtils.AddObjectLinkLabel(go as Object, 200);
                            }
                        }

                        if (p_NotFixableReportTargets != null)
                        {
                            if (p_FixingDelegate != null && p_NotFixableReportTargets.Count > 0)
                            {
                                ABS_EditorUtils.Space();
                                EditorGUILayout.HelpBox("The following items can not fixed", MessageType.Error);
                            }
                            foreach (Type go in p_NotFixableReportTargets)
                            {
                                ABS_EditorUtils.AddObjectLinkLabel(go as Object, 200);
                            }
                        }
                    }
                    ABS_EditorUtils.BoxEnd();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void ReportImpl<Type>(
            in Dictionary<string, List<Type>> p_ReportTargets,
            ref bool p_TargetDetails,
            in int p_ProblemCount,
            in string p_ProblemText,
            in string p_IssueDetailText,
            in FixingIssueDictionaryDelegate<Type> p_FixingDelegate)
            where Type : class
        {
            Dictionary<string, List<Type>> errorCases = new Dictionary<string, List<Type>>();
            foreach ((string value, List<Type> objectsInScene) in p_ReportTargets)
            {
                RemoveNullItems(objectsInScene);
                if (objectsInScene.Count >= p_ProblemCount)
                {
                    errorCases.Add(value, objectsInScene);
                }
            }

            List<string> keysToRemove = new List<string>();
            foreach (var pair in p_ReportTargets)
            {
                if (pair.Value == null || pair.Value.Count == 0)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            if (keysToRemove.Count > 0)
            {
                foreach (string key in keysToRemove)
                {
                    m_ReanalizationNeeded = true;
                    p_ReportTargets.Remove(key);
                }
            }

            if (errorCases.Count == 0)
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                    ref coloredTextStyle,
                    $"{p_ProblemText} : No {p_ProblemText} was found",
                    ABS_EditorStyleContainer.s_GreenColor);
                EditorGUILayout.LabelField(colorizesString, coloredTextStyle);
            }
            else
            {
                p_TargetDetails = EditorGUILayout.BeginFoldoutHeaderGroup(
                    foldout: p_TargetDetails,
                    content: $"{p_ProblemText} : {errorCases.Count} {p_ProblemText} was found",
                    style: m_EditorStyleContainer.ColoredHeaderStyle_Red);

                if (p_TargetDetails)
                {
                    ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                    {
                        EditorGUILayout.HelpBox(p_IssueDetailText, MessageType.Error);
                        if (p_FixingDelegate != null)
                        {
                            ABS_EditorUtils.Space();
                            p_FixingDelegate(errorCases);
                        }
                        foreach ((string value, List<Type> objectsInScene) in errorCases)
                        {
                            EditorGUILayout.LabelField(value, m_EditorStyleContainer.HeadStyleBasicProperties);
                            ABS_EditorUtils.IndentIn();
                            {
                                foreach (Type element in objectsInScene)
                                {
                                    ABS_EditorUtils.AddObjectLinkLabel(element as Object, 200);
                                }
                            }
                            ABS_EditorUtils.IndentOut();
                        }
                    }
                    ABS_EditorUtils.BoxEnd();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void ReportparentingIssues()
        {
            if (m_ParentingErrorCounter == 0)
            {
                GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
                string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                    ref coloredTextStyle,
                    "Script parenting issues : No script parenting issue was found",
                    ABS_EditorStyleContainer.s_GreenColor);
                EditorGUILayout.LabelField(colorizesString, coloredTextStyle);
            }
            else
            {
                m_ParentingErrorDetails = EditorGUILayout.BeginFoldoutHeaderGroup(
                    foldout: m_ParentingErrorDetails,
                    content: $"Script parenting issues : {m_ParentingErrorCounter} script parenting issues were found",
                    style: m_EditorStyleContainer.ColoredHeaderStyle_Red);

                if (m_ParentingErrorDetails)
                {
                    ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                    {
                        EditorGUILayout.HelpBox("The following GameObjects has ABS scipt component whit a parenting error", MessageType.Error);
                        ReportParentingIssueImpl(m_BuildingParent_Under_BuildingParent, "BuildingParent under another BuildingParent");
                        ReportParentingIssueImpl(m_BuildingParent_Under_Building, "BuildingParent under a Building");
                        ReportParentingIssueImpl(m_BuildingParent_Under_BuildingElement, "BuildingParent under a BuildingElement");
                        ReportParentingIssueImpl(m_BuildingParent_Under_BuildingElementLink, "BuildingParent under a BuildingElementLink");

                        ReportParentingIssueImpl(m_Building_NotUnder_BuildingParent, "Building not under a BuildingParent");
                        ReportParentingIssueImpl(m_Building_Under_Building, "Building under another Building");
                        ReportParentingIssueImpl(m_Building_Under_BuildingElement, "Building under a BuildingElement");
                        ReportParentingIssueImpl(m_Building_Under_BuildingElementLink, "Building under a BuildingElementLink");

                        ReportParentingIssueImpl(m_BuildingElement_Under_BuildingParent, "BuildingElement under a BuildingParent");
                        ReportParentingIssueImpl(m_BuildingElement_NotUnder_Building, "BuildingElement not under a Building");
                        ReportParentingIssueImpl(m_BuildingElement_Under_BuildingElement, "BuildingElement under another BuildingElement");
                        ReportParentingIssueImpl(m_BuildingElement_Under_BuildingElementLink, "BuildingElement under a BuildingElementLink");

                        ReportParentingIssueImpl(m_BuildingElementLink_Under_BuildingParent, "BuildingElementLink under another BuildingParent");
                        ReportParentingIssueImpl(m_BuildingElementLink_Under_Building, "BuildingElementLink under a Building");
                        ReportParentingIssueImpl(m_BuildingElementLink_NotUnder_BuildingElement, "BuildingElementLink not under a BuildingElement");

                        ReportParentingIssueImpl(m_OtherElementsUnderBuildingParent, "Not Building GameObject under the following BuildingParent");
                        ReportParentingIssueImpl(m_OtherElementsUnderBuilding, "Not BuildingElement GameObject under the following Building");
                    }
                    ABS_EditorUtils.BoxEnd();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void ReportParentingIssueImpl(List<GameObject> p_TargetList, string p_ErrorMsg)
        {
            m_ParentingErrorCounter -= RemoveNullItems(p_TargetList);
            if (p_TargetList.Count > 0)
            {
                EditorGUILayout.LabelField(p_ErrorMsg, m_EditorStyleContainer.HeadStyleBasicProperties);
                ABS_EditorUtils.IndentIn();
                {
                    foreach (GameObject go in p_TargetList)
                    {
                        ABS_EditorUtils.AddObjectLinkLabel(go as Object, 200);
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
        }

        private int RemoveNullItems<Type>(List<Type> p_ReportTargets) where Type : class
        {
            int removedCount = p_ReportTargets.RemoveAll(item => (item == null || item as Object == null));
            if (removedCount > 0)
            {
                m_ReanalizationNeeded = true;
            }
            return removedCount;
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Report Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
//*********************************************************************
//  Dependencies: System
using System;
using System.Linq;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_AdvancedGridBuilding : ABS_Building
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Delegates
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private delegate void VisitorDelegate(in ABS_AdvancedGridSnapPoint p_Snappoint, in Vector3 p_ParentLocalPosition);
        private delegate void RuledVisitorDelegate(in ABS_AdvancedGridSnapPointRule.SnapPoint p_RuleSnappoint, in Vector3 p_ParentLocalPosition);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private Vector3 m_BuildingPositionModifier = Vector3.zero;
        private Vector3 m_GridSize = Vector3.one;

        private ABS_AdvancedGridDeniedSnapPointCache m_DeniedPositionCache = null;

        [SerializeField] private bool m_EnableStability = false;
        [SerializeField][Range(3, 10)] private short m_StabilityLevel = 3;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Main Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public Vector3 BuildingPositionModifier
        {
            get { return m_BuildingPositionModifier; }
            set { m_BuildingPositionModifier = value; }
        }

        public bool EnableStability
        {
            get { return m_EnableStability; }
            set { m_EnableStability = value; }
        }
        
        public short StabilityLevel
        {
            get { return m_StabilityLevel; }
            set { m_StabilityLevel = value; }
        }
        
        public Vector3 GridSize
        {
            get { return m_GridSize; }
        }

        public ABS_AdvancedGridBuilding() : base (true, true, true)
        {
            PositionSearchAlgorithmType = ABS_PositionSearchAlgorithm.AdvancedGrid;
            m_DeniedPositionCache = new ABS_AdvancedGridDeniedSnapPointCache(m_Vector3Comparer);
        }

        protected override void ValidatePositionImpl(ABS_PositionValidationData p_ResultData,
                                                     in Vector3 p_LocalPosition,
                                                     in Quaternion p_LocalRotation,
                                                     in ABS_BuildingElement p_ElementForBuild)
        {
#if UNITY_EDITOR
            if (p_ResultData is not ABS_PositionValidationData_AdvancedGrid)
            {
                REST_Logging.Error($"{this}", "Wrong type of validation result data");
                return;
            }
#endif

            ABS_PositionValidationData_AdvancedGrid resultData = p_ResultData as ABS_PositionValidationData_AdvancedGrid;

            //Check Already Used Position
            if (!CheckUsedPosition(p_LocalPosition, p_ElementForBuild, p_ResultData))
            {
                return;
            }
            else if(!CheckRulesForOverrideStrategy(p_ElementForBuild, p_ResultData))
            {
                return;
            }

            //Check Denied Positions
            if (p_ElementForBuild.PositionAlgorithmSettings.SpecialRuleValidation &&
                m_DeniedPositionCache.IsPositionDenied(p_LocalPosition))
            {
                p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules_Denied = ABS_PositionValidationResult.ResultOptions.Failed;
                return;
            }

            //Validate the actual position
            ValidateAdvancedGirdLogic(p_LocalPosition, p_LocalRotation, p_ElementForBuild, resultData);
        }

        private bool CheckRulesForOverrideStrategy (in ABS_BuildingElement p_ElementForBuild, in ABS_PositionValidationData p_ResultData)
        {
            if (p_ResultData.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings == ABS_PositionValidationResult.ResultOptions.Failed)
            {
                return false;
            }
            else if (p_ResultData.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings == ABS_PositionValidationResult.ResultOptions.Unkown)
            {
                return true;
            }

            if (p_ResultData.m_ElementTarget_Override != null)
            {
                if(p_ElementForBuild.SnapPointRuleSet != p_ResultData.m_ElementTarget_Override.SnapPointRuleSet)
                {
                    p_ResultData.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings = ABS_PositionValidationResult.ResultOptions.Failed;
                    return false;
                }
            }
            else
            {
                REST_Logging.Error($"{this}", "Valdiated override scenario with null target");
            }

            return true;
        }

        private void ValidateAdvancedGirdLogic(in Vector3 p_LocalPosition,
                                               in Quaternion p_LocalRotation,
                                               in ABS_BuildingElement p_ElementForBuild,
                                               ABS_PositionValidationData_AdvancedGrid p_ResultData)
        {
            int blockedSnapCount = 0;
            int potentialValidatorNeighbour = 0;
            short maxStabilityLevel = p_ResultData.m_Stable ? m_StabilityLevel : (short)0;
            ABS_AdvancedGridBuilderSettings setting = (ABS_AdvancedGridBuilderSettings)p_ElementForBuild.PositionAlgorithmSettings;
            foreach (ABS_AdvancedGridType type in Enum.GetValues(typeof(ABS_AdvancedGridType)))
            {
                List<ABS_AdvancedGridSnapPointRule.SnapPoint> ruledSnapPoints = null;
                if (setting.SpecialRuleValidation && p_ElementForBuild.SnapPointRuleSet)
                {
                    ruledSnapPoints = p_ElementForBuild.SnapPointRuleSet.GetSnapPoints(type);
                }

                ABS_AdvancedGridSnapPoint[] snapPoints = ABS_AdvancedGridSnapPointCollection.GetSnapPointsForElements(p_ElementForBuild.AdvancedGridType, type);

#if UNITY_EDITOR
                if (ruledSnapPoints != null)
                {
                    if (ruledSnapPoints.Count != snapPoints.Length)
                    {
                        REST_Logging.Error($"{this}", "Not matching SnapPoint counts " +
                            $"BuildingElement : {p_ElementForBuild.name}  Type : {p_ElementForBuild.AdvancedGridType.ToString()}  " +
                            $"Target Type : {type.ToString()}");
                    }
                }
#endif
                    
                for (int i = 0; i < snapPoints.Length; ++i)
                {
#if UNITY_EDITOR
                    if (m_Vector3Comparer.Equals(snapPoints[i].m_Position, Vector3.zero))
                    {
                        REST_Logging.Error($"{this}", 
                            $"\nZero SnapPoint at position : {i} " +
                            $"\nBuildingElement : {p_ElementForBuild.name}  Type : {p_ElementForBuild.AdvancedGridType.ToString()}  " +
                            $"\nTarget Type : {type.ToString()} ");
                    }
#endif
                    Vector3 checkedLocalPosition = CalcualteCheckPosition(p_LocalPosition, p_LocalRotation, snapPoints[i].m_Position);
                    ABS_BuildingElement parentElement = null;
                    bool found = m_Elements.TryGetValue(checkedLocalPosition, out parentElement);
                    //Debug.Log($"ValidateAdvancedGirdLogic p_LocalPosition : {p_LocalPosition} | checkedLocalPosition : {checkedLocalPosition}");
                    if (found && parentElement != null && parentElement.AdvancedGridType == type && !parentElement.PreBuilt)
                    {
                        ++potentialValidatorNeighbour;

                        //Check if the element should snap to a foundation
                        //Also check this first becasue it is faster then the ruleset check
                        if (!CheckFoundationLogic(p_ElementForBuild, parentElement, p_ResultData))
                        {
                            continue;
                        }

                        //Check parentElement's snappoint rules
                        if (parentElement.SnapPointRuleSet)
                        {
                            bool ableToSnapToParent = CheckParentElementSnapPointRuelset(
                                parentElement,
                                p_ElementForBuild,
                                p_LocalPosition);

                            if (!ableToSnapToParent)
                            {
                                ++blockedSnapCount;
                                continue;
                            }
                        }

                        //Check p_ElementForBuild's snappoint rules
                        if (ruledSnapPoints != null)
                        {
                            ABS_AdvancedGridSnapPointRule.PermissionType perm = ruledSnapPoints[i].m_Permisson;
                            if (perm == ABS_AdvancedGridSnapPointRule.PermissionType.Deny)
                            {
                                p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules_Denied = ABS_PositionValidationResult.ResultOptions.Failed;
                                return;
                            }
                            else if (perm == ABS_AdvancedGridSnapPointRule.PermissionType.Block)
                            {
                                ++blockedSnapCount;
                            }
                        }

                        if (m_EnableStability && !p_ResultData.m_Stable)
                        {
                            CheckStabilityTransfer(p_ElementForBuild.AdvancedGridType, p_LocalPosition, parentElement, ref maxStabilityLevel);
                        }

                        //TODO
                        //In this case we know the the element is not breaking any position rule
                        //But it is a used position (Override, or prebuilt)
                        //The question is that the already built elements are still valid if the element will be replaced?
                    }
                }
            }

            //At this point we know that the position did not break any denied rule
            //Because in that case the function was already returned
            p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules_Denied = ABS_PositionValidationResult.ResultOptions.Validated;

            if (m_EnableStability)
            {
                if (p_ResultData.m_Stable || maxStabilityLevel > 0)
                {
                    p_ResultData.m_Stability = maxStabilityLevel;
                    p_ResultData.m_DragBuildingTemporaryStability = maxStabilityLevel;
                    p_ResultData.m_Result.SpecialElementValidation_Stability = ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    p_ResultData.m_Result.SpecialElementValidation_Stability = ABS_PositionValidationResult.ResultOptions.Failed;
                }
            }
            else
            {
                p_ResultData.m_Result.SpecialElementValidation_Stability = ABS_PositionValidationResult.ResultOptions.Validated;
            }

            if (potentialValidatorNeighbour == 0)
            {
                p_ResultData.m_Result.ParentBuildingValidation_InvalidPosition = ABS_PositionValidationResult.ResultOptions.Failed;
                CheckFoundationLogic(p_ElementForBuild, p_ElementForBuild, p_ResultData);
            }
            else
            {
                p_ResultData.m_Result.ParentBuildingValidation_InvalidPosition = ABS_PositionValidationResult.ResultOptions.Validated;

                if (potentialValidatorNeighbour == blockedSnapCount)
                {
                    p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules = ABS_PositionValidationResult.ResultOptions.Failed;
                }
                else
                {
                    p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules = ABS_PositionValidationResult.ResultOptions.Validated;
                }
            }
        }

        //How much stability can given with that stability connection type by the element
        private void CheckStabilityTransfer(
            ABS_AdvancedGridType p_CheckedAdvancedGridType, 
            Vector3 p_CheckedLocalPosition,
            in ABS_BuildingElement p_NeighbourElement,
            ref short p_MaxStabilityLevel)
        {
            if (p_NeighbourElement.StabilityLevel == 0)
            {
                return;
            }

            ABS_AdvancedGridSnapPoint sp = GetSnapPontForsStability(
                p_CheckedAdvancedGridType,
                p_CheckedLocalPosition,
                p_NeighbourElement);

            if(sp == null)
            {
                return;
            }

            short parentStability = 0;
            switch (sp.m_StabilityConnection)
            {
                case ABS_AdvancedGridStabilityConnectionType.None:
                    return;
                case ABS_AdvancedGridStabilityConnectionType.Stable :
                    parentStability = p_NeighbourElement.StabilityLevel;
                    break;
                case ABS_AdvancedGridStabilityConnectionType.Unstable:
                    parentStability = (short)(p_NeighbourElement.StabilityLevel - 1);
                    break;

            }

            if (parentStability > p_MaxStabilityLevel)
            {
                p_MaxStabilityLevel = parentStability;
            }
        }

        private bool CheckFoundationLogic (in ABS_BuildingElement p_ElementForBuild, 
                                           in ABS_BuildingElement p_ParentElement,
                                           ABS_PositionValidationData_AdvancedGrid p_ResultData)
        {
            if (p_ElementForBuild.ShouldSnapToFoundation)
            {
                if (p_ParentElement.Foundation)
                {
                    p_ResultData.m_Result.ParentBuildingValidation_SnappingToFoundation = ABS_PositionValidationResult.ResultOptions.Validated;
                    return true;
                }
                else if (ABS_PositionValidationResult.ResultOptions.Validated != p_ResultData.m_Result.ParentBuildingValidation_SnappingToFoundation)
                {
                    p_ResultData.m_Result.ParentBuildingValidation_SnappingToFoundation = ABS_PositionValidationResult.ResultOptions.Failed;
                    return false;
                }
            }
            return true;
        }

        private bool CheckParentElementSnapPointRuelset(in ABS_BuildingElement p_ParentElement,
                                                        in ABS_BuildingElement p_ElementForBuild,
                                                        in Vector3 p_LocalPosition)
        {
            Transform parentElementTransform = p_ParentElement.transform;
            List<ABS_AdvancedGridSnapPointRule.SnapPoint> parentRuledSnapPoints = p_ParentElement.SnapPointRuleSet.GetSnapPoints(p_ElementForBuild.AdvancedGridType);
            Quaternion parentElementRotation = p_ParentElement.transform.localRotation;

            foreach (ABS_AdvancedGridSnapPointRule.SnapPoint sp in parentRuledSnapPoints)
            {
                Vector3 parentElementCheckedPosition = CalcualteCheckPosition(parentElementTransform.localPosition,
                                                                              parentElementRotation,
                                                                              sp.m_AdvancedGridSnapPoint.m_Position);

                if (parentElementCheckedPosition != p_LocalPosition)
                {
                    continue;
                }

                if (sp.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow)
                {
                    return true;
                }
                else
                {
#if UNITY_EDITOR
                    if (sp.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Deny)
                    {
                        REST_Logging.Error($"{this}", 
                            "\nDenied element position from the ParentBuilding POV" +
                            $"\nBuildingElement : {p_ElementForBuild.name}" +
                            $"\nType : {p_ElementForBuild.AdvancedGridType.ToString()}" +
                            $"\nLocalPosition : {p_LocalPosition}" +
                            "\n\n" +
                            $"\nParentElement : {p_ParentElement.name}" +
                            $"\nParentElement Type : {p_ElementForBuild.AdvancedGridType.ToString()}" +
                            $"\nParentElement LocalPosition : {parentElementTransform.localPosition}" +
                            $"\nParentElement LocalRotation : {parentElementRotation.eulerAngles}" +
                            "\n\n" +
                            $"\nSnapPoint : {sp.m_AdvancedGridSnapPoint.m_Position}" +
                            $"\nCheckedPosition : {parentElementCheckedPosition}" +
                            $"\nSnapPoint local Position : {parentElementTransform.localPosition + parentElementCheckedPosition}");
                    }
#endif
                    return false;
                }
            }
            return false;
        }
        
        private Vector3 CalcualteCheckPosition (in Vector3 p_LocalPosition,
                                                in Quaternion p_LocalRotation,
                                                in Vector3 p_SnapPointPosition)
        {
            Vector3 rotatedPosition = p_LocalRotation * p_SnapPointPosition;
            Vector3 checkedPosition = new Vector3(
                p_LocalPosition.x + (rotatedPosition.x * m_GridSize.x),
                p_LocalPosition.y + (rotatedPosition.y * m_GridSize.y),
                p_LocalPosition.z + (rotatedPosition.z * m_GridSize.z)
            );

            return checkedPosition;
        }

        public void RefreshPositionModifier()
        {
            ABS_BuildingElement element = null;
            foreach (Transform child in transform)
            {
                element = child.GetComponent<ABS_BuildingElement>();
                if (element != null)
                {
                    break;
                }
            }

            if (element == null)
            {
                this.transform.position = this.transform.position - (Vector3.up * m_BuildingPositionModifier.y);
                m_BuildingPositionModifier = Vector3.zero;
                return;
            }

            if (element != null)
            {
                m_BuildingPositionModifier = ABS_AdvancedGirdBuilderGridHelper.GetParentPositionAlignment(element);
            }
        }

        protected override void ElementIsPlaced(ABS_BuildingElement p_PlacedElement)
        {
            m_GridSize = ((ABS_AdvancedGridBuilderSettings)p_PlacedElement.PositionAlgorithmSettings).GridSize;

            if (p_PlacedElement.SnapPointRuleSet != null && p_PlacedElement.PositionAlgorithmSettings.SpecialRuleValidation)
            {
                VisitNeighbours(p_PlacedElement, (in ABS_AdvancedGridSnapPointRule.SnapPoint p_RuleSnappoint, in Vector3 p_ParentLocalPosition) =>
                {
                    if (p_RuleSnappoint.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Deny)
                    {
                        m_DeniedPositionCache.SaveResultToCache(p_PlacedElement, p_ParentLocalPosition);
                    }

                    if (m_EnableStability && m_Elements.Count != 1)
                    {
                        RefreshStabilityForNeighboursReq(p_PlacedElement, p_RuleSnappoint, p_ParentLocalPosition, null);
                    }
                });
            }
            else if (m_EnableStability)
            {
                if (m_Elements.Count == 1)
                {
                    return;
                }

                VisitNeighbours(p_PlacedElement, (in ABS_AdvancedGridSnapPoint p_Snappoint, in Vector3 p_ParentLocalPosition) =>
                {
                    RefreshStabilityForNeighboursReq(p_PlacedElement, p_ParentLocalPosition, null);
                });
            }
        }

        public void RefreshStabilityForElement (ABS_BuildingElement p_Target)
        {
            if (p_Target == null)
            {
                REST_Logging.Error($"{this}", "Null element for refresh stability!");
                return;
            }
            
            if (m_Elements.Count == 1)
            {
                return;
            }

            if (!m_Elements.ContainsValue(p_Target))
            {
                REST_Logging.Error($"{this}", "The given element for stability refresh is not this Building's element!");
                return;
            }


            if (EnableStability)
            {
                if (p_Target.Stable)
                {
                    p_Target.StabilityLevel = m_StabilityLevel;
                }
                else
                {
                    VisitNeighbours(p_Target, (in ABS_AdvancedGridSnapPoint p_Snappoint, in Vector3 p_ParentLocalPosition) =>
                    {
                        ABS_BuildingElement parentElement = null;
                        bool found = m_Elements.TryGetValue(p_ParentLocalPosition, out parentElement);
                        short stabilityGivenByTheNeighbour = -1;
                        if (found
                            && parentElement != null
                            && !parentElement.PreBuilt
                            && p_Target.StabilityLevel <= parentElement.StabilityLevel -2
                            && CanStabilityDependentOnNeighbour(p_Target, parentElement, ref stabilityGivenByTheNeighbour))
                        {
                            p_Target.StabilityLevel = stabilityGivenByTheNeighbour;
                        }
                    });

                    RefreshStabilityForNeighboursReq(p_Target, null);
                }
            }
        }

        private void RefreshStabilityForNeighboursReq(ABS_BuildingElement p_Element, ABS_BuildingElement p_IgnoredElement)
        {
            if (p_Element.SnapPointRuleSet != null && p_Element.PositionAlgorithmSettings.SpecialRuleValidation)
            {
                VisitNeighbours(p_Element, (in ABS_AdvancedGridSnapPointRule.SnapPoint p_RuleSnappoint, in Vector3 p_ParentLocalPosition) =>
                {
                    RefreshStabilityForNeighboursReq(p_Element, p_RuleSnappoint, p_ParentLocalPosition, p_IgnoredElement);
                });
            }
            else
            {
                VisitNeighbours(p_Element, (in ABS_AdvancedGridSnapPoint p_Snappoint, in Vector3 p_ParentLocalPosition) =>
                {
                    RefreshStabilityForNeighboursReq(p_Element, p_ParentLocalPosition, p_IgnoredElement);
                });
            }
        }

        private void RefreshStabilityForNeighboursReq(
            ABS_BuildingElement p_Element,
            in ABS_AdvancedGridSnapPointRule.SnapPoint p_RuleSnappoint,
            in Vector3 p_ParentLocalPosition,
            in ABS_BuildingElement p_IgnoredElement)
        {
            if (p_RuleSnappoint.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow)
            {
                RefreshStabilityForNeighboursReq(p_Element, p_ParentLocalPosition, p_IgnoredElement);
            }
        }

        private void RefreshStabilityForNeighboursReq(
            in ABS_BuildingElement p_Element, 
            in Vector3 p_ParentLocalPosition,
            in ABS_BuildingElement p_IgnoredElement)
        {
            ABS_BuildingElement parentElement = null;
            bool found = m_Elements.TryGetValue(p_ParentLocalPosition, out parentElement);
            if (found
                && parentElement != null
                && !parentElement.PreBuilt
                && p_Element.StabilityLevel >= parentElement.StabilityLevel + 2
                && parentElement != p_IgnoredElement)
            {
                short stability = 0;
                CheckStabilityTransfer(parentElement.AdvancedGridType, parentElement.transform.localPosition, p_Element, ref stability);
                parentElement.StabilityLevel = stability;
                RefreshStabilityForNeighboursReq(parentElement, p_IgnoredElement);
            }
        }

        protected override void ElementWillBeRemoved(
            ABS_DestroyActionElementData p_BaseDestroyActionData,
            ABS_BuildingManagerTracker p_Tracker,
            bool p_TriggeredByHistory,
            bool p_IgnoreStability,
            ABS_BuildingElement p_ElementToRemove)
        {
            bool checkStability = !p_IgnoreStability && m_EnableStability && p_ElementToRemove.StabilityLevel > 1;
            bool checkPermission = p_ElementToRemove.SnapPointRuleSet != null && p_ElementToRemove.PositionAlgorithmSettings.SpecialRuleValidation;

            List<ABS_BuildingElement> dependentElements = null;
            Dictionary<ABS_BuildingElement, List<ABS_BuildingElement>> dependentElementsHighNeighbours = null;
            if (checkStability)
            {
                dependentElements = new List<ABS_BuildingElement>();
                dependentElements.Add(p_ElementToRemove);
                dependentElementsHighNeighbours = new Dictionary<ABS_BuildingElement, List<ABS_BuildingElement>>();
            }

            if (checkPermission)
            {
                List<ABS_BuildingElement> highNeighbours = null;
                if (checkStability)
                {
                    highNeighbours = GetHighStabilityNeighbourList(p_ElementToRemove, dependentElementsHighNeighbours);
                }
                VisitNeighbours(p_ElementToRemove, (in ABS_AdvancedGridSnapPointRule.SnapPoint p_RuleSnappoint, in Vector3 p_ParentLocalPosition) =>
                {
                    if (p_RuleSnappoint.m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Deny)
                    {
                        m_DeniedPositionCache.RemoveCacheData(p_ElementToRemove, p_ParentLocalPosition);
                    }

                    if (checkStability)
                    {
                        CheckStabilityForRemove(
                            p_CheckedElement: p_ElementToRemove,
                            p_Snappoint: p_RuleSnappoint.m_AdvancedGridSnapPoint,
                            p_SnapPosition: p_ParentLocalPosition,
                            p_DependentElements: dependentElements,
                            p_DependentElementsHighNeighboursCollection: dependentElementsHighNeighbours,
                            p_DependentElementsHighNeighbours : highNeighbours);
                    }
                });
            }
            else if (checkStability)
            {
                List<ABS_BuildingElement> highNeighbours = GetHighStabilityNeighbourList(p_ElementToRemove, dependentElementsHighNeighbours);
                VisitNeighbours(p_ElementToRemove, (in ABS_AdvancedGridSnapPoint p_Snappoint, in Vector3 p_ParentLocalPosition) =>
                {
                    CheckStabilityForRemove(
                        p_CheckedElement: p_ElementToRemove,
                        p_Snappoint: p_Snappoint,
                        p_SnapPosition: p_ParentLocalPosition,
                        p_DependentElements: dependentElements,
                        p_DependentElementsHighNeighboursCollection: dependentElementsHighNeighbours,
                        p_DependentElementsHighNeighbours: highNeighbours);
                });
            }

            if (checkStability)
            {
                RefreshStabilityDuringRemove(p_ElementToRemove, dependentElements, dependentElementsHighNeighbours);
                DestroyElementsBasedOnStability(p_BaseDestroyActionData, p_Tracker, p_TriggeredByHistory, p_ElementToRemove, dependentElements);
            }
        }

        //Collect the elements what should be removed becasue of the stbaility changes
        private void CheckStabilityForRemove(
            ABS_BuildingElement p_CheckedElement,
            ABS_AdvancedGridSnapPoint p_Snappoint,
            Vector3 p_SnapPosition,
            List<ABS_BuildingElement> p_DependentElements,
            Dictionary<ABS_BuildingElement, List<ABS_BuildingElement>> p_DependentElementsHighNeighboursCollection,
            List<ABS_BuildingElement> p_DependentElementsHighNeighbours)
        {
            ABS_BuildingElement neighbourElement = null;
            bool found = m_Elements.TryGetValue(p_SnapPosition, out neighbourElement);
            if (!found || neighbourElement == null || neighbourElement.PreBuilt)
            {
                return;
            }

            //Note: The element's neihgours can be smaller with only 1 or high with only 1 or equal
            //In only that case can be a neighour higher then 1 or smaller than 1
            //  if between the neighour or the checked element the conenction type is none from both POV
            //  which means that none of the element can give stability to the other one.
            //  Or if the elements has ruleset and the permission from bot POV is blocked

            short ignored = 0;
            if (CanStabilityDependentOnNeighbour(p_CheckedElement, neighbourElement, ref ignored))
            {
                p_DependentElementsHighNeighbours.Add(neighbourElement);
            }

            if (CanStabilityDependentOnNeighbour(neighbourElement, p_CheckedElement, ref ignored))
            {
                if (!p_DependentElements.Contains(neighbourElement))
                {
                    p_DependentElements.Add(neighbourElement);

                    List<ABS_BuildingElement> highNeighbours = GetHighStabilityNeighbourList(neighbourElement, p_DependentElementsHighNeighboursCollection);
                    VisitNeighbours(neighbourElement, (in ABS_AdvancedGridSnapPoint p_Snappoint, in Vector3 p_ParentLocalPosition) =>
                    {
                        CheckStabilityForRemove(
                            p_CheckedElement: neighbourElement,
                            p_Snappoint: p_Snappoint,
                            p_SnapPosition: p_ParentLocalPosition,
                            p_DependentElements: p_DependentElements,
                            p_DependentElementsHighNeighboursCollection: p_DependentElementsHighNeighboursCollection,
                            p_DependentElementsHighNeighbours: highNeighbours);
                    });
                }
            }
        }

        public List<ABS_BuildingElement> GetHighStabilityNeighbourList (
            ABS_BuildingElement p_Element,
            Dictionary<ABS_BuildingElement, List<ABS_BuildingElement>> p_DependentElementsHighNeighboursCollection)
        {
            List<ABS_BuildingElement> highNeighbours = null;
            if (!p_DependentElementsHighNeighboursCollection.TryGetValue(p_Element, out highNeighbours) || highNeighbours == null)
            {
                highNeighbours = new List<ABS_BuildingElement>();
                p_DependentElementsHighNeighboursCollection[p_Element] = highNeighbours;
            }

            return highNeighbours;
        }

        private bool CanStabilityDependentOnNeighbour(ABS_BuildingElement p_CheckedElement, ABS_BuildingElement p_NeighbourElement, ref short p_StabilityGivenByTheNeighbour)
        {
            if (p_CheckedElement.IsStable()
                || p_CheckedElement.StabilityLevel > p_NeighbourElement.StabilityLevel)
            {
                p_StabilityGivenByTheNeighbour = -1;
                return false;
            }

            Transform checkedTransform = p_CheckedElement.transform;
            Transform neighbourTransform = p_NeighbourElement.transform;

            ABS_AdvancedGridSnapPointRuleSet neighbourRuleSet = p_NeighbourElement.SnapPointRuleSet;
            if (neighbourRuleSet != null)
            {
                ABS_AdvancedGridSnapPointRule rule = neighbourRuleSet.Rules[(short)p_CheckedElement.AdvancedGridType];
                foreach (ABS_AdvancedGridSnapPointRule.SnapPoint sp in rule.SnapPoints)
                {
                    Vector3 calcualtedLocalPosition = CalcualteCheckPosition(
                        neighbourTransform.localPosition, 
                        neighbourTransform.localRotation, 
                        sp.m_AdvancedGridSnapPoint.m_Position);

                    if (m_Vector3Comparer.Equals(checkedTransform.localPosition, calcualtedLocalPosition))
                    {
                        if (sp.m_Permisson != ABS_AdvancedGridSnapPointRule.PermissionType.Allow)
                        {
                            p_StabilityGivenByTheNeighbour = -1;
                            return false;
                        }

                        if (sp.m_AdvancedGridSnapPoint.m_StabilityConnection == ABS_AdvancedGridStabilityConnectionType.None)
                        {
                            p_StabilityGivenByTheNeighbour = -1;
                            return false;
                        }

                        CheckStabilityTransfer(
                            p_CheckedElement.AdvancedGridType, 
                            checkedTransform.localPosition,
                            p_NeighbourElement,
                            ref p_StabilityGivenByTheNeighbour);
                        return true;
                    }
                }
            }
            else
            {
                ABS_AdvancedGridSnapPoint sp = GetSnapPontForsStability(
                    p_CheckedElement.AdvancedGridType,
                    p_CheckedElement.transform.localPosition, 
                    p_NeighbourElement);
                if (sp != null)
                {
                    if (sp.m_StabilityConnection == ABS_AdvancedGridStabilityConnectionType.None)
                    {
                        p_StabilityGivenByTheNeighbour = -1;
                        return false;
                    }
                    else
                    {
                        CheckStabilityTransfer(
                            p_CheckedElement.AdvancedGridType,
                            checkedTransform.localPosition,
                            p_NeighbourElement,
                            ref p_StabilityGivenByTheNeighbour);
                        return true;
                    }
                }
            }
            p_StabilityGivenByTheNeighbour = -1;
            return false;
        }

        //Give back the snapPoint fromt he Neighbour POV
        private ABS_AdvancedGridSnapPoint GetSnapPontForsStability(
            ABS_AdvancedGridType p_CheckedAdvancedGridType, 
            Vector3 p_CheckedLocalPosition,
            ABS_BuildingElement p_NeighbourElement)
        {
            Transform neighbourTransform = p_NeighbourElement.transform;

            ABS_AdvancedGridSnapPoint[] snapPoints =
                ABS_AdvancedGridSnapPointCollection.GetSnapPointsForElements(p_NeighbourElement.AdvancedGridType, p_CheckedAdvancedGridType);

            foreach (ABS_AdvancedGridSnapPoint sp in snapPoints)
            {
                Vector3 calcualtedLocalPosition = CalcualteCheckPosition(
                    neighbourTransform.localPosition,
                    neighbourTransform.localRotation,
                    sp.m_Position);

                if (m_Vector3Comparer.Equals(p_CheckedLocalPosition, calcualtedLocalPosition))
                {
                    return sp;
                }
            }
            return null;

        }

        private void RefreshStabilityDuringRemove (
            ABS_BuildingElement p_OriginallyDestroyedElement, 
            List<ABS_BuildingElement> p_DependentElements,
            Dictionary<ABS_BuildingElement, List<ABS_BuildingElement>> p_DependentElementsHighNeighbours)
        {
            foreach (ABS_BuildingElement element in p_DependentElements)
            {
                element.StabilityLevel = -1;
            }

            for (int i = 0; i < p_DependentElements.Count; ++i)
            {
                ABS_BuildingElement dependentElement = p_DependentElements[i];
                if (dependentElement == p_OriginallyDestroyedElement)
                {
                    continue;
                }

                List<ABS_BuildingElement> highNeighbours = null;
                if (!p_DependentElementsHighNeighbours.TryGetValue(dependentElement, out highNeighbours) || highNeighbours == null)
                {
                    //The element has no HighStabilityNeighbour
                    continue;
                }

                ABS_AdvancedGridType dependentElemenAdvancedGridType = dependentElement.AdvancedGridType;
                Vector3 dependentElementLocalPos = dependentElement.transform.localPosition;

                foreach (ABS_BuildingElement highStabilityNeighbour in highNeighbours)
                {
                    if (highStabilityNeighbour == p_OriginallyDestroyedElement
                        || p_DependentElements.Any(element => element == highStabilityNeighbour))
                    {
                        continue;
                    }

                    //Every element what contained by the dependentElements has -1 stability after the collection step
                    //During this stability ferreshing step they can get normal stability only if they already got a stabil connection to the building
                    //So if any element has higher than 0 stbaility can give stability to the currently checked element
                    //But the currently checked elemnt can has already refreshed stbaility so it should checked whichone is higher
                    if (highStabilityNeighbour.StabilityLevel <= 0 || highStabilityNeighbour.StabilityLevel < dependentElement.StabilityLevel)
                    {
                        continue;
                    }

                    //Check the Stability connection from the highNeighbour POV
                    short stabilityGivenByTheHighNeighbour = 0;
                    CheckStabilityTransfer(dependentElemenAdvancedGridType, dependentElementLocalPos, highStabilityNeighbour, ref stabilityGivenByTheHighNeighbour);
                    if (stabilityGivenByTheHighNeighbour > dependentElement.StabilityLevel)
                    {
                        dependentElement.StabilityLevel = stabilityGivenByTheHighNeighbour;
                    }
                }

                //The manimum stability what can give stability to others is 1
                if (dependentElement.StabilityLevel > 1)
                {
                    RefreshStabilityForNeighboursReq(dependentElement, p_OriginallyDestroyedElement);
                }
                //Unfortunetly at this point we can not not if an element should be destroyed or not
                //because even if is did not get stability by it's own check maybe the neighbour got and it will refresh this elements stability
            }
        }

        private void DestroyElementsBasedOnStability(
            ABS_DestroyActionElementData p_BaseDestroyActionData,
            ABS_BuildingManagerTracker p_Tracker,
            bool p_TriggeredByHistory,
            ABS_BuildingElement p_IgnoredElement, 
            List<ABS_BuildingElement> p_DependentElements)
        {
            List<ABS_BuildingElement> elementsForDestroy = new List<ABS_BuildingElement>();
            foreach (ABS_BuildingElement dependentElement in p_DependentElements)
            {
                if (dependentElement.StabilityLevel <= 0 && dependentElement != p_IgnoredElement)
                {
                    elementsForDestroy.Add(dependentElement);
                }
            }

            foreach (ABS_BuildingElement element in elementsForDestroy)
            {
                ABS_DestroyActionElementData destroyData = element.Destroy(p_Tracker, p_TriggeredByHistory, true, true);
                if (p_BaseDestroyActionData != null)
                {
                    p_BaseDestroyActionData.DestroyedConenctedElementData[destroyData] = ABS_BuildingElementConnectionType.Structure;
                }
            }
        }

        private void VisitNeighbours(ABS_BuildingElement p_CheckedElement, VisitorDelegate p_Callback)
        {
            Transform checkedElementTransform = p_CheckedElement.transform;
            foreach (ABS_AdvancedGridType type in Enum.GetValues(typeof(ABS_AdvancedGridType)))
            {
                ABS_AdvancedGridSnapPoint[] snapPoints =
                    ABS_AdvancedGridSnapPointCollection.GetSnapPointsForElements(p_CheckedElement.AdvancedGridType, type);

                for (int i = 0; i < snapPoints.Length; ++i)
                {
                    ABS_AdvancedGridSnapPoint snappoint = snapPoints[i];
                    Vector3 parentLocalPosition = CalcualteCheckPosition(checkedElementTransform.localPosition,
                                                                  checkedElementTransform.localRotation,
                                                                  snappoint.m_Position);

                    p_Callback(snappoint, parentLocalPosition);
                }
            }
        }

        private void VisitNeighbours(ABS_BuildingElement p_CheckedElement, RuledVisitorDelegate p_Callback)
        {
            Transform checkedElementTransform = p_CheckedElement.transform;
            foreach (ABS_AdvancedGridType type in Enum.GetValues(typeof(ABS_AdvancedGridType)))
            {
                List<ABS_AdvancedGridSnapPointRule.SnapPoint> parentRuledSnapPoints = 
                    p_CheckedElement.SnapPointRuleSet.GetSnapPoints(type);

                for (int i = 0; i < parentRuledSnapPoints.Count; ++i)
                {
                    ABS_AdvancedGridSnapPoint snappoint = parentRuledSnapPoints[i].m_AdvancedGridSnapPoint;
                    Vector3 parentLocalPosition = CalcualteCheckPosition(
                        checkedElementTransform.localPosition,
                        checkedElementTransform.localRotation,
                        snappoint.m_Position);

                    p_Callback(parentRuledSnapPoints[i], parentLocalPosition);
                }
            }
        }

        public List<(ABS_BuildingElement, ABS_BuildingElement)> FindAllAndReplaceElementsTypeBased(in ABS_BuildingElement p_ReplaceElement, in bool p_DestroyOld)
        {
            if (p_ReplaceElement == null
                || p_ReplaceElement.PositionSearchAlgorithm != PositionSearchAlgorithmType)
            {
                return null;
            }

            List<(ABS_BuildingElement, ABS_BuildingElement)> replacedElements = new List<(ABS_BuildingElement, ABS_BuildingElement)>();
            Dictionary<Vector3, ABS_BuildingElement> elements = GetElementsList();
            foreach ((Vector3 pos, ABS_BuildingElement oldElement) in elements)
            {
                if (oldElement.AdvancedGridType == p_ReplaceElement.AdvancedGridType)
                {
                    ABS_BuildingElement newElement = Instantiate(p_ReplaceElement, m_BuildingTransform);
                    replacedElements.Add((oldElement, newElement));
                }
            }

            foreach ((ABS_BuildingElement, ABS_BuildingElement) pair in replacedElements)
            {
                ABS_BuildingElement oldElement = pair.Item1;
                ABS_BuildingElement newElement = pair.Item2;

                Transform oldElementTransform = oldElement.transform;

                AddBuildingElement(
                        p_Tracker : null,
                        p_TriggeredByHistory : false,
                        p_NewElement : newElement,
                        p_LocalPosition : oldElementTransform.localPosition,
                        p_LocalEulerAngles: oldElementTransform.localEulerAngles,
                        p_Force : true,
                        p_DestroyOld : false);

                if (p_DestroyOld)
                {
                    oldElement.Destroy(null, false, true, true);
                }
            }

            //In case of destroy the first elements will be null!!
            ClearCache();
            return replacedElements;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Persistence
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        [System.Serializable]
        public class ABS_AdvancedGridBuildingPersistedData : ABS_Building.ABS_BuildingPersistedData
        {
            public Vector3 BuildingPositionModifier = Vector3.zero;
            public bool EnableStability = false;
            public short StabillityLevel = 4;

            public override string ToJSON(in bool p_PrettyPrint)
            {
                return ABS_PersistencyManager.ToJson(this, p_PrettyPrint);
            }
        }

        public override string ToJSON(in bool p_PrettyPrint)
        {
            return GetPersistedData().ToJSON(p_PrettyPrint);
        }

        public ABS_AdvancedGridBuildingPersistedData GetPersistedData()
        {
            ABS_AdvancedGridBuildingPersistedData data = new ABS_AdvancedGridBuildingPersistedData();
            GetBasePersistedData(data);
            data.BuildingPositionModifier = m_BuildingPositionModifier;
            data.EnableStability = m_EnableStability;
            data.StabillityLevel = m_StabilityLevel;
            return data;
        }

        protected override ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl(ABS_BuildingPersistedData p_Data)
        {
            ABS_AdvancedGridBuildingPersistedData data = p_Data as ABS_AdvancedGridBuildingPersistedData;
            if (data == null)
            {
                REST_Logging.Warrning("ABS_AdvancedGridBuilding", $"Can not convert the BuildingPersistedData to AdvancedGridBuildingPersistedData for {InstanceGuid}");
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            m_BuildingPositionModifier = data.BuildingPositionModifier;
            m_EnableStability = data.EnableStability;
            m_StabilityLevel = data.StabillityLevel;
            return ABS_PersistencyLoadErrorCode.Successful;
        }
    }
}
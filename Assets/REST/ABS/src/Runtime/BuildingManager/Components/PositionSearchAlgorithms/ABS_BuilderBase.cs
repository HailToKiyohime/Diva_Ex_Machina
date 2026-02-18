//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_BuilderBase : ABS_BuildingManagerComponentBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected ABS_BuilderBaseSettings m_Settings = null;
        protected ABS_BuildingElement m_ActiveBuildingElement = null;

        protected ABS_BuilderPositionCache m_Cache = null;
        protected ABS_PositionValidator m_PositionValidator = null;
        protected REST_Vector3EqualityComparer m_VectorCompareator = new REST_Vector3EqualityComparer();

        protected float m_MouseWheelRotation = 0f;

#if UNITY_EDITOR
        protected static string s_StatisticsNumberColorFormat = "<color=#FFFFFF>{0}</color>";

        protected ulong m_StatisticsPositioningCounter = 0;
        protected double m_StatisticsSearchProcessTimeCounter = 0d;
        protected double m_StatisticsSearchProcessTimeMaximum = 0d;

        protected ulong m_StatisticsCounterOverlapCheck = 0;
        protected ulong m_StatisticsCounterRaycast = 0;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Statistics
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public ulong StatisticsCounterRaycast
        {
            get
            {
                return m_PositionValidator.StatisticsCounterRaycast
                    + m_StatisticsCounterRaycast;
            }
        }
        public ulong StatisticsCounterOverlapCheck
        {
            get 
            { 
                return m_PositionValidator.StatisticsCounterOverlapCheck
                    + m_StatisticsCounterOverlapCheck; 
            }
        }

        public void StatisticsReset()
        {
            m_PositionValidator.StatisticsReset();
            m_StatisticsCounterRaycast = 0;
            m_StatisticsCounterOverlapCheck = 0;
            StatisticsResetImpl();
        }
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuilderBase(ABS_IBuildingManagerInternalInterface p_Manager, ABS_BuildingManagerTracker p_Tracker)
            : base(p_Manager, p_Tracker) 
        {
            m_Cache = new ABS_BuilderPositionCache(); 
            m_PositionValidator = new ABS_PositionValidator(p_Manager, p_Tracker);
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++c+++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void SearchPositionImpl(in bool m_IsMain, in ABS_PositionSearchResult p_ResultData);
        protected abstract bool CanSnapToElement(ABS_BuildingElement p_Element);
        public abstract Vector3 GetParentPositionAlignment(ABS_BuildingElement p_Element);
#if UNITY_EDITOR
        public abstract void OnDrawGizmosImpl(in ABS_ProjectSettings p_ProjectSettings, in ABS_PositionSearchResult p_PositionSearchResult);
        public abstract void TriggerStatisticsPrint();
        public abstract void StatisticsResetImpl();
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void MouseWheelChanged(in float p_Value)
        {
            m_MouseWheelRotation += p_Value;
        }

        public virtual ABS_BuilderBaseSettings Settings
        {
            set 
            { 
                m_Settings = value;
                m_PositionValidator.Settings = m_Settings;
            }
            get { return m_Settings; }
        }

        public void SearchPosition(in bool m_IsMain, in ABS_PositionSearchResult p_ResultData)
        {
#if UNITY_EDITOR
            DateTime startTime = DateTime.Now;
            ++m_StatisticsPositioningCounter;
#endif
            SearchPositionImpl(m_IsMain, p_ResultData);

#if UNITY_EDITOR
            DateTime endTime = DateTime.Now;
            TimeSpan difference = endTime - startTime;
            m_StatisticsSearchProcessTimeCounter += difference.TotalMilliseconds;
            if (m_StatisticsSearchProcessTimeMaximum < difference.TotalMilliseconds)
            {
                m_StatisticsSearchProcessTimeMaximum = difference.TotalMilliseconds;
            }
#endif
        }

        public virtual void ResetActiveBuildingElement(in ABS_BuildingElement p_ActiveBuildingElement)
        {
            m_ActiveBuildingElement = p_ActiveBuildingElement;
            m_PositionValidator.ActiveBuildingElement = p_ActiveBuildingElement;
            m_Cache.ClearCache();
            ResetMouseWheelRotation();
        }

        protected float CalcualteMixedRotation(in float p_BaseRotation)
        {
            return (m_MouseWheelRotation * p_BaseRotation * 10.0f) % 360; ;
        }

        public void ResetMouseWheelRotation()
        {
            m_MouseWheelRotation = 0.0f;
        }

        public void ClearCache()
        {
            m_Cache.ClearCache();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Validation Algorithms
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void BaseElementValidation(in ABS_PositionValidationData p_Result,
                                          in ABS_Building p_TargetBuilding,
                                          in Vector3 p_GlobalPosition, 
                                          in Quaternion p_GlobalRotation,
                                          in bool p_IsElementAlignedToGround,
                                          in bool p_SkipUndergroundCheck)
        {
            m_PositionValidator.Validate(p_Result, p_TargetBuilding, p_GlobalPosition, p_GlobalRotation, p_IsElementAlignedToGround, p_SkipUndergroundCheck);
        }

        protected bool CheckValidationResult(ABS_PositionValidationResult p_ValidationResult, in bool p_IgnoreShouldAttach = true)
        {
            return p_ValidationResult.IsSuccessFull(
                p_IgnoreCollisionCheck          : m_Settings.CollisionFailureHandling == ABS_ValidationFailureHandling.BlockBuilding,
                p_IgnoreElementCollisionCheck   : m_Settings.ElementCollisionFailureHandling == ABS_ValidationFailureHandling.BlockBuilding,
                p_IgnoreBuildableGroundCheck    : m_Settings.BuildableGroundValidationFailureHandling == ABS_ValidationFailureHandling.BlockBuilding,
                p_IgnorePositionRules           : m_Settings.SpecialRuleValidationFailureHandling == ABS_ValidationFailureHandling.BlockBuilding,
                p_IgnoreShouldSnapToFoundation  : m_Settings.ShouldSnapToFoundationFailureHandling == ABS_ValidationFailureHandling.BlockBuilding,
                p_IgnoreShouldAttached          : p_IgnoreShouldAttach,
                p_IgnoreGroundedCheck           : m_Settings.GroundedCheckFailureHandling == ABS_ValidationFailureHandling.BlockBuilding,
                p_IgnoreStabiltyCheck           : m_Settings.StabiltyFailureHandling == ABS_ValidationFailureHandling.BlockBuilding);
        }

        protected bool IsBlockingNeededBasedOnValidationLogic(ABS_PositionValidationResult p_Result)
        {
            if (m_Settings.CollisionFailureHandling == ABS_ValidationFailureHandling.BlockBuilding
                    && p_Result.BaseElementValidation_Collision == ABS_PositionValidationResult.ResultOptions.Failed)
            {
                return true;
            }
            else if (m_Settings.ElementCollisionFailureHandling == ABS_ValidationFailureHandling.BlockBuilding
                    && p_Result.BaseElementValidation_ElementCollision == ABS_PositionValidationResult.ResultOptions.Failed)
            {
                return true;
            }
            else if (m_Settings.BuildableGroundValidationFailureHandling == ABS_ValidationFailureHandling.BlockBuilding
                    && p_Result.BaseElementValidation_BuildableGround == ABS_PositionValidationResult.ResultOptions.Failed)
            {
                return true;
            }
            else if (m_Settings.SpecialRuleValidationFailureHandling == ABS_ValidationFailureHandling.BlockBuilding
                    && (p_Result.ParentBuildingValidation_BreakPositionRules == ABS_PositionValidationResult.ResultOptions.Failed 
                        || p_Result.ParentBuildingValidation_BreakPositionRules_Denied == ABS_PositionValidationResult.ResultOptions.Failed))
            {
                return true;
            }
            else if (m_Settings.GroundedCheckFailureHandling == ABS_ValidationFailureHandling.BlockBuilding
                    && p_Result.BaseElementValidation_GroundedCheck == ABS_PositionValidationResult.ResultOptions.Failed
                    && p_Result.BaseElementValidation_AirHeightLimit != ABS_PositionValidationResult.ResultOptions.Validated)
            {
                return true;
            }
            else if (m_Settings.StabiltyFailureHandling == ABS_ValidationFailureHandling.BlockBuilding
                    && p_Result.SpecialElementValidation_Stability == ABS_PositionValidationResult.ResultOptions.Failed)
            {
                return true;
            }

            return false;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Search Algorithms
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected bool SearchForNearBuildingElements(
            in ABS_PositionSearchAlgorithm p_Algorithm, 
            in Vector3 p_SearchPosition, 
            in float p_Radius, 
            ref List<ABS_BuildingElement> p_BuildingElementList, 
            ref HashSet<ABS_Building> p_Buildings, 
            ref ABS_BuildingElement p_PreBuildTarget)
        {
            Collider[] colliders = REST_CollisionChecker.OverlapSphere(p_SearchPosition, p_Radius, m_Settings.LayerCollection.LayerOfBuildingElement);
#if UNITY_EDITOR
            ++m_StatisticsCounterOverlapCheck;
#endif
            bool foundPreBuilt = false;
            float distance = Settings.BuildRadius;
            foreach (Collider coll in colliders)
            {
                ABS_BuildingElement be = ABS_BuildingElementLink.FindElement(coll.transform);
                if (be == null)
                {
                    continue;
                }

                if (be == null
                    || be.PositionSearchAlgorithm != p_Algorithm
                    || be.ParentBuilding == null
                    || !CanSnapToElement(be))
                {
                    continue;
                }

#if UNITY_EDITOR
                if (be.PositionSearchAlgorithm != be.ParentBuilding.PositionSearchAlgorithmType)
                {
                    REST_Logging.Error("ABS_BuilderBase", 
                        "Invalide state : the ABS_BuildingElement and the parent has a differnet algorithm type! " +
                        $"Element: {be.name}" +
                        $"ElementType: {be.PositionSearchAlgorithm}" +
                        $"ParentType: {be.ParentBuilding.PositionSearchAlgorithmType}");
                    continue;
                }
#endif

                if (m_Settings.PrioritizePreBuilt && be.PreBuilt)
                {
                    if (be.PrefabGuid != m_ActiveBuildingElement.PrefabGuid)
                    {
                        //Check Final Element too
                        if (!(m_ActiveBuildingElement.FinalElement != null
                            && m_ActiveBuildingElement.SnapToPreBuiltFinalElement
                            && m_ActiveBuildingElement.FinalElement.PrefabGuid == be.PrefabGuid))
                        {
                            continue;
                        }
                    }

                    
                    Vector3 pos = be.gameObject.transform.position;
                    float currentDistance = Vector3.Distance(pos, p_SearchPosition);
                    if (distance >= currentDistance)
                    {
                        foundPreBuilt = true;
                        p_PreBuildTarget = be;
                        distance = currentDistance;
                    }
                }
                else
                {
                    p_Buildings.Add(be.ParentBuilding);
                    p_BuildingElementList.Add(be);
                }
            }

            return foundPreBuilt;
        }

        protected void GetPreBuiltPositionResultData(ABS_BuildingElement p_Element, ABS_PositionSearchResult p_Result)
        {
            p_Result.TargetBuilding = p_Element.ParentBuilding;
            p_Result.TargetPreBuiltElement = p_Element;
            p_Result.WorldPosition = p_Result.TargetBuilding.transform.TransformPoint(p_Element.transform.localPosition);
            p_Result.Rotation = p_Element.transform.rotation;
            p_Result.IsPreBuiltSnapping = true;

            p_Result.ValidationResult.m_Result.ParentBuildingValidation_UsedPosition = ABS_PositionValidationResult.ResultOptions.Failed;
            p_Result.ValidationResult.m_Result.ParentBuildingValidation_ValidatedByPreBuilt = ABS_PositionValidationResult.ResultOptions.Validated;
            p_Result.Result = ABS_PositionSearchResult.ResultType.Success;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public void OnDrawGizmos(in ABS_ProjectSettings p_ProjectSettings, in ABS_PositionSearchResult p_PositionSearchResult)
        {
            m_PositionValidator.OnDrawGizmos(p_PositionSearchResult, p_ProjectSettings);

            if (p_ProjectSettings.PositionSearch_SearchCollider)
            {
                REST_GizmosUtils.DrawWireSphere(m_Manager.GetRaycastHitOrEndPosition(), m_Settings.SearchRadius, p_ProjectSettings.PositionSearch_SearchColliderColor);
            }

            if (p_ProjectSettings.PositionSearch_BuildCollider)
            {
                REST_GizmosUtils.DrawWireSphere(m_Manager.GetRaycastHitOrEndPosition(), m_Settings.BuildRadius, p_ProjectSettings.PositionSearch_BuildColliderColor);
            }
            
            if (m_Settings.AllowBuildingInTheAir 
                && m_Settings.AddMaximumHeightToAirBuilding
                && p_ProjectSettings.PositionValidation_AirBuildingMaximumRange)
            {
                Vector3 pos = p_PositionSearchResult.WorldPosition;
                switch (m_Settings.AirPositionReferencePoint)
                {
                    case ABS_AirPositionReferencePoint.Center: break;
                    case ABS_AirPositionReferencePoint.Top:
                        pos += Vector3.up * m_ActiveBuildingElement.VerticalShifting.y;
                        break;
                    case ABS_AirPositionReferencePoint.Bottom:
                        pos -= Vector3.up * m_ActiveBuildingElement.VerticalShifting.y;
                        break;
                }

                REST_GizmosUtils.DrawArrow(pos,
                    new Vector3(pos.x, pos.y - m_Settings.MaximumAirHeight, pos.z),
                    (p_PositionSearchResult.ValidationResult.m_Result.BaseElementValidation_AirHeightLimit == ABS_PositionValidationResult.ResultOptions.Validated
                    ? UnityEngine.Color.green
                    : UnityEngine.Color.red));
            }
            
            OnDrawGizmosImpl(p_ProjectSettings, p_PositionSearchResult);
        }
#endif
    }
}
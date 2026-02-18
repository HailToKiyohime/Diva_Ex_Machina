//*********************************************************************
//  Dependencies: System
using REST.Utils;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_FreeBuilder : ABS_BuilderBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properites
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_FreeBuilderSettings m_FreeBuilderSettings;
        private ABS_FreeBuilding m_ParentBuilding = null;

        private bool m_BEAligned = false;
        private float m_AlignedRotation;
        private ABS_BuildingElement m_AlignmentTarget = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_FreeBuilder(ABS_IBuildingManagerInternalInterface p_Manager , ABS_BuildingManagerTracker p_Tracker)
            : base(p_Manager, p_Tracker)
        {
            m_ParentBuilding = p_Manager.GlobalFreeParent;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override ABS_BuilderBaseSettings Settings
        {
            set
            {
                m_FreeBuilderSettings = (value as ABS_FreeBuilderSettings);
                base.Settings = value;
            }
        }

#if UNITY_EDITOR
        public override void StatisticsResetImpl()
        {
            //nothing
        }

        public override void TriggerStatisticsPrint()
        {
            //nothing
        }

        public override void OnDrawGizmosImpl(in ABS_ProjectSettings p_ProjectSettings, in ABS_PositionSearchResult p_PositionSearchResult)
        {
            //nothing
        }
#endif

        public override Vector3 GetParentPositionAlignment(ABS_BuildingElement p_Element)
        {
            return Vector3.zero;
        }

        protected override void SearchPositionImpl(in bool m_IsMain, in ABS_PositionSearchResult p_ResultData)
        {
            Vector3 raycastPosition = m_Manager.GetRaycastHitOrEndPosition();

            if (!m_IsMain)
            {
                HashSet<ABS_Building> buildings = new HashSet<ABS_Building>();
                List<ABS_BuildingElement> buildingElements = new List<ABS_BuildingElement>();
                ABS_BuildingElement preBuiltTarget = null;
                bool isPreBuiltFound = SearchForNearBuildingElements(
                    ABS_PositionSearchAlgorithm.Free,
                    raycastPosition,
                    m_Settings.SearchRadius,
                    ref buildingElements,
                    ref buildings,
                    ref preBuiltTarget);

                if (isPreBuiltFound)
                {
                    GetPreBuiltPositionResultData(preBuiltTarget, p_ResultData);
                    return;
                }
            }

            FindPosition(p_ResultData);
            HandleRotation(m_IsMain, p_ResultData);
            if (p_ResultData.Result != ABS_PositionSearchResult.ResultType.SuccessBlockNeeded)
            {
                ValidateResults(m_IsMain, p_ResultData);
            }
        }

        public override void ResetActiveBuildingElement(in ABS_BuildingElement p_ActiveBuildingElement)
        {
            base.ResetActiveBuildingElement(p_ActiveBuildingElement);
            m_BEAligned = false;
            m_AlignmentTarget = null;
        }

        private void FindPosition(in ABS_PositionSearchResult p_Result)
        {
            if (m_Manager.Raycaster.HitTransform != null)
            {
                if (m_Settings.AlignPositionToGround)
                {
                    p_Result.IsAlignedToGround = true;
                    p_Result.WorldPosition = m_Manager.Raycaster.HitPoint + m_ActiveBuildingElement.VerticalShifting;
                }
                else
                {
                    p_Result.WorldPosition = m_Manager.Raycaster.HitPoint;
                }
            }
            else
            {
                Vector3 raycastEndPosition = m_Manager.GetRaycastEndPosition();
                Vector3 groundPosition = Vector3.zero;
                if (m_Settings.AlignPositionToGround && AlignToGround(raycastEndPosition, out groundPosition))
                {
                    p_Result.IsAlignedToGround = true;
                    p_Result.WorldPosition = groundPosition + m_ActiveBuildingElement.VerticalShifting;
                }
                else
                {
                    p_Result.WorldPosition = raycastEndPosition;
                }
            }
        }

        private bool AlignToGround(in Vector3 p_CheckedPosition, out Vector3 p_GroundPosition)
        {
            int combinedLayerMask = m_Settings.LayerCollection.LayerOfGround.value | m_Settings.LayerCollection.LayerOfBuildingElement.value;

#if UNITY_EDITOR
            ++m_StatisticsCounterRaycast;
#endif

            RaycastHit hit;
            bool res = ABS_Raycaster.GetGroundPosition(
                        p_CheckedPosition,
                        out p_GroundPosition,
                        combinedLayerMask,
                        out hit,
                        m_ActiveBuildingElement.VerticalShifting.y);
            return res;
        }


        private void HandleRotation (in bool m_IsMain, in ABS_PositionSearchResult p_Result)
        {
            if (HandleAlignmentToBudildingElements(p_Result))
            {
                return;
            }

            p_Result.Rotation = GetPlayerRotation();

            if (m_IsMain)
            {
                HandleAlignmentToGround(p_Result);
            }
        }

        private bool HandleAlignmentToBudildingElements (in ABS_PositionSearchResult p_Result)
        {
            if (!m_Settings.EnableAlignRotationToBuildingElements
                || m_Settings.RotationStrategy == ABS_RotationStrategy.NoRotation
                || m_Settings.RotationStrategy == ABS_RotationStrategy.CameraRotation
                || m_Settings.RotationStrategy == ABS_RotationStrategy.FixDegree)
            {
                return false;
            }

            ABS_ElementRotaionAlignmentStrategy strategy = m_ActiveBuildingElement.PositionAlgorithmSettings.ElementRotaionAlignmentStrategy;

            if (m_Manager.Raycaster.HitTransform != null
                && m_Manager.IsAlignRotationToBuidlingElementsTriggered())
            {
                ABS_BuildingElement buildingElement = m_Manager.Raycaster.BuildingElement;
                if (buildingElement != null)
                {
                    if (strategy == ABS_ElementRotaionAlignmentStrategy.CopyRotation)
                    {
                        ResetMouseWheelRotation();
                        m_AlignedRotation = m_Manager.Raycaster.HitTransform.rotation.eulerAngles.y;
                    }
                    else if (strategy == ABS_ElementRotaionAlignmentStrategy.AlignBasedOnStartRotation
                            || strategy == ABS_ElementRotaionAlignmentStrategy.AlignBasedOnCameraRotation)
                    {
                        bool useAligedPositionAsBase = strategy == ABS_ElementRotaionAlignmentStrategy.AlignBasedOnStartRotation;
                        if (!useAligedPositionAsBase)
                        {
                            ResetMouseWheelRotation();
                        }

                        float playerRotation = useAligedPositionAsBase ? m_AlignedRotation : GetPlayerRotation().eulerAngles.y;
                        float raycastHitRotation = m_Manager.Raycaster.HitTransform.rotation.eulerAngles.y;
                        playerRotation -= raycastHitRotation;
                        playerRotation = Mathf.Round(playerRotation / m_Settings.RotationYDegree) * m_Settings.RotationYDegree;
                        m_AlignedRotation = raycastHitRotation + playerRotation;
                    }
                    else if (strategy == ABS_ElementRotaionAlignmentStrategy.LockOnTargetFixed
                        || strategy == ABS_ElementRotaionAlignmentStrategy.LockOnTargetContinuous)
                    {
                        ResetMouseWheelRotation();
                        m_AlignmentTarget = buildingElement;
                    }

                    m_BEAligned = true;
                }
            }
            
            if (m_BEAligned)
            {
                if (strategy == ABS_ElementRotaionAlignmentStrategy.LockOnTargetFixed
                        || strategy == ABS_ElementRotaionAlignmentStrategy.LockOnTargetContinuous)
                {
                    if (m_AlignmentTarget == null)
                    {
                        m_BEAligned = false;
                        return false;
                    }
                    Vector3 alignmentTargetPosition = m_AlignmentTarget.transform.position;
                    alignmentTargetPosition.y = 0;
                    Vector3 raycastHitPosition = m_Manager.Raycaster.Hit.point;
                    raycastHitPosition.y = 0;
                    Vector3 directionToTarget = alignmentTargetPosition - raycastHitPosition;
                    if (!REST_Vector3EqualityComparer.Static_Equals(directionToTarget, Vector3.zero)
                        && directionToTarget.sqrMagnitude > 0.0001f)
                    { 
                        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

                        if (strategy == ABS_ElementRotaionAlignmentStrategy.LockOnTargetFixed)
                        {
                            Vector3 targetRotationEulerAngles = targetRotation.eulerAngles;
                            targetRotationEulerAngles.y = Mathf.Round(targetRotationEulerAngles.y / m_Settings.RotationYDegree) * m_Settings.RotationYDegree;
                            targetRotation = Quaternion.Euler(targetRotationEulerAngles);
                        }
                        p_Result.Rotation = targetRotation;
                    }
                }
                else
                {
                    p_Result.Rotation = Quaternion.Euler(0.0f, m_AlignedRotation + CalcualteMixedRotation(m_Settings.RotationYDegree), 0.0f);
                }
                return true;
            }

            return false;
        }

        private void HandleAlignmentToGround(in ABS_PositionSearchResult p_Result)
        {
            ABS_AlignRotationStrategy alignRotationToGroundStrategy = m_FreeBuilderSettings.AlignRotationToGroundStrategy;
            if (m_Manager.Raycaster.HitTransform != null
                 && (alignRotationToGroundStrategy == ABS_AlignRotationStrategy.Always
                    || (alignRotationToGroundStrategy == ABS_AlignRotationStrategy.ButtonHold && m_Manager.IsRotationAlignmentToGroundHold())
                    || (alignRotationToGroundStrategy == ABS_AlignRotationStrategy.TurnOnOff && m_Manager.IsRotationAlignmentToGroundOn())))
            {
                p_Result.Rotation = Quaternion.FromToRotation(p_Result.Rotation.eulerAngles, m_Manager.Raycaster.Hit.normal);

                float maximumAlignment = m_FreeBuilderSettings.MaximumRotationAlignment;
                Vector3 rotation = p_Result.Rotation.eulerAngles;
                if (!(rotation.x < maximumAlignment 
                    && rotation.x > -maximumAlignment 
                    && rotation.z < maximumAlignment 
                    && rotation.z > -maximumAlignment))
                {
                    p_Result.ValidationResult.m_Result.SpecialElementValidation_RotationMaximumAngle = 
                        ABS_PositionValidationResult.ResultOptions.Failed;  
                    p_Result.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
                }
            }
        }

        protected Quaternion GetPlayerRotation()
        {
            float Ydegree = 0.0f;
            switch (m_Settings.RotationStrategy)
            {
                case ABS_RotationStrategy.NoRotation:
                    {
                        Ydegree = 0.0f;
                    }
                    break;
                case ABS_RotationStrategy.PlayerRotation:
                    {
                        Ydegree = CalcualteMixedRotation(m_Settings.RotationYDegree);
                    }
                    break;
                case ABS_RotationStrategy.CameraRotation:
                    {
                        Ydegree = m_Manager.Camera.transform.rotation.eulerAngles.y;
                    }
                    break;
                case ABS_RotationStrategy.CamerAndPlayerRotation:
                    {
                        Ydegree = m_Manager.Camera.transform.rotation.eulerAngles.y + CalcualteMixedRotation(m_Settings.RotationYDegree);
                    }
                    break;
                case ABS_RotationStrategy.FixDegree:
                    {
                        Ydegree = m_Settings.RotationYDegree;
                    }
                    break;
            }
            return Quaternion.Euler(0.0f, Ydegree, 0.0f);
        }

        private void ValidateResults(in bool m_IsMain, ABS_PositionSearchResult p_Result)
        {
            p_Result.ValidationResult = new ABS_PositionValidationData();

            BaseElementValidation(p_Result.ValidationResult,
                                  null,
                                  p_Result.WorldPosition, 
                                  p_Result.Rotation,
                                  p_Result.IsAlignedToGround,
                                  false);

            bool ignoreShouldAttachCheck = true;
            if (m_IsMain
                && m_FreeBuilderSettings.EnableAttachementConnection
                && m_ActiveBuildingElement.ShouldAttached)
            {
                ignoreShouldAttachCheck = false;
                if (p_Result.ValidationResult.m_Result.BaseElementValidation_BuildOnTopOfElement == ABS_PositionValidationResult.ResultOptions.Validated
                        && p_Result.ValidationResult.m_ElementTarget_BuildOnTopOfElement != null
                        && !p_Result.ValidationResult.m_ElementTarget_BuildOnTopOfElement.CanNotBeAttachTarget)
                {
                    p_Result.ValidationResult.m_Result.SpecialElementValidation_ShouldAttached = ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    p_Result.ValidationResult.m_Result.SpecialElementValidation_ShouldAttached = ABS_PositionValidationResult.ResultOptions.Failed;
                }
            }

            if (CheckValidationResult(p_Result.ValidationResult.m_Result, ignoreShouldAttachCheck)
                && !IsBlockingNeededBasedOnValidationLogic(p_Result.ValidationResult.m_Result))
            {
                p_Result.Result = ABS_PositionSearchResult.ResultType.Success;
            }
            else
            {
                p_Result.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
            }
        }

        protected override bool CanSnapToElement(ABS_BuildingElement p_Element)
        {
            return true;
        }
    }
}
//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;
using UnityEngine.UIElements;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    public class ABS_PositionValidator
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected ABS_IBuildingManagerInternalInterface m_Manager = null;
        protected ABS_BuildingManagerTracker m_Tracker = null;

        protected ABS_BuilderBaseSettings m_Settings = null;
        protected ABS_BuildingElement m_ActiveBuildingElement = null;

        private RaycastHit m_Hit = new RaycastHit();

#if UNITY_EDITOR
        protected ulong m_StatisticsCounterRaycast = 0;
        protected ulong m_StatisticsCounterOverlapCheck = 0;

        private bool m_BuildableGround_RaycastHappened = false;
        private bool m_BuildableGround_RaycastHit1 = false;
        private bool m_BuildableGround_RaycastHit2 = false;
        private bool m_BuildableGround_RaycastHit3 = false;
        private bool m_BuildableGround_RaycastHit4 = false;
        private Vector3 m_BuildableGround_RaycastSource1 = Vector3.zero;
        private Vector3 m_BuildableGround_RaycastSource2 = Vector3.zero;
        private Vector3 m_BuildableGround_RaycastSource3 = Vector3.zero;
        private Vector3 m_BuildableGround_RaycastSource4 = Vector3.zero;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_PositionValidator (ABS_IBuildingManagerInternalInterface p_Manager, ABS_BuildingManagerTracker p_Tracker)
        {
            m_Manager = p_Manager;
            m_Tracker = p_Tracker;
        }

#if UNITY_EDITOR
        public ulong StatisticsCounterRaycast
        {
            get { return m_StatisticsCounterRaycast; }
        }
        public ulong StatisticsCounterOverlapCheck
        {
            get { return m_StatisticsCounterOverlapCheck; }
        }

        public void StatisticsReset()
        {
            m_StatisticsCounterRaycast = 0;
            m_StatisticsCounterOverlapCheck = 0;
        }
#endif

        public virtual ABS_BuildingElement ActiveBuildingElement
        {
            set { m_ActiveBuildingElement = value; }
        }


        public virtual ABS_BuilderBaseSettings Settings
        {
            set { m_Settings = value; }
        }

        public void Validate(in ABS_PositionValidationData p_ValidationData,
                             in ABS_Building p_TargetBuilding,
                             in Vector3 p_GlobalPosition,
                             in Quaternion p_GlobalRotation,
                             in bool p_IsElementAlignedToGround,
                             in bool p_SkipUndergroundCheck)
        {
#if UNITY_EDITOR
            m_BuildableGround_RaycastHappened = false;
#endif

            //If the element is aigned to the ground then we know that
            //It is not under the ground and it is not in the air also the aligned elements count as grounded
            if (p_IsElementAlignedToGround)
            {
                p_ValidationData.m_PositionWasAlignedToGround = true;
            }

            if (!p_SkipUndergroundCheck
                && m_Settings.IsUnderGroundValidationSupported()
                && m_Settings.CheckUnderGroundPosition)
            {
                if (p_IsElementAlignedToGround)
                {
                    p_ValidationData.m_Result.BaseElementValidation_UnderGround = ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    ValidateUnderGroundPosition(p_ValidationData, p_GlobalPosition);
                }
            }

            if (m_Settings.IsAllowBuildingInTheAirSupported()
                && m_Settings.AllowBuildingInTheAir
                && m_Settings.AddMaximumHeightToAirBuilding)
            {
                if (p_IsElementAlignedToGround)
                {
                    p_ValidationData.m_Result.BaseElementValidation_AirHeightLimit = ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    ValidateAirHeight(p_ValidationData, p_GlobalPosition);
                }
            }

            if (m_Settings.IsGroundedCheckSupported() && m_Settings.GroundedCheck)
            {
                if (p_IsElementAlignedToGround)
                {
                    p_ValidationData.m_Result.BaseElementValidation_GroundedCheck = ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    GroundedCheck(p_ValidationData, p_GlobalPosition, p_GlobalRotation);
                }
            }

            if (m_Settings.PositionValidationElementCollisionCheck)
            {
                ValidateBuildingElementOverlap(p_ValidationData, p_GlobalPosition, p_GlobalRotation, p_TargetBuilding);
            }

            if (m_Settings.IsValidationCollisionCheckSupported() && m_Settings.PositionValidationCollisionCheck)
            {
                ValidateBlockingCollision(p_ValidationData, p_GlobalPosition, p_GlobalRotation);
            }

            if (m_Settings.IsBuildableGroundValidationSupported() && m_Settings.BuildableGroundValidation)
            {
                ValidateBuildableGround(p_ValidationData, p_TargetBuilding, p_GlobalPosition, p_GlobalRotation);
            }

            if (m_Settings.IsBuildOnTopOfElementSupported() && m_Settings.BuildOnTopOfElement)
            {
                ValidateBuildOnTopOfElement(p_ValidationData, p_GlobalPosition);
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void ValidateBuildingElementOverlap(
            in ABS_PositionValidationData p_ValidationResultData, 
            in Vector3 p_Position,
            in Quaternion p_Rotation, 
            in ABS_Building p_TargetBuilding)
        {
            Collider[] colliders = OverlapBox(p_Position, p_Rotation, m_Settings.LayerCollection.LayerOfBuildingElement, m_Settings.ColliderSizeModifier);

            if (colliders.Length > 0)
            {
                foreach (Collider coll in colliders)
                {
                    ABS_BuildingElement be = ABS_BuildingElementLink.FindElement(coll.gameObject.transform);
                    if (be != null)
                    {
                        if (be.ParentBuilding == null)
                        {
                            REST_Logging.Warrning("ABS_PositionValidator", $"BuildingElement without parent : {be.name}");
                            p_ValidationResultData.m_Result.BaseElementValidation_Collision =
                                ABS_PositionValidationResult.ResultOptions.Failed;
                            return;
                        }
                        else if (p_TargetBuilding == null || be.ParentBuilding != p_TargetBuilding)
                        {
                            p_ValidationResultData.m_Result.BaseElementValidation_Collision =
                                ABS_PositionValidationResult.ResultOptions.Failed;
                            return;
                        }
                    }
                }
            }

            p_ValidationResultData.m_Result.BaseElementValidation_Collision =
                ABS_PositionValidationResult.ResultOptions.Validated;
        }

        private void ValidateBlockingCollision(
            in ABS_PositionValidationData p_ValidationResultData,
            in Vector3 p_Position,
            in Quaternion p_Rotation)
        {
            Collider[] colliders = OverlapBox(p_Position, p_Rotation, m_Settings.LayerCollection.BlockingLayers, m_Settings.ColliderSizeModifier);
            if (colliders.Length > 0)
            {
                p_ValidationResultData.m_Result.BaseElementValidation_ElementCollision =
                    ABS_PositionValidationResult.ResultOptions.Failed;
            }
            else
            {
                p_ValidationResultData.m_Result.BaseElementValidation_ElementCollision =
                    ABS_PositionValidationResult.ResultOptions.Validated;
            }
        }

        private Collider[] OverlapBox(in Vector3 p_Position, in Quaternion p_Rotation, in LayerMask p_Layer, in float p_ColliderResizing = 0.999f)
        {
#if UNITY_EDITOR
            ++m_StatisticsCounterOverlapCheck;
#endif
            Vector3 size = m_ActiveBuildingElement.Dimension / 2 * p_ColliderResizing;
            return REST_CollisionChecker.OverlapBox(p_Position, p_Rotation, p_Layer, size);
        }

        private void ValidateUnderGroundPosition(in ABS_PositionValidationData p_ValidationResultData, in Vector3 p_Position)
        {
            Vector3 raycastStartPosition = m_Manager.GetRaycastHitOrEndPosition();
            bool result = ABS_Raycaster.IsUnderground(p_Position, ref raycastStartPosition, ref m_Hit, m_Settings.LayerCollection.LayerOfGround);

#if UNITY_EDITOR
            ++m_StatisticsCounterRaycast;
#endif

            p_ValidationResultData.m_Result.BaseElementValidation_UnderGround =
                result ? ABS_PositionValidationResult.ResultOptions.Failed
                       : ABS_PositionValidationResult.ResultOptions.Validated;
        }

        private void ValidateAirHeight(
            in ABS_PositionValidationData p_ValidationResultData,
            in Vector3 p_CheckedPosition)
        {
            Vector3 pos = p_CheckedPosition;
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

            Vector3 ground = Vector3.zero;
            if (GetGroundPosition(pos, out ground, m_Settings.MaximumAirHeight))
            {
                p_ValidationResultData.m_Result.BaseElementValidation_AirHeightLimit = 
                    ABS_PositionValidationResult.ResultOptions.Validated;
            }
            else
            {
                p_ValidationResultData.m_Result.BaseElementValidation_AirHeightLimit =
                    ABS_PositionValidationResult.ResultOptions.Failed;
            }
        }
        
        private void ValidateBuildableGround(
            in ABS_PositionValidationData p_ValidationResultData, 
            in ABS_Building p_TargetBuilding, 
            in Vector3 p_GlobalPosition, 
            in Quaternion p_GlobalRotation)
        {
            Vector3 localPosition = GetLocalPosition(p_TargetBuilding, p_GlobalPosition);

            Vector3 halfDimension = m_ActiveBuildingElement.Shifting;
            Vector3 shortenDimension = halfDimension * 0.9f;
            float elementBottom = localPosition.y - halfDimension.y + m_Settings.BuildableGround_RangeOffset;
            float range = m_Settings.BuildableGround_RangeLimit + m_Settings.BuildableGround_RangeOffset;

            Vector3 tmpCalcualtedLocalPosition1 = new Vector3(localPosition.x + shortenDimension.x, elementBottom, localPosition.z + shortenDimension.z);
            Vector3 tmpCalcualtedLocalPosition2 = new Vector3(localPosition.x + shortenDimension.x, elementBottom, localPosition.z - shortenDimension.z);
            Vector3 tmpCalcualtedLocalPosition3 = new Vector3(localPosition.x - shortenDimension.x, elementBottom, localPosition.z + shortenDimension.z);
            Vector3 tmpCalcualtedLocalPosition4 = new Vector3(localPosition.x - shortenDimension.x, elementBottom, localPosition.z - shortenDimension.z);

            Vector3 calcualtedGlobalPosition1 = GetGlobalPosition(p_TargetBuilding, tmpCalcualtedLocalPosition1);
            Vector3 calcualtedGlobalPosition2 = GetGlobalPosition(p_TargetBuilding, tmpCalcualtedLocalPosition2);
            Vector3 calcualtedGlobalPosition3 = GetGlobalPosition(p_TargetBuilding, tmpCalcualtedLocalPosition3);
            Vector3 calcualtedGlobalPosition4 = GetGlobalPosition(p_TargetBuilding, tmpCalcualtedLocalPosition4);

            Vector3 groundPosition = Vector3.zero;
            bool result1 = GetGroundPosition(calcualtedGlobalPosition1, out groundPosition, range);
            bool result2 = GetGroundPosition(calcualtedGlobalPosition2, out groundPosition, range);
            bool result3 = GetGroundPosition(calcualtedGlobalPosition3, out groundPosition, range);
            bool result4 = GetGroundPosition(calcualtedGlobalPosition4, out groundPosition, range);

            if (m_Settings.BuildableGround_ShouldAllCheckHit)
            {
                if (result1 && result2 && result3 && result4)
                {
                    p_ValidationResultData.m_Result.BaseElementValidation_BuildableGround =
                        ABS_PositionValidationResult.ResultOptions.Validated;
                }
            }
            else if (result1 || result2 || result3 || result4)
            {
                p_ValidationResultData.m_Result.BaseElementValidation_BuildableGround =
                    ABS_PositionValidationResult.ResultOptions.Validated;
            }

            p_ValidationResultData.m_Result.BaseElementValidation_BuildableGround =
                ABS_PositionValidationResult.ResultOptions.Failed;
        }

        private void ValidateBuildOnTopOfElement(in ABS_PositionValidationData p_ValidationResultData, in Vector3 p_GlobalPosition)
        {
            Vector3 startPosition = p_GlobalPosition - m_ActiveBuildingElement.VerticalShifting + (Vector3.up * 0.01f);
            Vector3 hitPosition;
            RaycastHit hit;
            bool isElementOnTopOfElement = ABS_Raycaster.GetGroundPosition(startPosition, out hitPosition, m_Settings.LayerCollection.LayerOfBuildingElement, out hit, 0.02f);

            ABS_BuildingElement hitElement = null;
            if (isElementOnTopOfElement)
            {
                Transform hitTransform = hit.transform;
                if (hitTransform && hitTransform != null)
                {
                    hitElement = ABS_BuildingElementLink.FindElement(hitTransform);
                }
                else
                {
                    isElementOnTopOfElement = false;
                }
            }

            p_ValidationResultData.m_ElementTarget_BuildOnTopOfElement = hitElement;

            if (isElementOnTopOfElement)
            {
                if (m_Settings.BuildOnTopOfElementResultHandling == ABS_m_BuildOnTopOfElementResultHandling.Block)
                {
                    p_ValidationResultData.m_Result.BaseElementValidation_BuildOnTopOfElement =
                        ABS_PositionValidationResult.ResultOptions.Failed;
                }
                else if (m_Tracker.CanBuiltOnTopOfElement(m_ActiveBuildingElement, hitElement))
                {
                    p_ValidationResultData.m_Result.BaseElementValidation_BuildOnTopOfElement =
                        ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    p_ValidationResultData.m_Result.BaseElementValidation_BuildOnTopOfElement =
                        ABS_PositionValidationResult.ResultOptions.Failed;
                }
            }
            else
            {
                if (m_Settings.BuildOnTopOfElementResultHandling == ABS_m_BuildOnTopOfElementResultHandling.Needed)
                {
                    p_ValidationResultData.m_Result.BaseElementValidation_BuildOnTopOfElement =
                        ABS_PositionValidationResult.ResultOptions.Failed;
                }
                else
                {
                    p_ValidationResultData.m_Result.BaseElementValidation_BuildOnTopOfElement =
                    ABS_PositionValidationResult.ResultOptions.Validated;
                }
             }
        }

        private Vector3 GetLocalPosition (in ABS_Building p_TargetBuilding, in Vector3 p_GlobalPosition)
        {
            return p_TargetBuilding == null
                ? m_Manager.GetTMPLocalPositionFromGlobal(p_GlobalPosition)
                : p_TargetBuilding.transform.InverseTransformPoint(p_GlobalPosition);
        }

        private Vector3 GetGlobalPosition(in ABS_Building p_TargetBuilding, in Vector3 p_LocalPosition)
        {
            return p_TargetBuilding == null
                ? m_Manager.GetGlobalPositionFromTMPLocal(p_LocalPosition)
                : p_TargetBuilding.transform.TransformPoint(p_LocalPosition);
        }

        private void GroundedCheck (
            in ABS_PositionValidationData p_ValidationResultData,
            in Vector3 p_Position,
            in Quaternion p_Rotation)
        {
            Collider[] colliders = OverlapBox(p_Position, p_Rotation, m_Settings.LayerCollection.LayerOfGround, m_Settings.ColliderSizeModifier);
            if (colliders.Length == 0)
            {
                p_ValidationResultData.m_Result.BaseElementValidation_GroundedCheck =
                    ABS_PositionValidationResult.ResultOptions.Failed;
            }
            else
            {
                p_ValidationResultData.m_Result.BaseElementValidation_GroundedCheck =
                    ABS_PositionValidationResult.ResultOptions.Validated;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  protected Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected bool GetGroundPosition(in Vector3 p_CheckedPosition, out Vector3 p_GroundPosition, in float p_Range = float.MaxValue)
        {
#if UNITY_EDITOR
            ++m_StatisticsCounterRaycast;
#endif
            RaycastHit hit;
            return ABS_Raycaster.GetGroundPosition(p_CheckedPosition, out p_GroundPosition, m_Settings.LayerCollection.LayerOfGround, out hit, p_Range);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public void OnDrawGizmos(in ABS_PositionSearchResult p_PositionSearchResult, in ABS_ProjectSettings p_ProjectSettings)
        {
            if (m_BuildableGround_RaycastHappened
                && p_ProjectSettings.PositionValidation_BuildableGround)
            {
                m_BuildableGround_RaycastHappened = false;

                Vector3 rangeModifier = new Vector3(0, m_Settings.BuildableGround_RangeLimit + m_Settings.BuildableGround_RangeOffset, 0);

                REST_GizmosUtils.DrawArrow(
                    m_BuildableGround_RaycastSource1, 
                    m_BuildableGround_RaycastSource1 - rangeModifier,
                    m_BuildableGround_RaycastHit1 ? UnityEngine.Color.green : UnityEngine.Color.red);

                REST_GizmosUtils.DrawArrow(
                    m_BuildableGround_RaycastSource2, 
                    m_BuildableGround_RaycastSource2 - rangeModifier,
                    m_BuildableGround_RaycastHit2 ? UnityEngine.Color.green : UnityEngine.Color.red);

                REST_GizmosUtils.DrawArrow(
                    m_BuildableGround_RaycastSource3, 
                    m_BuildableGround_RaycastSource3 - rangeModifier,
                    m_BuildableGround_RaycastHit3 ? UnityEngine.Color.green : UnityEngine.Color.red);

                REST_GizmosUtils.DrawArrow(
                    m_BuildableGround_RaycastSource4,
                    m_BuildableGround_RaycastSource4 - rangeModifier,
                    m_BuildableGround_RaycastHit4 ? UnityEngine.Color.green : UnityEngine.Color.red);
            }
        }
#endif
    }
}

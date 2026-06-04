//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewSnapPointBasedBuilderSettings", menuName = "AdvancedBuildingSystem/Building Algorithm Settings/New SnapPointBasedBuilder Settings")]
    public class ABS_SnapPointBasedBuilderSettings : ABS_BuilderBaseSettings
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private ABS_SnapRelationshipList m_SnapRelationshipList = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_SnapRelationshipList ABS_SnapRelationshipList => m_SnapRelationshipList;

        public override ABS_PositionSearchAlgorithm AlgorithmType => ABS_PositionSearchAlgorithm.SnapPointBased;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_BuilderBaseSettings Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override bool IsDragBuildingSpecSupported()
        {
            return false;
        }

        public override bool IsRepositionSpecSupported()
        {
            return true;
        }

        public override bool IsOverrideElementSpecSupported()
        {
            return true;
        }
        public override bool IsValidationCollisionCheckSupported()
        {
            return true;
        }

        public override bool IsSpecialRuleValidationSupported()
        {
            return false;
        }

        public override bool IsUnderGroundValidationSupported()
        {
            return true;
        }
        public override bool IsGroundedCheckSupported()
        {
            return true;
        }
        public override bool IsBuildableGroundValidationSupported()
        {
            return true;
        }
        public override bool IsAlignRotationToGroundStrategySupported()
        {
            return false;
        }
        public override bool IsAllowBuildingInTheAirSupported()
        {
            return false;
        }
        public override bool IsAllowPositionSearchAtRaycastEndPositionSupported()
        {
            return true;
        }
        public override bool IsAlignPositionToGroundSupported()
        {
            return true;
        }
        public override bool IsPrioritizePreBuiltSupported()
        {
            return true;
        }
        public override bool IsFoundationLogicSupported()
        {
            return true;
        }
        public override bool IsBuildOnTopOfElementSupported()
        {
            return true;
        }
        public override bool IsStabilitySupported()
        {
            return false;
        }
    }
}
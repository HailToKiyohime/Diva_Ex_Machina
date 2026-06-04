//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewFreeBuilderSettings", menuName = "AdvancedBuildingSystem/Building Algorithm Settings/New FreeBuilder Settings")]
    public class ABS_FreeBuilderSettings : ABS_BuilderBaseSettings
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private bool m_EnableAttachementConnection = true;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override ABS_PositionSearchAlgorithm AlgorithmType => ABS_PositionSearchAlgorithm.Free;

        public bool EnableAttachementConnection => m_EnableAttachementConnection;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  implementation ABS_BuilderBaseSettings
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override bool IsDragBuildingSpecSupported()
        {
            return true;
        }

        public override bool IsRepositionSpecSupported()
        {
            return false;
        }

        public override bool IsOverrideElementSpecSupported()
        {
            return false;
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
            return true;
        }
        public override bool IsAllowBuildingInTheAirSupported()
        {
            return true;
        }
        public override bool IsAllowPositionSearchAtRaycastEndPositionSupported()
        {
            return false;
        }
        public override bool IsAlignPositionToGroundSupported()
        {
            return true;
        }
        public override bool IsPrioritizePreBuiltSupported()
        {
            return false;
        }
        public override bool IsFoundationLogicSupported()
        {
            return false;
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
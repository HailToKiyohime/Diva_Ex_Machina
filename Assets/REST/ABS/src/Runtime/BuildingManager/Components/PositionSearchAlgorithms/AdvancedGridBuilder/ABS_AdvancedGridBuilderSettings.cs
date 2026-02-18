//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewAdvancedGridBuilderSettings", menuName = "AdvancedBuildingSystem/Building Algorithm Settings/New AdvancedGridBuildier Settings")]
    public class ABS_AdvancedGridBuilderSettings : ABS_BuilderBaseSettings
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //----------------------------------------------------------------------------------------------------------------------
        //Grid building
        [SerializeField] private Vector3 m_GridSize = Vector3.one;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public Vector3 GridSize => m_GridSize;
        public Vector3 HalfGridSize => m_GridSize / 2;
        public Vector3 MixedAxisGridSize => new Vector3(m_GridSize.x / 2f, m_GridSize.y, m_GridSize.z / 2f);

        public override ABS_PositionSearchAlgorithm AlgorithmType => ABS_PositionSearchAlgorithm.AdvancedGrid;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_BuilderBaseSettings Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public override bool IsDragBuildingSpecSupported()
        {
            return true;
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
            return true;
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
            return true;
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
            return true;
        }
    }
}
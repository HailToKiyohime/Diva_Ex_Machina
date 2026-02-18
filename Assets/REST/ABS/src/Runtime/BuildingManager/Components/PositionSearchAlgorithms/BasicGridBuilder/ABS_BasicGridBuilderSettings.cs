//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum VerticalGridPlacement
    {
        FixedPosition,
        RaycastPosition,
        AlignToGround
    }

    [CreateAssetMenu(fileName = "NewBasicGridBuilderSettings", menuName = "AdvancedBuildingSystem/Building Algorithm Settings/New BasicGridBuilder Settings")]
    public class ABS_BasicGridBuilderSettings : ABS_BuilderBaseSettings
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private Vector2 m_GridSize = Vector2.one;

        [SerializeField] private VerticalGridPlacement m_VerticalGridPlacement = VerticalGridPlacement.FixedPosition;
        [SerializeField] private float m_VerticalGridFixedPosition = 0f;


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public Vector3 GridSize => new Vector3(m_GridSize.x, 0, m_GridSize.y); 
        public VerticalGridPlacement VerticalGridPlacement => m_VerticalGridPlacement;
        public float VerticalGridFixedPosition => m_VerticalGridFixedPosition;

        public override ABS_PositionSearchAlgorithm AlgorithmType => ABS_PositionSearchAlgorithm.BasicGrid;

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
            return false;
        }

        public override bool IsUnderGroundValidationSupported()
        {
            return true;
        }
        public override bool IsGroundedCheckSupported()
        {
            return false;
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
            return false;
        }
        public override bool IsPrioritizePreBuiltSupported()
        {
            return true;
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
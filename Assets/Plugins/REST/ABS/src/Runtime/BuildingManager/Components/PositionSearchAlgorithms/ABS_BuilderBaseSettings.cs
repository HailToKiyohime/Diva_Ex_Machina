//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_RotationStrategy
    {
        NoRotation,
        PlayerRotation,
        CameraRotation,
        CamerAndPlayerRotation,
        FixDegree
    }

    public enum ABS_AlignRotationStrategy
    {
        None,
        Always,
        TurnOnOff,
        ButtonHold
    }

    public enum ABS_ElementVisibilityStrategy
    {
        [Tooltip("The BuildingElement is always visible")]
        VisibleAlways,
        [Tooltip("The BuildingElement is only visible when the raycast hit something")]
        VisibleOnHit
    }

    public enum ABS_RepositionStrategy
    {
        Instant,
        Smooth
    }

    public enum ABS_AirPositionReferencePoint
    {
        Top,
        Center,
        Bottom
    }

    public enum ABS_ValidationFailureHandling
    {
        DenyBuilding,
        BlockBuilding
    }

    public enum ABS_m_BuildOnTopOfElementResultHandling
    {
        Needed,
        Allow,
        Block
    }

    public enum ABS_OverrideStrategy
    {
        None,
        Ruleset
    }

    public enum ABS_ElementRotaionAlignmentStrategy
    {
        CopyRotation,
        AlignBasedOnStartRotation,
        AlignBasedOnCameraRotation,
        LockOnTargetFixed,
        LockOnTargetContinuous
    }

    public abstract class ABS_BuilderBaseSettings : ScriptableObject
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //----------------------------------------------------------------------------------------------------------------------
        // Basics
        [SerializeField][Range(0.1f, 10.0f)] private float m_SearchRadius = 3f;
        [SerializeField][Range(0.1f, 10.0f)] private float m_BuildRadius = 4f;
        [SerializeField] private ABS_ElementVisibilityStrategy m_ElementVisiblity = ABS_ElementVisibilityStrategy.VisibleAlways;
        [SerializeField] private bool m_AllowPositionSearchAtRaycastEndPosition = true;
        [SerializeField] private bool m_AlignPositionToGround = true;
        [SerializeField] private bool m_PrioritizePreBuilt = true;
        [SerializeField] private bool m_UseFoundationLogic = false;

        //----------------------------------------------------------------------------------------------------------------------
        // Search Base Properties
        [SerializeField] private ABS_LayerCollection m_LayerCollection = null;

        //----------------------------------------------------------------------------------------------------------------------
        // Rotation
        [SerializeField] private ABS_RotationStrategy m_RotationStrategy = ABS_RotationStrategy.CamerAndPlayerRotation;
        [SerializeField][Range(0f, 360f)] private float m_RotationYDegree = 22.5f;

        [SerializeField] private ABS_AlignRotationStrategy m_AlignRotationToGroundStrategy = ABS_AlignRotationStrategy.None;
        [SerializeField][Range(0f, 90f)] private float m_MaximumRotationAlignment = 90f;

        [SerializeField] private bool m_EnableAlignRotationToBuildingElements = false;
        [SerializeField] private ABS_ElementRotaionAlignmentStrategy m_ElementRotaionAlignmentStrategy = ABS_ElementRotaionAlignmentStrategy.AlignBasedOnCameraRotation;

        //----------------------------------------------------------------------------------------------------------------------
        // Drag ABS_Building
        [SerializeField] private bool m_EnableDragBuilding = true;
        [SerializeField] private bool m_DragBuildingLimitX = false;
        [SerializeField] private uint m_DragBuildingLimitXAmount = 1;
        [SerializeField] private bool m_DragBuildingLimitZ = false;
        [SerializeField] private uint m_DragBuildingLimitZAmount = 1;
        [SerializeField] private bool m_AllowDragBuildingWithoutHit = true;

        //----------------------------------------------------------------------------------------------------------------------
        // Reposition Type
        [SerializeField] private ABS_RepositionStrategy m_RepositionStrategy = ABS_RepositionStrategy.Smooth;
        [SerializeField] private bool m_AllowPlacementDuringMovement = true;
        [SerializeField] [Range(1f, 100f)] private float m_RepositionMoveSpeed = 50f;
        [SerializeField] [Range(1f, 100f)] private float m_RepositionRotateSpeed = 50;

        //----------------------------------------------------------------------------------------------------------------------
        //Element Modification Settings
        [SerializeField] private ABS_OverrideStrategy m_OverrideStrategy = ABS_OverrideStrategy.None;
        [SerializeField] private ABS_OverrideElementRuleset m_OverrideElementRuleset = null;

        //----------------------------------------------------------------------------------------------------------------------
        // Validation

        [SerializeField][Range(0.5f, 1.5f)] private float m_ColliderSizeModifier = 0.99f;

        [SerializeField] private bool m_PositionValidationElementCollisionCheck = true;
        [SerializeField] private ABS_ValidationFailureHandling m_ElementCollisionFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;

        [SerializeField] private bool m_PositionValidationCollisionCheck = false;
        [SerializeField] private ABS_ValidationFailureHandling m_CollisionFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;

        [SerializeField] private bool m_CheckUnderGroundPosition = true;

        [SerializeField] private bool m_GroundedCheck = false;
        [SerializeField] private ABS_ValidationFailureHandling m_GroundedCheckFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;

        [SerializeField] private bool m_BuildableGroundValidation = false;
        [SerializeField] private ABS_ValidationFailureHandling m_BuildableGroundValidationFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;
        [SerializeField] private bool m_BuildableGround_ShouldAllCheckHit = false;
        [SerializeField] private float m_BuildableGround_RangeOffset = 0.1f;
        [SerializeField] private float m_BuildableGround_RangeLimit = 0.3f;

        [SerializeField] private bool m_AllowBuildingInTheAir = false;
        [SerializeField] private bool m_AddMaximumHeightToAirBuilding = false;
        [SerializeField] private ABS_AirPositionReferencePoint m_AirPositionReferencePoint = ABS_AirPositionReferencePoint.Center;
        [SerializeField] private float m_MaximumAirHeight = 10f;

        [SerializeField] private bool m_SpecialRuleValidation = false;
        [SerializeField] private ABS_ValidationFailureHandling m_SpecialRuleValidationFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;

        [SerializeField] private bool m_BuildOnTopOfElement = false;
        [SerializeField] private ABS_m_BuildOnTopOfElementResultHandling m_BuildOnTopOfElementResultHandling = ABS_m_BuildOnTopOfElementResultHandling.Block;
        
        //OTHERS
        [SerializeField] private ABS_ValidationFailureHandling m_ShouldSnapToFoundationFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;
        [SerializeField] private ABS_ValidationFailureHandling m_StabiltyFailureHandling = ABS_ValidationFailureHandling.BlockBuilding;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters/Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        // Basics
        public ABS_ElementVisibilityStrategy ElementVisiblity => m_ElementVisiblity;
        public bool AllowPositionSearchAtRaycastEndPosition 
        { 
            get 
            { 
                return IsAllowPositionSearchAtRaycastEndPositionSupported() 
                    && m_AllowPositionSearchAtRaycastEndPosition; 
            } 
        }
        public bool AlignPositionToGround => m_AlignPositionToGround;
        public bool PrioritizePreBuilt => m_PrioritizePreBuilt;
        public bool UseFoundationLogic => m_UseFoundationLogic;

        // Search Base Properties
        public ABS_LayerCollection LayerCollection => m_LayerCollection;

        // Rotation
        public ABS_AlignRotationStrategy AlignRotationToGroundStrategy => m_AlignRotationToGroundStrategy;
        public float MaximumRotationAlignment => m_MaximumRotationAlignment;
        public bool EnableAlignRotationToBuildingElements => m_EnableAlignRotationToBuildingElements;
        public ABS_ElementRotaionAlignmentStrategy ElementRotaionAlignmentStrategy => m_ElementRotaionAlignmentStrategy;
        public ABS_RotationStrategy RotationStrategy => m_RotationStrategy;
        public float RotationYDegree => m_RotationYDegree;

        // Drag ABS_Building
        public bool EnableDragBuilding => m_EnableDragBuilding;
        public bool DragBuildingLimitX => m_DragBuildingLimitX;
        public uint DragBuildingLimitXAmount => m_DragBuildingLimitXAmount;
        public bool DragBuildingLimitZ => m_DragBuildingLimitZ;
        public uint DragBuildingLimitZAmount => m_DragBuildingLimitZAmount;
        public bool AllowDragBuildingWithoutHit => m_AllowDragBuildingWithoutHit;

        // Reposition Type
        public ABS_RepositionStrategy RepositionStrategy => m_RepositionStrategy;
        public bool AllowPlacementDuringMovement => m_AllowPlacementDuringMovement;
        public float RepositionMoveSpeed => m_RepositionMoveSpeed;
        public float RepositionRotateSpeed => m_RepositionRotateSpeed;

        //Element Modification Settings
        public ABS_OverrideStrategy OverrideStrategy => m_OverrideStrategy;
        public ABS_OverrideElementRuleset OverrideElementRuleset => m_OverrideElementRuleset;

        // Validation
        public bool PositionValidationElementCollisionCheck => m_PositionValidationElementCollisionCheck;
        public ABS_ValidationFailureHandling ElementCollisionFailureHandling => m_ElementCollisionFailureHandling;

        public bool PositionValidationCollisionCheck => m_PositionValidationCollisionCheck;
        public ABS_ValidationFailureHandling CollisionFailureHandling => m_CollisionFailureHandling;

        public bool CheckUnderGroundPosition => m_CheckUnderGroundPosition;

        public bool GroundedCheck => m_GroundedCheck;
        public ABS_ValidationFailureHandling GroundedCheckFailureHandling => m_GroundedCheckFailureHandling;
        public float ColliderSizeModifier => m_ColliderSizeModifier;

        public bool BuildableGroundValidation => m_BuildableGroundValidation;
        public ABS_ValidationFailureHandling BuildableGroundValidationFailureHandling => m_BuildableGroundValidationFailureHandling;
        public bool BuildableGround_ShouldAllCheckHit => m_BuildableGround_ShouldAllCheckHit;
        public float BuildableGround_RangeOffset => m_BuildableGround_RangeOffset;
        public float BuildableGround_RangeLimit => m_BuildableGround_RangeLimit;

        public bool AllowBuildingInTheAir => IsAllowBuildingInTheAirSupported() && m_AllowBuildingInTheAir;
        public bool AddMaximumHeightToAirBuilding => m_AddMaximumHeightToAirBuilding;
        public ABS_AirPositionReferencePoint AirPositionReferencePoint => m_AirPositionReferencePoint;
        public float MaximumAirHeight => m_MaximumAirHeight;

        public bool SpecialRuleValidation => m_SpecialRuleValidation;
        public ABS_ValidationFailureHandling SpecialRuleValidationFailureHandling => m_SpecialRuleValidationFailureHandling;

        public bool BuildOnTopOfElement => m_BuildOnTopOfElement;
        public ABS_m_BuildOnTopOfElementResultHandling BuildOnTopOfElementResultHandling => m_BuildOnTopOfElementResultHandling;

        public ABS_ValidationFailureHandling ShouldSnapToFoundationFailureHandling => m_ShouldSnapToFoundationFailureHandling;
        public ABS_ValidationFailureHandling StabiltyFailureHandling => m_StabiltyFailureHandling;

        // SearchAndBuilding Range
        public float SearchRadius  => m_SearchRadius;
        public float BuildRadius => m_BuildRadius;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstract Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public abstract ABS_PositionSearchAlgorithm AlgorithmType { get; }

        //Basics
        public abstract bool IsAllowPositionSearchAtRaycastEndPositionSupported();
        public abstract bool IsAlignPositionToGroundSupported();
        public abstract bool IsPrioritizePreBuiltSupported();
        public abstract bool IsFoundationLogicSupported();

        //Rotation
        public abstract bool IsAlignRotationToGroundStrategySupported();

        //DragBuilding
        public abstract bool IsDragBuildingSpecSupported();

        //Reposition
        public abstract bool IsRepositionSpecSupported();

        //ElementModifications
        public abstract bool IsOverrideElementSpecSupported();

        //Validations
        public abstract bool IsValidationCollisionCheckSupported();
        public abstract bool IsUnderGroundValidationSupported();
        public abstract bool IsGroundedCheckSupported();
        public abstract bool IsBuildableGroundValidationSupported();
        public abstract bool IsAllowBuildingInTheAirSupported();
        public abstract bool IsSpecialRuleValidationSupported();
        public abstract bool IsBuildOnTopOfElementSupported();
        public abstract bool IsStabilitySupported();
    }
}
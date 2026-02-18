//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IBuildingManagerInternalInterface
    {
        //----------------------------------------------------------------------------------------------------------------------
        // MetaData
        public bool IsSandbox { get; }
        public bool IsCachingEnabled { get; }
        public ABS_BuildingManagerBuildMode BuildMode { get; }

        //----------------------------------------------------------------------------------------------------------------------
        //Raycast
        public ABS_Raycaster Raycaster { get; }
        public ABS_LayerCollection LayerCollection { get; }
        public float RaycastDistance { get; }
        public float RaycastOffset { get; }
        public Transform HitTransform { get; }
        public Camera Camera { get; }
        public Vector3 GetRaycastHitOrEndPosition();
        public Vector3 GetRaycastEndPosition();

        //----------------------------------------------------------------------------------------------------------------------
        // Parent
        public ABS_BasicGridBuilding GlobalBasicGridParent { get; }
        public ABS_FreeBuilding GlobalFreeParent { get; }
        public ABS_Building GetParentForNewBuildingElement();
        public ABS_BuildingParent GetBuildingParent();
        public Vector3 GetParentPositionAlignment(ABS_BuildingElement p_TargetElement);

        //----------------------------------------------------------------------------------------------------------------------
        //Input
        public ABS_InputType InputType { get; }
        public ABS_RotationInputType RotationInputType { get; }

        public KeyCode KeyForRotationRight { get; }
        public KeyCode KeyForRotationLeft { get; }

        public KeyCode KeyForBuild { get; }
        public KeyCode KeyForDestroy { get; }
        public KeyCode KeyForDragBuild { get; }
        public KeyCode KeyForDragDestroy { get; }
        public KeyCode KeyForUndo { get; }
        public KeyCode KeyForRedo { get; }
        public KeyCode KeyForModeChange { get; }
        public KeyCode KeyForForcedFallback { get; }
        public KeyCode KeyForAlignRotationToGround { get; }
        public KeyCode KeyForAlignRotationToBuildingElements { get; }

        //----------------------------------------------------------------------------------------------------------------------
        //Destroy
        public DestroyType DestroyType { get; }
        public float DestroyTimerDuration { get; }
        public bool CutTimerOnLookAway { get; }

        public uint MaximumDestoryCount { get; }

        //----------------------------------------------------------------------------------------------------------------------
        //Drag Buidling
        public ABS_PositionValidationData ValidatePosition(in Vector3 p_Position, in UnityEngine.Quaternion p_Rotation);

        //----------------------------------------------------------------------------------------------------------------------
        //Temporay
        public Vector3 GetTMPLocalPositionFromGlobal (in Vector3 p_GlobalPosition);
        public Vector3 GetGlobalPositionFromTMPLocal (in Vector3 p_LocalPosition);

        //----------------------------------------------------------------------------------------------------------------------
        //Others
        public bool IsRotationAlignmentToGroundOn();
        public bool IsRotationAlignmentToGroundHold();
        public bool IsAlignRotationToBuidlingElementsTriggered();
        public bool IsForcedFallbackIsOn();
    }
}
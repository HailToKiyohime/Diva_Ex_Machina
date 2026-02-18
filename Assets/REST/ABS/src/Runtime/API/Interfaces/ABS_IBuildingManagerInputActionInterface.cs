//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_InputType
    {
        Messages,
        Key,
        Both
    }

    public enum ABS_RotationInputType
    {
        MouseWheel,
        Button
    }

    public interface ABS_IBuildingManagerInputActionInterface
    {
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Settings
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_InputType InputType { get; set; }
        public ABS_RotationInputType RotationInputType { get; set; }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Legacy Keycodes
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public KeyCode KeyForRotationRight { get; set; }
        public KeyCode KeyForRotationLeft { get; set; }

        public KeyCode KeyForBuild { get; set; }
        public KeyCode KeyForDestroy { get; set; }
        public KeyCode KeyForDragBuild { get; set; }
        public KeyCode KeyForDragDestroy { get; set; }

        public KeyCode KeyForUndo { get; set; }
        public KeyCode KeyForRedo { get; set; }

        public KeyCode KeyForModeChange { get; set; }
        public KeyCode KeyForForcedFallback { get; set; }

        public KeyCode KeyForAlignRotationToGround { get; set; }
        public KeyCode KeyForAlignRotationToBuildingElements { get; set; }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Input Messages
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //Change BuildingMode
        public void OnChangeModePressed();
        public void OnChangeModePressedImmediate();

        //BuildingProcess
        public void OnForcedFallbackPressed();
        public void OnForcedFallbackPressedImmediate();

        public void OnSimpleBuildingPressed();
        public void OnSimpleBuildingPressedImmediate();
        public void OnDragBuildingPressed();
        public void OnDragBuildingPressedImmediate();
        public void OnDragBuildingReleased();
        public void OnDragBuildingReleasedImmediate();

        public void OnSimpleDestroyReleased();
        public void OnSimpleDestroyReleasedImmediate();
        public void OnSimpleDestroyPressed();
        public void OnSimpleDestroyPressedImmediate();
        public void OnDragDestroyPressed();
        public void OnDragDestroyPressedImmediate();
        public void OnDragDestroyReleased();
        public void OnDragDestroyReleasedImmediate();

        //Rotation
        public void OnRotationButtonRightPressed();
        public void OnRotationButtonRightPressedImmediate();
        public void OnRotationButtonRightHold();
        public void OnRotationButtonRightHoldImmediate();
        public void OnRotationButtonRightReleased();
        public void OnRotationButtonRightReleasedImmediate();

        public void OnRotationButtonLeftPressed();
        public void OnRotationButtonLeftPressedImmediate();
        public void OnRotationButtonLeftHold();
        public void OnRotationButtonLeftHoldImmediate();
        public void OnRotationButtonLeftReleased();
        public void OnRotationButtonLeftReleasedImmediate();

        //RotationAlignment
        public void OnAlignRotationToGroundPressed();
        public void OnAlignRotationToGroundPressedImmediate();
        public void OnAlignRotationToGroundReleased();
        public void OnAlignRotationToGroundReleasedImmediate();
        public void OnAlignRotationToBuildingElementsPressed();
        public void OnAlignRotationToBuildingElementsPressedImmediate();

        //History
        public void OnUndoPressed();
        public void OnUndoPressedImmediate();
        public void OnRedoPressed();
        public void OnRedoPressedImmediate();
    }
}

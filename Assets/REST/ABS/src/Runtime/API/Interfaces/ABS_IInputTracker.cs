//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IInputTracker
    {
        //Change BuildingMode
        public void ChangeModePressed();

        //BuildingProcess
        public void ForcedFallbackPressed();
        public void SimpleBuildingPressed();
        public void DragBuildingPressed();
        public void DragBuildingReleased();

        //Rotation
        public void MouseWheelChanged(in float p_Value);
        public void RotationButtonRightPressed();
        public void RotationButtonRightHold();
        public void RotationButtonRightReleased();
        public void RotationButtonLeftPressed();
        public void RotationButtonLeftHold();
        public void RotationButtonLeftReleased();

        //RotationAlignment
        public void AlignRotationToGroundPressed();
        public void AlignRotationToGroundReleased();
        public void AlignRotationToBuildingElementsPressed();

        //History
        public void UndoPressed();
        public void RedoPressed();

        //Destroy
        public void SimpleDestroyPressed();
        public void SimpleDestroyHold();
        public void SimpleDestroyReleased();
        public void DragDestroyPressed();
        public void DragDestroyReleased();
    }
}


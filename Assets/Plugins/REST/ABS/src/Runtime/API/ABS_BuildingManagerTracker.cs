//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_BuildingManagerTracker : MonoBehaviour, ABS_IInputTracker
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        protected ABS_IBuildingManagerExternalInterface m_BuildingManager = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region ExternalInterface
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual void SetExternalInterface(in ABS_IBuildingManagerExternalInterface p_ExternalInterface)
        {
            m_BuildingManager = p_ExternalInterface;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // ExternalInterface
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region General processes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual void Activated(in ABS_BuildingElement p_BuildingElementPrefab, in ABS_BuildingManagerBuildMode p_BuildMode) { }
        public virtual void ActivatedOnlyDestroy() { }
        public virtual void Deactivated() { }
        public virtual void ModeChanged(ABS_BuildingManagerMode p_Mode) { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // General processes
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region BuildingElement
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual void ResetBuildingElement(in ABS_BuildingElement p_BuildingElement) { }
        public virtual bool BeforePlace(in ABS_BuildingElement p_BuildingElement, in Vector3 p_Position, in Quaternion p_Rotation, in ABS_Building p_ParentBuilding) { return true; }
        public virtual void BuildingElementPlaced(List<ABS_BuildingElement> p_PlacedBuildingElements) { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // BuildingElement
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Building
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual void BuildingPlaced(in ABS_Building p_Building) { }
        public virtual void BuildingWillBeDestroyed(in ABS_Building p_Building) { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Building
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Building Cycle
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual void StartOfBuildingCycle() { }
        public virtual void EndOfBuildingCycle() { }
        public virtual void SimpleBuildResult(ABS_SimpleBuildingProcessErrorCode p_ErrorCode) { }
        public virtual void RaycastOnHit(in bool p_Hit, in GameObject p_HitObject) { }

        //PositionCustomValidation : Only for AdvancedGrid and BasicGrid Building
        public virtual bool PositionCustomValidation(ABS_BuildingElement p_ActiveBuildingElement, Vector3 p_CheckedPosition, Vector3 p_Hitpoint, Quaternion p_Rotation) { return true; }
        public virtual bool CanBuiltOnTopOfElement(ABS_BuildingElement p_ActiveBuildingElement, ABS_BuildingElement p_BuildTargetElement) { return true; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Building Cycle
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Drag Building Process
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual void DragBuildingStarted() { }
        public virtual void DragBuildingStoped() { }
        public virtual void FirstBuildingElementBlockedStateChanged(in ABS_TemporaryBuildingElement.ABS_BlockState p_BlockState) { }
        public virtual void FirstBuildingElementCurrentPosition(in Vector3 p_Position, in Quaternion p_Rotation) { }
        public virtual void CurrentValidBuildingElements(in uint p_Count) { }
        public virtual uint GetMaximumCountOfBuildingElements(in ABS_BuildingElement p_BuildingElement) { return uint.MaxValue; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Drag Building Process
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Destroy Process
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual bool BeforeDestroy(in ABS_BuildingElement p_BuildingElement) { return true; }
        public virtual bool CanBuildingElementWithConnectionsDestroyed(in ABS_BuildingElement p_BuildingElement, in List<ABS_BuildingElement> p_ConnectedElements) { return true; }
        public virtual void DestroyTimerStarted(in float p_DestroyTimerDuratation, in List<ABS_BuildingElement> p_DestoryedElements) { }
        public virtual void DestroyTimerIsOngoing(in float p_TimeLeftBeforDestroy) { }
        public virtual void DestroyTimerStoped() { }
        public virtual void BuildingElementDestroyed(in ABS_BuildingElement p_BuildingElement) { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Destroy Process
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Action History
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public virtual void ActionHistroryRedoResult(ABS_ActionHistoryErrorCodes p_Result) { }
        public virtual void ActionHistroryUndoResult(ABS_ActionHistoryErrorCodes p_Result) { }

        public virtual bool BeforeHistoryDestroy(in ABS_BuildingElement p_BuildingElement) { return true; }
        public virtual void BuildingElementHistoryDestroyed(in ABS_BuildingElement p_BuildingElement) { }
        public virtual bool BeforeHistoryPlace(in ABS_BuildingElement p_BuildingElement, in Vector3 p_Position, in Vector3 p_LocalEulerAngles, in ABS_Building p_ParentBuilding) { return true; }
        public virtual void BuildingElementHistoryPlaced(List<ABS_BuildingElement> p_PlacedBuildingElements) { }
        public virtual void BuildingWillBeHistoryDestroyed(in ABS_Building p_Building) { }
        public virtual void BuildingHistoryPlaced(in ABS_Building p_Building) { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Action History
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Input
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Change BuildingMode
        public virtual void ChangeModePressed() { }

        //BuildingProcess
        public virtual void ForcedFallbackPressed() { }
        public virtual void SimpleBuildingPressed() { }
        public virtual void DragBuildingPressed() { }
        public virtual void DragBuildingReleased() { }

        //Rotation
        public virtual void MouseWheelChanged(in float p_Value) { }
        public virtual void RotationButtonRightPressed() { }
        public virtual void RotationButtonRightHold() { }
        public virtual void RotationButtonRightReleased() { }
        public virtual void RotationButtonLeftPressed() { }
        public virtual void RotationButtonLeftHold() { }
        public virtual void RotationButtonLeftReleased() { }

        //RotationAlignment
        public virtual void AlignRotationToGroundPressed() { }
        public virtual void AlignRotationToGroundReleased() { }
        public virtual void AlignRotationToBuildingElementsPressed() { }

        //History
        public virtual void UndoPressed() { }
        public virtual void RedoPressed() { }

        //Destroy
        public virtual void SimpleDestroyPressed() { }
        public virtual void SimpleDestroyHold() { }
        public virtual void SimpleDestroyReleased() { }
        public virtual void DragDestroyPressed() { }
        public virtual void DragDestroyReleased() { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Input
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
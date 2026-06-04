//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_TrackerInternalHelper : ABS_BuildingManagerTracker
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<ABS_BuildingManagerTracker> m_Trackers = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Init(GameObject[] p_Objects)
        {
            m_Trackers = new List<ABS_BuildingManagerTracker>();
            if (p_Objects != null && p_Objects.Length > 0)
            {
                foreach (GameObject obj in p_Objects)
                {
                    if (obj == null)
                    {
                        REST_Logging.Debug("ABS_TrackerInternalHelper", "Null tracker Object!");
                    }
                    else
                    {
                        ABS_BuildingManagerTracker tracker = obj.GetComponent<ABS_BuildingManagerTracker>();
                        if (tracker == null)
                        {
                            REST_Logging.Debug("ABS_TrackerInternalHelper", $"Tracker GameObject without ABS_BuildingManagerTracker component : {obj.name}!");
                        }
                        else
                        {
                            m_Trackers.Add(tracker);
                        }
                    }
                }
            }
        }

        public void AddTracker(in ABS_BuildingManagerTracker p_Tracker)
        {
            if (!m_Trackers.Contains(p_Tracker))
            {
                m_Trackers.Add(p_Tracker);
            }
        }

        public void RemoveTracker(in ABS_BuildingManagerTracker p_Tracker)
        {
            m_Trackers.Remove(p_Tracker);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_BuildingManagerTracker Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override void SetExternalInterface(in ABS_IBuildingManagerExternalInterface p_ExternalInterface)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.SetExternalInterface(p_ExternalInterface);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //General processes

        public override void Activated(in ABS_BuildingElement p_BuildingElementPrefab, in ABS_BuildingManagerBuildMode p_BuildMode)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.Activated(p_BuildingElementPrefab, p_BuildMode);
            }
        }
        
        public override void ActivatedOnlyDestroy()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ActivatedOnlyDestroy();
            }
        }

        public override void Deactivated()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    //This is triggered when the game is stopped in the Unity editor
                    //In that case the tracker objects can be already deleted
                    //So we do not log here to not spam the log every time when the game stoped
                    continue;
                }
                tracker.Deactivated();
            }
        }

        public override void ModeChanged(ABS_BuildingManagerMode p_Mode)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ModeChanged(p_Mode);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //ABS_BuildingElement
        public override void ResetBuildingElement(in ABS_BuildingElement p_BuildingElement)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ResetBuildingElement(p_BuildingElement);
            }
        }

        public override bool BeforePlace(in ABS_BuildingElement p_BuildingElement, in Vector3 p_Position, in Quaternion p_Rotation, in ABS_Building p_ParentBuilding)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.BeforePlace(p_BuildingElement, p_Position, p_Rotation, p_ParentBuilding);
            }
            return res;
        }

        public override void BuildingElementPlaced(List<ABS_BuildingElement> p_PlacedBuildingElements)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingElementPlaced(p_PlacedBuildingElements);
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //ABS_Building
        public override void BuildingPlaced(in ABS_Building p_Building)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingPlaced(p_Building);
            }
        }

        public override void BuildingWillBeDestroyed(in ABS_Building p_Building)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingWillBeDestroyed(p_Building);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //ABS_Building Cycle
        public override void StartOfBuildingCycle()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.StartOfBuildingCycle();
            }
        }

        public override void EndOfBuildingCycle()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.EndOfBuildingCycle();
            }
        }

        public override void SimpleBuildResult(ABS_SimpleBuildingProcessErrorCode p_ErrorCode)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.SimpleBuildResult(p_ErrorCode);
            }
        }

        public override void RaycastOnHit(in bool p_Hit, in GameObject p_HitObject)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RaycastOnHit(p_Hit, p_HitObject);
            }
        }


        //AdvancedGrid and BasicGrid ABS_Building
        public override bool PositionCustomValidation(ABS_BuildingElement p_ActiveBuildingElement, Vector3 p_CheckedPosition, Vector3 p_Hitpoint, Quaternion p_Rotation)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.PositionCustomValidation(p_ActiveBuildingElement, p_CheckedPosition, p_Hitpoint, p_Rotation);
            }
            return res;
        }

        public override bool CanBuiltOnTopOfElement(ABS_BuildingElement p_ActiveBuildingElement, ABS_BuildingElement p_BuildTargetElement)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.CanBuiltOnTopOfElement(p_ActiveBuildingElement, p_BuildTargetElement);
            }
            return res;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //Drag ABS_Building Process
        public override void DragBuildingStarted()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DragBuildingStarted();
            }
        }

        public override void DragBuildingStoped()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DragBuildingStoped();
            }
        }

        public override void FirstBuildingElementBlockedStateChanged(in ABS_TemporaryBuildingElement.ABS_BlockState p_BlockState)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.FirstBuildingElementBlockedStateChanged(p_BlockState);
            }
        }

        public override void FirstBuildingElementCurrentPosition(in Vector3 p_Position, in Quaternion p_Rotation)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.FirstBuildingElementCurrentPosition(p_Position, p_Rotation);
            }
        }

        public override void CurrentValidBuildingElements(in uint p_Count)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    //This is triggered when the game is stopped in the Unity editor
                    //In that case the tracker objects can be already deleted
                    //So we do not log here to not spam the log every time when the game stoped
                    continue;
                }
                tracker.CurrentValidBuildingElements(p_Count);
            }
        }

        public override uint GetMaximumCountOfBuildingElements(in ABS_BuildingElement p_BuildingElement)
        {
            uint res = uint.MaxValue;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                uint maxNumber = tracker.GetMaximumCountOfBuildingElements(p_BuildingElement);
                if (maxNumber < res)
                {
                    res = maxNumber;
                }
            }
            return res;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //Destroy Process
        public override bool BeforeDestroy(in ABS_BuildingElement p_BuildingElement)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.BeforeDestroy(p_BuildingElement);
            }
            return res;
        }

        public override bool CanBuildingElementWithConnectionsDestroyed(in ABS_BuildingElement p_BuildingElement, in List<ABS_BuildingElement> p_ConnectedElements)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.CanBuildingElementWithConnectionsDestroyed(p_BuildingElement, p_ConnectedElements);
            }
            return res;
        }

        public override void DestroyTimerStarted(in float p_DestroyTimerDuratation, in List<ABS_BuildingElement> p_DestoryedElements)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DestroyTimerStarted(p_DestroyTimerDuratation, p_DestoryedElements);
            }
        }

        public override void DestroyTimerIsOngoing(in float p_TimeLeftBeforDestroy)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DestroyTimerIsOngoing(p_TimeLeftBeforDestroy);
            }
        }

        public override void DestroyTimerStoped()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DestroyTimerStoped();
            }
        }

        public override void BuildingElementDestroyed(in ABS_BuildingElement p_BuildingElement)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingElementDestroyed(p_BuildingElement);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //Action History
        public override void ActionHistroryRedoResult(ABS_ActionHistoryErrorCodes p_Result)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ActionHistroryRedoResult(p_Result);
            } 
        }
        public override void ActionHistroryUndoResult(ABS_ActionHistoryErrorCodes p_Result)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ActionHistroryUndoResult(p_Result);
            }
        }

        public override bool BeforeHistoryDestroy(in ABS_BuildingElement p_BuildingElement)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.BeforeHistoryDestroy(p_BuildingElement);
            }
            return res;
        }
        public override void BuildingElementHistoryDestroyed(in ABS_BuildingElement p_BuildingElement)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingElementHistoryDestroyed(p_BuildingElement);
            }
        }

        public override bool BeforeHistoryPlace(
            in ABS_BuildingElement p_BuildingElement, 
            in Vector3 p_Position, 
            in Vector3 p_LocalEulerAngles, 
            in ABS_Building p_ParentBuilding)
        {
            bool res = true;
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                res &= tracker.BeforeHistoryPlace(p_BuildingElement, p_Position, p_LocalEulerAngles, p_ParentBuilding);
            }
            return res;
        }

        public override void BuildingElementHistoryPlaced(List<ABS_BuildingElement> p_PlacedBuildingElements)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingElementHistoryPlaced(p_PlacedBuildingElements);
            }
        }

        public override void BuildingWillBeHistoryDestroyed(in ABS_Building p_Building)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingWillBeHistoryDestroyed(p_Building);
            }
        }

        public override void BuildingHistoryPlaced(in ABS_Building p_Building) 
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.BuildingHistoryPlaced(p_Building);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //Input

        public override void ChangeModePressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ChangeModePressed();
            }
        }

        public override void ForcedFallbackPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.ForcedFallbackPressed();
            }
        }
        public override void SimpleBuildingPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.SimpleBuildingPressed();
            }
        }
        public override void DragBuildingPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DragBuildingPressed();
            }
        }
        public override void DragBuildingReleased()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DragBuildingReleased();
            }
        }

        public override void MouseWheelChanged(in float p_Value)
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.MouseWheelChanged(p_Value);
            }
        }

        public override void RotationButtonRightPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RotationButtonRightPressed();
            }
        }
        public override void RotationButtonRightHold()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RotationButtonRightHold();
            }
        }
        public override void RotationButtonRightReleased()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RotationButtonRightReleased();
            }
        }
        public override void RotationButtonLeftPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RotationButtonLeftPressed();
            }
        }
        public override void RotationButtonLeftHold()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RotationButtonLeftHold();
            }
        }
        public override void RotationButtonLeftReleased()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RotationButtonLeftReleased();
            }
        }


        public override void AlignRotationToGroundPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.AlignRotationToGroundPressed();
            }
        }
        public override void AlignRotationToGroundReleased()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.AlignRotationToGroundReleased();
            }
        }
        public override void AlignRotationToBuildingElementsPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.AlignRotationToBuildingElementsPressed();
            }
        }

        public override void UndoPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.UndoPressed();
            }
        }
        public override void RedoPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.RedoPressed();
            }
        }

        public override void SimpleDestroyPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.SimpleDestroyPressed();
            }
        }
        public override void SimpleDestroyHold()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.SimpleDestroyHold();
            }
        }
        public override void SimpleDestroyReleased()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.SimpleDestroyReleased();
            }
        }
        public override void DragDestroyPressed()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DragDestroyPressed();
            }
        }
        public override void DragDestroyReleased()
        {
            foreach (ABS_BuildingManagerTracker tracker in m_Trackers)
            {
                if (tracker == null)
                {
                    REST_Logging.Warrning($"{this}", "The tracker object is a nullptr");
                    continue;
                }
                tracker.DragDestroyReleased();
            }
}
    }
}
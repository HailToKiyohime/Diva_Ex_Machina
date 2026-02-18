//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_InputManager : 
        ABS_BuildingManagerComponentBaseMonoBehaviour,
        ABS_IBuildingManagerInputActionInterface
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_IInputTracker m_InputTracker = null;
        private ABS_IInputTracker m_ManagerInputTracker = null;

        //Change BuildingMode
        private bool m_Press_ChangeMode = false;

        //BuildingProcess
        private bool m_Press_ForcedFallback = false;
        private bool m_Press_SimpleBuilding = false;
        private bool m_Press_DragBuilding = false;
        private bool m_Release_DragBuilding = false;

        private bool m_Press_SimpleDestroy = false;
        private bool m_Hold_SimpleDestroy = false;
        private bool m_Release_SimpleDestroy = false;
        private bool m_Press_DragDestroy = false;
        private bool m_Release_DragDestroy = false;

        //Rotation
        private bool m_Press_RotationRight = false;
        private bool m_Hold_RotationRight = false;
        private bool m_Release_RotationRight = false;
        private bool m_Press_RotationLeft = false;
        private bool m_Hold_RotationLeft = false;
        private bool m_Release_RotationLeft = false;

        //RotationAlignment
        private bool m_Press_AlignRotationToGround = false;
        private bool m_Release_AlignRotationToGround = false;
        private bool m_Press_AlignRotationToBuildingElements = false;

        //History
        private bool m_Press_Undo = false;
        private bool m_Press_Redo = false;

        private ABS_InputType m_InputType = ABS_InputType.Key;
        private ABS_RotationInputType m_RotationInputType = ABS_RotationInputType.MouseWheel;

        //Key Input settings
        private KeyCode m_KeyForRotationRight = 0;
        private KeyCode m_KeyForRotationLeft = 0;

        private KeyCode m_KeyForForcedFallback = 0;
        private KeyCode m_KeyForBuild = 0;
        private KeyCode m_KeyForDestroy = 0;
        private KeyCode m_KeyForDragBuild = 0;
        private KeyCode m_KeyForDragDestroy = 0;

        private KeyCode m_KeyForModeChange = 0;

        private KeyCode m_KeyForUndo = 0;
        private KeyCode m_KeyForRedo = 0;

        private KeyCode m_KeyForAlignRotationToGround = 0;
        private KeyCode m_KeyForAlignRotationToBuildingElements = 0;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_InputType InputType
        {
            get { return m_InputType; }
            set 
            {
                ClearData();
                m_InputType = value; 
            } 
        }
        public ABS_RotationInputType RotationInputType
        {
            get { return m_RotationInputType; }
            set { m_RotationInputType = value; }
        }

        public KeyCode KeyForRotationRight
        {
            get { return m_KeyForRotationRight; }
            set { m_KeyForRotationRight = value; }
        }
        public KeyCode KeyForRotationLeft
        {
            get { return m_KeyForRotationLeft; }
            set { m_KeyForRotationLeft = value; }
        }

        public KeyCode KeyForBuild
        {
            get { return m_KeyForBuild; }
            set { m_KeyForBuild = value; }
        }
        public KeyCode KeyForDestroy
        {
            get { return m_KeyForDestroy; }
            set { m_KeyForDestroy = value; }
        }
        public KeyCode KeyForDragBuild
        {
            get { return m_KeyForDragBuild; }
            set { m_KeyForDragBuild = value; }
        }
        public KeyCode KeyForDragDestroy
        {
            get { return m_KeyForDragDestroy; }
            set { m_KeyForDragDestroy = value; }
        }
        public KeyCode KeyForUndo
        {
            get { return m_KeyForUndo; }
            set { m_KeyForUndo = value; }
        }
        public KeyCode KeyForRedo
        {
            get { return m_KeyForRedo; }
            set { m_KeyForRedo = value; }
        }
        public KeyCode KeyForModeChange
        {
            get { return m_KeyForModeChange; }
            set { m_KeyForModeChange = value; }
        }
        public KeyCode KeyForForcedFallback
        {
            get { return m_KeyForForcedFallback; }
            set { m_KeyForForcedFallback = value; }
        }
        public KeyCode KeyForAlignRotationToGround
        {
            get { return m_KeyForAlignRotationToGround; }
            set { m_KeyForAlignRotationToGround = value; }
        }
        public KeyCode KeyForAlignRotationToBuildingElements
        {
            get { return m_KeyForAlignRotationToBuildingElements; }
            set { m_KeyForAlignRotationToBuildingElements = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override void Init(
            ABS_IBuildingManagerInternalInterface p_Manager, 
            ABS_BuildingManagerTracker p_Tracker)
        {
            base.Init(p_Manager, p_Tracker);
            m_InputTracker = p_Tracker;
            m_ManagerInputTracker = p_Manager as ABS_IInputTracker;

            m_InputType = p_Manager.InputType;
            m_RotationInputType = p_Manager.RotationInputType;

            m_KeyForForcedFallback = p_Manager.KeyForForcedFallback;
            m_KeyForBuild = p_Manager.KeyForBuild;
            m_KeyForDestroy = p_Manager.KeyForDestroy;
            m_KeyForDragBuild = p_Manager.KeyForDragBuild;
            m_KeyForDragDestroy = p_Manager.KeyForDragDestroy;
            m_KeyForModeChange = p_Manager.KeyForModeChange;
            m_KeyForUndo = p_Manager.KeyForUndo;
            m_KeyForRedo = p_Manager.KeyForRedo;
            m_KeyForRotationRight = p_Manager.KeyForRotationRight;
            m_KeyForRotationLeft = p_Manager.KeyForRotationLeft;
            m_KeyForAlignRotationToGround = p_Manager.KeyForAlignRotationToGround;
            m_KeyForAlignRotationToBuildingElements = p_Manager.KeyForAlignRotationToBuildingElements;
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IBuildingManagerInputActionInterface Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //Change BuildingMode
        public void OnChangeModePressed() { m_Press_ChangeMode = true; }
        public void OnChangeModePressedImmediate() { if(CheckMSGType()) ChangeModePressed(); }

        //BuildingProcess
        public void OnForcedFallbackPressed() { m_Press_ForcedFallback = true; }
        public void OnForcedFallbackPressedImmediate() { if(CheckMSGType()) ForcedFallbackPressed(); }

        public void OnSimpleBuildingPressed() { m_Press_SimpleBuilding = true; }
        public void OnSimpleBuildingPressedImmediate() { if(CheckMSGType()) SimpleBuildingPressed(); }
        public void OnDragBuildingPressed() { m_Press_DragBuilding = true; }
        public void OnDragBuildingPressedImmediate() { if(CheckMSGType()) DragBuildingPressed(); }
        public void OnDragBuildingReleased() { m_Release_DragBuilding = true; }
        public void OnDragBuildingReleasedImmediate() { if(CheckMSGType()) DragBuildingReleased(); }

        public void OnSimpleDestroyPressed() { m_Press_SimpleDestroy = true; }
        public void OnSimpleDestroyPressedImmediate() { if(CheckMSGType()) SimpleDestroyPressed(); }
        public void OnSimpleDestroyReleased() { m_Release_SimpleDestroy = true; }
        public void OnSimpleDestroyReleasedImmediate() { if(CheckMSGType()) SimpleDestroyReleased(); }
        public void OnDragDestroyPressed() { m_Press_DragDestroy = true; }
        public void OnDragDestroyPressedImmediate() { if(CheckMSGType()) DragDestroyPressed(); }
        public void OnDragDestroyReleased() { m_Release_DragDestroy = true; }
        public void OnDragDestroyReleasedImmediate() { if(CheckMSGType()) DragDestroyReleased(); }

        //Rotation

        public void OnRotationButtonRightPressed() { m_Press_RotationRight = true; }
        public void OnRotationButtonRightPressedImmediate() { if(CheckMSGType()) RotationButtonRightPressed(); }
        public void OnRotationButtonRightHold() { m_Hold_RotationRight = true; }
        public void OnRotationButtonRightHoldImmediate() { if(CheckMSGType()) RotationButtonRightHold(); }
        public void OnRotationButtonRightReleased() { m_Release_RotationRight = true; }
        public void OnRotationButtonRightReleasedImmediate() { if(CheckMSGType()) RotationButtonRightReleased(); }

        public void OnRotationButtonLeftPressed() { m_Press_RotationLeft = true; }
        public void OnRotationButtonLeftPressedImmediate() { if(CheckMSGType()) RotationButtonLeftPressed(); }
        public void OnRotationButtonLeftHold() { m_Hold_RotationLeft = true; }
        public void OnRotationButtonLeftHoldImmediate() { if(CheckMSGType()) RotationButtonLeftHold(); }
        public void OnRotationButtonLeftReleased() { m_Release_RotationLeft = true; }
        public void OnRotationButtonLeftReleasedImmediate() { if(CheckMSGType()) RotationButtonLeftReleased(); }

        //RotationAlignment
        public void OnAlignRotationToGroundPressed() { m_Press_AlignRotationToGround = true; }
        public void OnAlignRotationToGroundPressedImmediate() { if(CheckMSGType()) AlignRotationToGroundPressed(); }
        public void OnAlignRotationToGroundReleased() { m_Release_AlignRotationToGround = true; }
        public void OnAlignRotationToGroundReleasedImmediate() { if(CheckMSGType()) AlignRotationToGroundReleased(); }
        public void OnAlignRotationToBuildingElementsPressed() { m_Press_AlignRotationToBuildingElements = true; }
        public void OnAlignRotationToBuildingElementsPressedImmediate() { if(CheckMSGType()) AlignRotationToBuildingElementsPressed(); }

        //History
        public void OnUndoPressed() { m_Press_Undo = true; }
        public void OnUndoPressedImmediate() { if(CheckMSGType()) UndoPressed(); }
        public void OnRedoPressed() { m_Press_Redo = true; }
        public void OnRedoPressedImmediate() { if(CheckMSGType()) RedoPressed();  }

        public void ClearData()
        {
            m_Press_ChangeMode = false;

            m_Press_ForcedFallback = false;

            m_Press_SimpleBuilding = false;
            m_Press_DragBuilding = false;
            m_Release_DragBuilding = false;

            m_Press_SimpleDestroy = false;
            m_Release_SimpleDestroy = false;
            m_Press_DragDestroy = false;
            m_Hold_SimpleDestroy = false;
            m_Release_DragDestroy = false;

            m_Press_RotationRight = false;
            m_Hold_RotationRight = false;
            m_Release_RotationRight = false;
            m_Press_RotationLeft = false;
            m_Hold_RotationLeft = false;
            m_Release_RotationLeft = false;
            m_Press_AlignRotationToGround = false;
            m_Press_AlignRotationToBuildingElements = false;

            m_Press_Undo = false;
            m_Press_Redo = false;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Update()
        {
            //Processing Order
            //  History
            //  Change BuildingMode
            //  BuildingProcess
            //  RotationAlignment

            //Processing Order In functions
            //  UndoPressed
            //  RedoPressed
            //
            //  ChangeModePressed
            //
            //  ForcedFallbackPressed
            //  SimpleBuildingPressed
            //  DragBuildingPressed
            //  DragBuildingReleased
            //
            //  AlignRotationToGroundPressed
            //  AlignRotationToBuildingElementsPressed

            if (m_InputType == ABS_InputType.Messages
                || m_InputType == ABS_InputType.Both)
            {
                if (m_Press_Undo)
                {
                    UndoPressed();
                    m_Press_Undo = false;
                }

                if (m_Press_Redo)
                {
                    UndoPressed();
                    m_Press_Redo = false;
                }

                if (m_Press_ChangeMode)
                {
                    ChangeModePressed();
                    m_Press_ChangeMode = false;
                }
               
                if (m_Press_ForcedFallback)
                {
                    ForcedFallbackPressed();
                    m_Press_ForcedFallback = false;
                }

                if (m_Press_SimpleBuilding)
                {
                    SimpleBuildingPressed();
                    m_Press_SimpleBuilding = false;
                }

                if (m_Press_DragBuilding)
                {
                    DragBuildingPressed();
                    m_Press_DragBuilding = false;
                }

                if (m_Release_DragBuilding)
                {
                    DragBuildingReleased();
                    m_Release_DragBuilding = false;
                }

                if (m_Press_SimpleDestroy)
                {
                    SimpleDestroyPressed();
                    m_Press_SimpleDestroy = false;
                }

                if (m_Release_SimpleDestroy)
                {
                    SimpleDestroyReleased();
                    m_Release_SimpleDestroy = false;
                }

                if (m_Press_DragDestroy)
                {
                    DragDestroyPressed();
                    m_Press_DragDestroy = false;
                }

                if (m_Release_DragDestroy)
                {
                    DragDestroyReleased();
                    m_Release_DragDestroy = false;
                }

                if (m_Press_AlignRotationToGround)
                {
                    AlignRotationToGroundPressed();
                    m_Press_AlignRotationToGround = false;
                }
                
                if (m_Release_AlignRotationToGround)
                {
                    AlignRotationToGroundReleased();
                    m_Release_AlignRotationToGround = false;
                }

                if (m_Press_AlignRotationToBuildingElements)
                {
                    AlignRotationToBuildingElementsPressed();
                    m_Press_AlignRotationToBuildingElements = false;
                }

                if (m_Press_RotationRight)
                {
                    RotationButtonRightPressed();
                    m_Press_RotationRight = false;
                }

                if (m_Hold_RotationRight)
                {
                    RotationButtonRightHold();
                    m_Hold_RotationRight = false;
                }
                
                if (m_Release_RotationRight)
                {
                    RotationButtonRightReleased();
                    m_Release_RotationRight = false;
                }

                if (m_Press_RotationLeft)
                {
                    RotationButtonLeftPressed();
                    m_Press_RotationLeft = false;
                }

                if (m_Hold_RotationLeft)
                {
                    RotationButtonLeftHold();
                    m_Hold_RotationLeft = false;
                }

                if (m_Release_RotationLeft)
                {
                    RotationButtonLeftReleased();
                    m_Release_RotationLeft = false;
                }
            }

            if (m_InputType == ABS_InputType.Key
                || m_InputType == ABS_InputType.Both)
            {
                if (IsKeyPressed(m_KeyForUndo))
                {
                    UndoPressed();
                }

                if (IsKeyPressed(m_KeyForRedo))
                {
                    RedoPressed();
                }

                if (IsKeyPressed(m_KeyForModeChange))
                {
                    ChangeModePressed();
                }

                if (IsKeyPressed(m_KeyForForcedFallback))
                {
                    ForcedFallbackPressed();
                }

                if (IsKeyPressed(m_KeyForBuild))
                {
                    SimpleBuildingPressed();
                }

                if (IsKeyPressed(m_KeyForDragBuild))
                {
                    DragBuildingPressed();
                }

                if (IsKeyReleased(m_KeyForDragBuild))
                {
                    DragBuildingReleased();
                }

                if (IsKeyPressed(m_KeyForDestroy))
                {
                    SimpleDestroyPressed();
                }

                if (IsKeyReleased(m_KeyForDestroy))
                {
                    SimpleDestroyReleased();
                }

                if (IsKeyPressed(m_KeyForDragDestroy))
                {
                    DragDestroyPressed();
                }
                else if (IsKeyReleased(m_KeyForDragDestroy))
                {
                    DragDestroyReleased();
                }

                if (IsKeyPressed(m_KeyForAlignRotationToGround))
                {
                    AlignRotationToGroundPressed();
                }
                else if (IsKeyReleased(m_KeyForAlignRotationToGround))
                {
                    AlignRotationToGroundReleased();
                }

                if (IsKeyPressed(m_KeyForAlignRotationToBuildingElements))
                {
                    AlignRotationToBuildingElementsPressed();
                }

                if (m_RotationInputType == ABS_RotationInputType.MouseWheel)
                {
                    MouseWheelChanged(GetMouseWheelChange());
                }
                else
                {
                    if (IsKeyPressed(m_KeyForRotationRight))
                    {
                        RotationButtonRightPressed();
                    }
                    else if (IsKeyHold(m_KeyForRotationRight))
                    {
                        RotationButtonRightHold();
                    }
                    else if (IsKeyReleased(m_KeyForRotationRight))
                    {
                        RotationButtonRightReleased();
                    }

                    if (IsKeyPressed(m_KeyForRotationLeft))
                    {
                        RotationButtonLeftPressed();
                    }
                    else if (IsKeyHold(m_KeyForRotationLeft))
                    {
                        RotationButtonLeftHold();
                    }

                    if (IsKeyReleased(m_KeyForRotationLeft))
                    {
                        RotationButtonLeftReleased();
                    }
                }
            }

            if (m_Hold_SimpleDestroy)
            {
                SimpleDestroyHold();
            }
        }

        private bool CheckMSGType ()
        {
            return m_InputType == ABS_InputType.Messages
                || m_InputType == ABS_InputType.Both;
        }

        private void ChangeModePressed()
        {
            m_ManagerInputTracker.ChangeModePressed();
            m_InputTracker.ChangeModePressed();
        }

        private void ForcedFallbackPressed()
        {
            m_ManagerInputTracker.ForcedFallbackPressed();
            m_InputTracker.ForcedFallbackPressed();
        }

        private void SimpleBuildingPressed()
        {
            m_ManagerInputTracker.SimpleBuildingPressed();
            m_InputTracker.SimpleBuildingPressed();
        }

        private void DragBuildingPressed()
        {
            m_ManagerInputTracker.DragBuildingPressed();
            m_InputTracker.DragBuildingPressed();
        }

        private void DragBuildingReleased()
        {
            m_ManagerInputTracker.DragBuildingReleased();
            m_InputTracker.DragBuildingReleased();
        }

        private void MouseWheelChanged(in float p_Value)
        {
            m_ManagerInputTracker.MouseWheelChanged(p_Value);
            m_InputTracker.MouseWheelChanged(p_Value);
        }

        private void RotationButtonRightPressed()
        {
            m_Hold_RotationRight = true;
            m_ManagerInputTracker.RotationButtonRightPressed();
            m_InputTracker.RotationButtonRightPressed();
        }

        public void RotationButtonRightHold()
        {
            m_ManagerInputTracker.RotationButtonRightHold();
            m_InputTracker.RotationButtonRightHold();
        }
        public void RotationButtonRightReleased()
        {
            m_Hold_RotationLeft = false;
            m_ManagerInputTracker.RotationButtonRightReleased();
            m_InputTracker.RotationButtonRightReleased();
        }

        private void RotationButtonLeftPressed()
        {
            m_Hold_RotationLeft = true;
            m_ManagerInputTracker.RotationButtonLeftPressed();
            m_InputTracker.RotationButtonLeftPressed();
        }
        public void RotationButtonLeftHold()
        {
            m_ManagerInputTracker.RotationButtonLeftHold();
            m_InputTracker.RotationButtonLeftHold();
        }
        public void RotationButtonLeftReleased()
        {
            m_Hold_RotationLeft = false;
            m_ManagerInputTracker.RotationButtonLeftReleased();
            m_InputTracker.RotationButtonLeftReleased();
        }

        private void AlignRotationToGroundPressed()
        {
            m_ManagerInputTracker.AlignRotationToGroundPressed();
            m_InputTracker.AlignRotationToGroundPressed();
        }
        
        private void AlignRotationToGroundReleased()
        {
            m_ManagerInputTracker.AlignRotationToGroundReleased();
            m_InputTracker.AlignRotationToGroundReleased();
        }

        private void AlignRotationToBuildingElementsPressed()
        {
            m_ManagerInputTracker.AlignRotationToBuildingElementsPressed();
            m_InputTracker.AlignRotationToBuildingElementsPressed();
        }

        private void UndoPressed()
        {
            m_ManagerInputTracker.UndoPressed();
            m_InputTracker.UndoPressed();
        }

        private void RedoPressed()
        {
            m_ManagerInputTracker.RedoPressed();
            m_InputTracker.RedoPressed();
        }

        private void SimpleDestroyPressed()
        {
            m_Hold_SimpleDestroy = true;
            m_ManagerInputTracker.SimpleDestroyPressed();
            m_InputTracker.SimpleDestroyPressed();
        }
        private void SimpleDestroyHold()
        {
            m_ManagerInputTracker.SimpleDestroyHold();
            m_InputTracker.SimpleDestroyHold();
        }
        private void SimpleDestroyReleased()
        {
            m_Hold_SimpleDestroy = false;
            m_ManagerInputTracker.SimpleDestroyReleased();
            m_InputTracker.SimpleDestroyReleased();
        }
        private void DragDestroyPressed()
        {
            m_ManagerInputTracker.DragDestroyPressed();
            m_InputTracker.DragDestroyPressed();
        }
        private void DragDestroyReleased()
        {
            m_ManagerInputTracker.DragDestroyReleased();
            m_InputTracker.DragDestroyReleased();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Legacy Key Utility Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool IsKeyHold(in KeyCode p_Key)
        {
            return Input.GetKey(p_Key);
        }
        private bool IsKeyReleased(in KeyCode p_Key)
        {
            return Input.GetKeyUp(p_Key);
        }
        private bool IsKeyPressed(in KeyCode p_Key)
        {
            return Input.GetKeyDown(p_Key);
        }
        private float GetMouseWheelChange()
        {
            return Input.GetAxis("Mouse ScrollWheel");
        }
    }
}
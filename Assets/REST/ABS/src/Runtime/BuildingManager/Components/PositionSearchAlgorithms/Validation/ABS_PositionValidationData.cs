//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    public class ABS_PositionValidationData
    {
        public ABS_PositionValidationResult m_Result = new ABS_PositionValidationResult();

        public bool m_PositionWasAlignedToGround = false;
        public ABS_BuildingElement m_ElementTarget_BuildOnTopOfElement = null;
        public ABS_BuildingElement m_ElementTarget_Override = null;

        public ABS_PositionValidationData() { }

        public ABS_PositionValidationData(ABS_PositionValidationData p_CopyTarget)
        {
            m_Result = p_CopyTarget.m_Result;
            m_PositionWasAlignedToGround = p_CopyTarget.m_PositionWasAlignedToGround;
            m_ElementTarget_BuildOnTopOfElement = p_CopyTarget.m_ElementTarget_BuildOnTopOfElement;
            m_ElementTarget_Override = p_CopyTarget.m_ElementTarget_Override;
        }

        public bool IsSuccessFull()
        {
            return m_Result.IsSuccessFull();
        }

        public bool IsFailed()
        {
            return m_Result.IsFailed();
        }

        public bool IsSuccessFull(in bool p_IgnoreCollisionCheck = false,
                                  in bool p_IgnoreElementCollisionCheck = false,
                                  in bool p_IgnoreBuildableGroundCheck = false,
                                  in bool p_IgnorePositionRules = false,
                                  in bool p_IgnoreShouldSnapToFoundation = false,
                                  in bool p_IgnoreGroundedCheck = false,
                                  in bool p_IgnoreStabiltyCheck = false)
        {
            return m_Result.IsSuccessFull(
                p_IgnoreCollisionCheck,
                p_IgnoreElementCollisionCheck,
                p_IgnoreBuildableGroundCheck,
                p_IgnorePositionRules,
                p_IgnoreShouldSnapToFoundation,
                p_IgnoreGroundedCheck,
                p_IgnoreStabiltyCheck);
        }

        public override string ToString()
        {
            return $"ElementTarget BuildOnTopOfElement : {(m_ElementTarget_BuildOnTopOfElement == null ? REST_Logging.s_Literal_Null : m_ElementTarget_BuildOnTopOfElement.name)}" +
                    $"\nElementTarget Override : {(m_ElementTarget_Override == null ? REST_Logging.s_Literal_Null : m_ElementTarget_Override.name)}" +
                    $"\nPosition Was Aligned To Ground : {REST_Logging.ColorizeBlooean(m_PositionWasAlignedToGround)}" +
                    "\n**********************************************************************" +
                    $"\nPositionValidationResult : \n\n{m_Result}";
        }

        public static ABS_PositionValidationData PositionValidationDataFactory(ABS_BuildingElement m_Element)
        {
            switch(m_Element.PositionSearchAlgorithm)
            {
                case ABS_PositionSearchAlgorithm.AdvancedGrid: return new ABS_PositionValidationData_AdvancedGrid();
                case ABS_PositionSearchAlgorithm.Free:
                case ABS_PositionSearchAlgorithm.BasicGrid:
                case ABS_PositionSearchAlgorithm.SnapPointBased:
                default: return new ABS_PositionValidationData();
            }
        }
    }

    public class ABS_PositionValidationData_AdvancedGrid : ABS_PositionValidationData
    {
        public enum DragBuildingTemporaryState : ushort
        {
            Checked,
            CheckNeeded
        }

        public bool m_Stable = false;
        public short m_Stability = -1;
        public short m_DragBuildingTemporaryStability = -1;
        public DragBuildingTemporaryState m_DragBuildingTemporaryState = DragBuildingTemporaryState.CheckNeeded;

        public ABS_PositionValidationData_AdvancedGrid() : base() { }

        public ABS_PositionValidationData_AdvancedGrid(ABS_PositionValidationData p_CopyTarget) : base(p_CopyTarget) { }
        public ABS_PositionValidationData_AdvancedGrid(ABS_PositionValidationData_AdvancedGrid p_CopyTarget) : base(p_CopyTarget) 
        {
            this.m_Stability = p_CopyTarget.m_Stability;
            this.m_DragBuildingTemporaryStability = p_CopyTarget.m_DragBuildingTemporaryStability;
            this.m_DragBuildingTemporaryState = p_CopyTarget.m_DragBuildingTemporaryState;
        }

        public override string ToString()
        {
            string checkstate = REST_Logging.ColorizeString(
                m_DragBuildingTemporaryState.ToString(),
                m_DragBuildingTemporaryState == ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.Checked
                 ? REST_Logging.Colors.Green : REST_Logging.Colors.Red);

            return $"AdvancedGrid Validation Data \n" +
                $"\n m_Stable : {REST_Logging.ColorizeBlooean(m_Stable)}" +
                $"\n m_Stability : {REST_Logging.ColorizeNumberHigherThanZero(m_Stability)}" +
                $"\n m_DragBuildingTemporaryStability : {REST_Logging.ColorizeNumberHigherThanZero(m_DragBuildingTemporaryStability)}" +
                $"\n m_DragBuildingTemporaryState : {checkstate}" +
                "\n**********************************************************************" +
                "\n" + base.ToString();
        }
    }
}
//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_DragBuildingAdvancedGridHelper
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BuildingElement m_ActiveBuildingElement = null;

        private bool m_IsStabilityFeatureActive = false; 

        private List<List<ABS_TemporaryBuildingElement>> m_TemporaryElements = null;

        private bool m_AllowMixedAxisDragBuilding = false;
        private bool m_RotationShouldBeChecked = false;
        private int m_AxisModifier = 0;

        private bool m_CanValidate_XPos = true;
        private bool m_CanValidate_XNeg = true;
        private bool m_CanValidate_ZPos = true;
        private bool m_CanValidate_ZNeg = true;
        private bool m_CanValidate_XPosZPos = true;
        private bool m_CanValidate_XPosZNeg = true;
        private bool m_CanValidate_XNegZPos = true;
        private bool m_CanValidate_XNegZNeg = true;

#if UNITY_EDITOR
        private ABS_ProjectSettings m_ProjectSettings = null;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters / Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElement ActiveBuildingElement
        {
            get { return m_ActiveBuildingElement; }
            set
            {
#if UNITY_EDITOR
                if (m_ProjectSettings == null)
                {
                    m_ProjectSettings = ABS_ProjectSettingsGetter.GetSettings();
                }
#endif

                m_ActiveBuildingElement = value;
                if (m_ActiveBuildingElement == null)
                {
                    return;
                }

                m_IsStabilityFeatureActive = false;

                m_RotationShouldBeChecked = m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.Wall;
                m_RotationShouldBeChecked |= m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.EdgeHorizontal;
                m_AllowMixedAxisDragBuilding  = m_RotationShouldBeChecked && m_ActiveBuildingElement.AllowMixedAxisDragBuilding;
                m_AxisModifier = m_AllowMixedAxisDragBuilding ? 1 : 0;

                if (m_ActiveBuildingElement.SnapPointRuleSet == null)
                {
                    m_CanValidate_XPos = true;
                    m_CanValidate_XNeg = true;
                    m_CanValidate_ZPos = true;
                    m_CanValidate_ZNeg = true;
                    m_CanValidate_XPosZPos = true;
                    m_CanValidate_XPosZNeg = true;
                    m_CanValidate_XNegZPos = true;
                    m_CanValidate_XNegZNeg = true;
                }
                else
                {
                    List<ABS_AdvancedGridSnapPointRule> rules = m_ActiveBuildingElement.SnapPointRuleSet.Rules;
                    List<ABS_AdvancedGridSnapPointRule.SnapPoint> snappoints = rules[(int)m_ActiveBuildingElement.AdvancedGridType].SnapPoints;
                    switch (m_ActiveBuildingElement.AdvancedGridType)
                    {
                        case ABS_AdvancedGridType.Floor:
                        case ABS_AdvancedGridType.Center:
                            {
                                m_CanValidate_XPos = snappoints[0].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNeg = snappoints[1].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_ZPos = snappoints[2].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_ZNeg = snappoints[3].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XPosZPos = false;
                                m_CanValidate_XPosZNeg = false;
                                m_CanValidate_XNegZPos = false;
                                m_CanValidate_XNegZNeg = false;
                            } break;
                        case ABS_AdvancedGridType.Wall:
                            {
                                m_CanValidate_XPos = snappoints[2].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNeg = snappoints[3].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_ZPos = snappoints[2].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_ZNeg = snappoints[3].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XPosZPos = snappoints[5].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XPosZNeg = snappoints[4].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNegZPos = snappoints[7].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNegZNeg = snappoints[6].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                            }
                            break;
                        case ABS_AdvancedGridType.EdgeHorizontal:
                            {
                                m_CanValidate_XPos = snappoints[0].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNeg = snappoints[1].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_ZPos = snappoints[0].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_ZNeg = snappoints[1].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XPosZPos = snappoints[2].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XPosZNeg = snappoints[4].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNegZPos = snappoints[3].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                                m_CanValidate_XNegZNeg = snappoints[5].m_Permisson == ABS_AdvancedGridSnapPointRule.PermissionType.Allow;
                            }
                            break;
                        case ABS_AdvancedGridType.Corner:
                        case ABS_AdvancedGridType.EdgeVertical:
                        default:
                            m_CanValidate_XPos = false;
                            m_CanValidate_XNeg = false;
                            m_CanValidate_ZPos = false;
                            m_CanValidate_ZNeg = false;
                            m_CanValidate_XPosZPos = false;
                            m_CanValidate_XPosZNeg = false;
                            m_CanValidate_XNegZPos = false;
                            m_CanValidate_XNegZNeg = false;
                            break;
                    }
                }
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_DragBuildingAdvancedGridHelper (List<List<ABS_TemporaryBuildingElement>> p_TemporaryElements)
        {
            m_TemporaryElements = p_TemporaryElements;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  public implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //The goal of this fucntion is to validate those temporary elements what has valid position
        //but did not snap to any already built element
        //So when the drag building is finalized the element wouold have a valid snap point
        //so it should be able to place at once with it's future valid neighbours
        public void ParentNeighbourValidation(in bool p_IsStabilityFeatureActive)
        {
            m_IsStabilityFeatureActive = p_IsStabilityFeatureActive;
            if (m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.EdgeVertical
                || m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.Corner)
            {
                return;
            }

#if UNITY_EDITOR
            //Separator between the frames to better readability for the log
            if (m_ProjectSettings && m_ProjectSettings.DragBuilding_AdvancedGridValidationProcess) 
                REST_Logging.Debug($"/////////////////////////////////////////////////");
#endif

            for (int x = 0; x < m_TemporaryElements.Count; ++x)
            {
                for (int z = 0; z < m_TemporaryElements[x].Count; ++z)
                {
                    ParentNeighbourValidationReq(x, z);
                }
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  private implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void ParentNeighbourValidationReq(in int p_X, in int p_Z)
        {
            ABS_TemporaryBuildingElement tmpElement = m_TemporaryElements[p_X][p_Z];
            if (tmpElement == null)
            {
                return;
            }

            ABS_PositionValidationData_AdvancedGrid currentElementValiationData = GetValidationData(p_X, p_Z);
            if (currentElementValiationData == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (m_ProjectSettings && m_ProjectSettings.DragBuilding_AdvancedGridValidationProcess)
            {
                string index = REST_Logging.ColorizeString($"({p_X};{p_Z})", REST_Logging.Colors.White);
                string validated = REST_Logging.ColorizeBlooean(currentElementValiationData.IsSuccessFull());
                string checkstate = REST_Logging.ColorizeString(
                    currentElementValiationData.m_DragBuildingTemporaryState.ToString(),
                    currentElementValiationData.m_DragBuildingTemporaryState == ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.Checked
                     ? REST_Logging.Colors.Green : REST_Logging.Colors.Red);
                string baseStability = REST_Logging.ColorizeNumberHigherThanZero(currentElementValiationData.m_Stability);
                string currentStability = REST_Logging.ColorizeNumberHigherThanZero(currentElementValiationData.m_DragBuildingTemporaryStability);

                REST_Logging.Debug($"Start validation with {index}" +
                    $" Validated : {validated}" +
                    $" Checked : {checkstate}" +
                    (m_IsStabilityFeatureActive ? $" Base Stability : {baseStability} Temporary Stability : {currentStability}" : ""));
            }
#endif

            ParentNeighbourValidationReq(currentElementValiationData, p_X, p_Z);
        }
        private void ParentNeighbourValidationReq(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        {
            if (p_CurrentElementValiationData == null
                || p_CurrentElementValiationData.m_DragBuildingTemporaryState == ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.Checked)
            {
                return;
            }

            p_CurrentElementValiationData.m_DragBuildingTemporaryState = ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.Checked;
            bool rotated = IsRotatedPosition(p_X, p_Z);
            if (CanValdiatedTheNeighbourWithCurrent(p_CurrentElementValiationData))
            {
                //In case of rotation should NOT checked, check:
                //  X + 1, Z
                //  X - 1, Z
                //  X, Z + 1
                //  X, Z - 1
                //In case of rotated element check:
                //  X, Z + 1
                //  X, Z - 1
                //In case of NOT rotated element check:
                //  X + 1, Z
                //  X - 1, Z
                if (!m_RotationShouldBeChecked || !rotated)
                {
                    ValidateNeighbourWithCurrent_XAxis(p_CurrentElementValiationData, p_X, p_Z);
                }
                
                if (!m_RotationShouldBeChecked || rotated)
                {
                    ValidateNeighbourWithCurrent_ZAxis(p_CurrentElementValiationData, p_X, p_Z);
                }

                //All of the following checks are only valid in case of m_AllowMixedAxisDragBuilding is true
                // X + 1, Z + 1
                // X + 1, Z - 1
                // X - 1, Z + 1
                // X - 1, Z - 1
                if (m_AllowMixedAxisDragBuilding)
                {
                    ValidateNeighbourWithCurrent_MixedAxis(p_CurrentElementValiationData, p_X, p_Z);
                }
            }
            else if (CanValdiatedByNeighbour(p_CurrentElementValiationData.m_Result))
            {
                //TODO check all neighbour for stability even if any of the neighbour validated the element

                //In case of rotation should NOT checked, check:
                //  X + 1, Z
                //  X - 1, Z
                //  X, Z + 1
                //  X, Z - 1
                //In case of rotated element check:
                //  X, Z + 1
                //  X, Z - 1
                //In case of NOT rotated element check:
                //  X + 1, Z
                //  X - 1, Z
                if (!m_RotationShouldBeChecked || !rotated)
                {
                    if (ValidateCurrentWithNeighbour_XAxis(p_CurrentElementValiationData, p_X, p_Z))
                    {
                        return;
                    }
                }
                
                if (!m_RotationShouldBeChecked || rotated)
                {
                    if (ValidateCurrentWithNeighbour_ZAxis(p_CurrentElementValiationData, p_X, p_Z))
                    {
                        return;
                    }
                }

                //After this every check is only valid in case of m_AllowMixedAxisDragBuilding is true
                // X + 1, Z + 1
                // X + 1, Z - 1
                // X - 1, Z + 1
                // X - 1, Z - 1
                if (m_AllowMixedAxisDragBuilding)
                {
                    if (ValidateCurrentWithNeighbour_MixedAxis(p_CurrentElementValiationData, p_X, p_Z))
                    {
                        return;
                    }
                }
            }
        }

        private bool CanValdiatedTheNeighbourWithCurrent(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData)
        {
            if (p_CurrentElementValiationData.IsFailed ())
            {
                return false;
            }

            if (m_IsStabilityFeatureActive)
            {
                //No element can be built with zero.
                //If this is the neighbour with the highest stability to the it's neighbour element
                //then the neighbour of this elemnt will get it's stability minus 1
                //So the minimum stability is 2 for giving the minimum 1 stability to our neighbour,
                return p_CurrentElementValiationData.m_DragBuildingTemporaryStability >= 2;
            }
            return true;
        }

        private bool CanValdiatedByNeighbour(ABS_PositionValidationResult p_CheckedNeighbourValidation)
        {
            return p_CheckedNeighbourValidation.DragBuildingValidation_ValidatedByNeighbour == ABS_PositionValidationResult.ResultOptions.Unkown
                    && (p_CheckedNeighbourValidation.ParentBuildingValidation_InvalidPosition == ABS_PositionValidationResult.ResultOptions.Failed
                        || p_CheckedNeighbourValidation.ParentBuildingValidation_BreakPositionRules == ABS_PositionValidationResult.ResultOptions.Failed);
        }

        private void ValidateNeighbourWithCurrent_XAxis(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        {
            int modified_X;

            //Check X + 1, Z
            modified_X = p_X + 1 + m_AxisModifier;
            if (m_TemporaryElements.Count > modified_X && m_TemporaryElements[modified_X].Count > p_Z)
            {
                ValidateNeighbourWithCurrent(
                    p_CurrentElementValiationData,
                    p_X,
                    p_Z,
                    m_CanValidate_XNegZNeg,
                    m_CanValidate_XPosZPos,
                    modified_X,
                    p_Z);
            }

            //Check X - 1, Z
            modified_X = p_X - 1 - m_AxisModifier;
            if (p_X > m_AxisModifier)
            {
                ValidateNeighbourWithCurrent(
                    p_CurrentElementValiationData,
                    p_X,
                    p_Z,
                    m_CanValidate_XNegZNeg,
                    m_CanValidate_XPosZPos,
                    modified_X,
                    p_Z);
            }
        }

        private void ValidateNeighbourWithCurrent_ZAxis(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        {
            int modified_Z;

            //Check X, Z + 1
            modified_Z = p_Z + 1 + m_AxisModifier;
            if (m_TemporaryElements[p_X].Count > modified_Z)
            {
                ValidateNeighbourWithCurrent(
                    p_CurrentElementValiationData,
                    p_X,
                    p_Z,
                    m_CanValidate_XNegZNeg,
                    m_CanValidate_XPosZPos,
                    p_X,
                    modified_Z);
            }

            //Check X, Z - 1
            modified_Z = p_Z - 1 - m_AxisModifier;
            if (p_Z > m_AxisModifier)
            {
                ValidateNeighbourWithCurrent(
                    p_CurrentElementValiationData,
                    p_X,
                    p_Z,
                    m_CanValidate_XNegZNeg,
                    m_CanValidate_XPosZPos,
                    p_X,
                    modified_Z);
            }
        }

        private void ValidateNeighbourWithCurrent_MixedAxis(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        {
            int modified_X, modified_Z;

            // Check X + 1
            modified_X = p_X + 1;
            if (m_TemporaryElements.Count > modified_X)
            {
                // Check X + 1, Z + 1
                modified_Z = p_Z + 1;
                if (m_TemporaryElements[modified_X].Count > modified_Z)
                {
                    ValidateNeighbourWithCurrent(
                        p_CurrentElementValiationData,
                        p_X,
                        p_Z,
                        m_CanValidate_XNegZNeg,
                        m_CanValidate_XPosZPos,
                        modified_X,
                        modified_Z);
                }

                // Check X + 1, Z - 1
                modified_Z = p_Z - 1;
                if (p_Z > 0)
                {
                    ValidateNeighbourWithCurrent(
                        p_CurrentElementValiationData,
                        p_X,
                        p_Z,
                        m_CanValidate_XNegZNeg,
                        m_CanValidate_XPosZPos,
                        modified_X,
                        modified_Z);
                }
            }

            // Check X - 1
            modified_X = p_X - 1;
            if (p_X > 0)
            {
                // Check X - 1, Z + 1
                modified_Z = p_Z + 1;
                if (m_TemporaryElements[modified_X].Count > modified_Z)
                {
                    ValidateNeighbourWithCurrent(
                        p_CurrentElementValiationData,
                        p_X,
                        p_Z,
                        m_CanValidate_XNegZNeg,
                        m_CanValidate_XPosZPos,
                        modified_X,
                        modified_Z);
                }

                // Check X - 1, Z - 1
                modified_Z = p_Z - 1;
                if (p_Z > 0)
                {
                    ValidateNeighbourWithCurrent(
                        p_CurrentElementValiationData,
                        p_X,
                        p_Z,
                        m_CanValidate_XNegZNeg,
                        m_CanValidate_XPosZPos,
                        modified_X,
                        modified_Z);
                }
            }
        }

        private void ValidateNeighbourWithCurrent(
            in ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData,
            in int p_CurrentX,
            in int p_CurrentZ,
            in bool p_CanValdiate, 
            in bool p_CanValdiateInverse,
            in int p_NeighbourX, 
            in int p_NeighbourZ)
        {
            ABS_PositionValidationData_AdvancedGrid checkedNeighbourValidationData =
                checkedNeighbourValidationData = GetValidationData(p_NeighbourX, p_NeighbourZ);

            if (!CanValidateNeighbourWithCurrent(checkedNeighbourValidationData, p_CanValdiate, p_CanValdiateInverse))
            {
                return;
            }

            if (checkedNeighbourValidationData.m_Result.DragBuildingValidation_ValidatedByNeighbour == ABS_PositionValidationResult.ResultOptions.Validated
                || checkedNeighbourValidationData.m_DragBuildingTemporaryState == ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.Checked)
            {
                CheckStability(p_CurrentElementValiationData, p_CurrentX, p_CurrentZ, checkedNeighbourValidationData, p_NeighbourX, p_NeighbourZ);
            }
            else if (checkedNeighbourValidationData.m_Result.DragBuildingValidation_BreakPositionRules != ABS_PositionValidationResult.ResultOptions.Failed
                        && (checkedNeighbourValidationData.m_Result.ParentBuildingValidation_InvalidPosition == ABS_PositionValidationResult.ResultOptions.Failed
                            || (m_IsStabilityFeatureActive 
                                    && checkedNeighbourValidationData.m_Result.SpecialElementValidation_Stability == ABS_PositionValidationResult.ResultOptions.Failed)))
            {

#if UNITY_EDITOR
                if (m_ProjectSettings && m_ProjectSettings.DragBuilding_AdvancedGridValidationProcess)
                {
                    string currentIndex = REST_Logging.ColorizeString($"({p_CurrentX};{p_CurrentZ})", REST_Logging.Colors.White);
                    string neighbourIndex = REST_Logging.ColorizeString($"({p_NeighbourX};{p_NeighbourZ})", REST_Logging.Colors.White);
                    string currentBaseStability = REST_Logging.ColorizeNumberHigherThanZero(p_CurrentElementValiationData.m_Stability);
                    string currentTempStability = REST_Logging.ColorizeNumberHigherThanZero(p_CurrentElementValiationData.m_DragBuildingTemporaryStability);
                    string neighbourBaseStability = REST_Logging.ColorizeNumberHigherThanZero(checkedNeighbourValidationData.m_Stability);
                    string neighbourTempStability = REST_Logging.ColorizeNumberHigherThanZero(checkedNeighbourValidationData.m_DragBuildingTemporaryStability);
                    REST_Logging.Debug(
                        $"Current validator : {currentIndex}" +
                        (m_IsStabilityFeatureActive ? $" Base Stability : {currentBaseStability} Temporary Stability : {currentTempStability}" : "")+
                        $" | Validated Neighbour {neighbourIndex}" +
                        (m_IsStabilityFeatureActive ? $" Base Stability : {neighbourBaseStability} Temporary Stability : {neighbourTempStability}" : "")+
                        $" ValidationData : \n\n{checkedNeighbourValidationData}");
                }
#endif

                //This CheckStabilityOfNeighbourWithCurrent can not be optimalised out because in this case
                //the ValidatedByNeighbour set is important before the check
                checkedNeighbourValidationData.m_Result.DragBuildingValidation_ValidatedByNeighbour = ABS_PositionValidationResult.ResultOptions.Validated;
                CheckStability(p_CurrentElementValiationData, p_CurrentX, p_CurrentZ, checkedNeighbourValidationData, p_NeighbourX, p_NeighbourZ);

                ParentNeighbourValidationReq(checkedNeighbourValidationData, p_NeighbourX, p_NeighbourZ);
            }
        }

        private bool CanValidateNeighbourWithCurrent(
            in ABS_PositionValidationData_AdvancedGrid p_CheckedNeighbourValidationData,
            in bool p_CanValdiate,
            in bool p_CanValdiateInverse)
        {
            if (p_CanValdiate && p_CanValdiateInverse)
            {
                return true;
            }
            else
            {
                if (p_CheckedNeighbourValidationData == null)
                {
                    return false;
                }

                ABS_PositionValidationResult checkedNeighbourResult = p_CheckedNeighbourValidationData.m_Result;
                if (checkedNeighbourResult != null)
                {
                    checkedNeighbourResult.DragBuildingValidation_BreakPositionRules = ABS_PositionValidationResult.ResultOptions.Failed;
                }

                return false;
            }
        }

        private void CheckStability(
            in ABS_PositionValidationData_AdvancedGrid p_Validator,
            in int p_ValidatorX,
            in int p_ValidatorZ,
            in ABS_PositionValidationData_AdvancedGrid p_Valiadted,
            in int p_ValiadtedX,
            in int p_ValiadtedZ)
        {
            if (!m_IsStabilityFeatureActive)
            {
                return;
            }

            if ((short)(p_Validator.m_DragBuildingTemporaryStability - 1) > p_Valiadted.m_DragBuildingTemporaryStability)
            {
                p_Valiadted.m_DragBuildingTemporaryStability = (short)(p_Validator.m_DragBuildingTemporaryStability - 1);
                p_Valiadted.m_DragBuildingTemporaryState = ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.CheckNeeded;
                //TODO Stability alignment for the neighbours

#if UNITY_EDITOR
                if (m_ProjectSettings && m_ProjectSettings.DragBuilding_AdvancedGridValidationProcess)
                {
                    string sourceIndex = REST_Logging.ColorizeString($"({p_ValidatorX};{p_ValidatorZ})", REST_Logging.Colors.White);
                    string sourceBaseStability = REST_Logging.ColorizeNumberHigherThanZero(p_Validator.m_Stability);
                    string sourceTempStability = REST_Logging.ColorizeNumberHigherThanZero(p_Validator.m_DragBuildingTemporaryStability);

                    string targetIndex = REST_Logging.ColorizeString($"({p_ValiadtedX};{p_ValiadtedZ})", REST_Logging.Colors.White);
                    string targetBaseStability = REST_Logging.ColorizeNumberHigherThanZero(p_Valiadted.m_Stability);
                    string stargetTempStability = REST_Logging.ColorizeNumberHigherThanZero(p_Valiadted.m_DragBuildingTemporaryStability);

                    REST_Logging.Debug($"Stability source element : {sourceIndex} Base Stability : {sourceBaseStability} Temporary Stability : {sourceTempStability}" +
                                    $" | Set Stability for Element {targetIndex} Base Stability : {targetBaseStability} Temporary Stability : {stargetTempStability}");
                }
#endif
            }
            else if (p_Validator.m_DragBuildingTemporaryStability < (short)(p_Valiadted.m_DragBuildingTemporaryStability - 1))
            {
                p_Validator.m_DragBuildingTemporaryStability = (short)(p_Valiadted.m_DragBuildingTemporaryStability - 1);
                p_Validator.m_DragBuildingTemporaryState = ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.CheckNeeded;
                //TODO Stability alignment for the currents

#if UNITY_EDITOR
                if (m_ProjectSettings && m_ProjectSettings.DragBuilding_AdvancedGridValidationProcess)
                {
                    string sourceIndex = REST_Logging.ColorizeString($"({p_ValidatorX};{p_ValidatorZ})", REST_Logging.Colors.White);
                    string sourceBaseStability = REST_Logging.ColorizeNumberHigherThanZero(p_Validator.m_Stability);
                    string sourceTempStability = REST_Logging.ColorizeNumberHigherThanZero(p_Validator.m_DragBuildingTemporaryStability);

                    string targetIndex = REST_Logging.ColorizeString($"({p_ValiadtedX};{p_ValiadtedX})", REST_Logging.Colors.White);
                    string targetBaseStability = REST_Logging.ColorizeNumberHigherThanZero(p_Valiadted.m_Stability);
                    string stargetTempStability = REST_Logging.ColorizeNumberHigherThanZero(p_Valiadted.m_DragBuildingTemporaryStability);

                    REST_Logging.Debug($"Stability source element : {targetIndex} Base Stability : {targetBaseStability} Temporary Stability : {stargetTempStability}" +
                                    $" | Set Stability for Element {sourceIndex} Base Stability : {sourceBaseStability} Temporary Stability : {sourceTempStability}");
                }
#endif
            }
        }

        private bool ValidateCurrentWithNeighbour_XAxis(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        { 
            int modified_X;
            // Check X + 1, Z 
            modified_X = p_X + 1 + m_AxisModifier;
            if (m_TemporaryElements.Count > modified_X && m_TemporaryElements[modified_X].Count > p_Z
                && CanValidateCurrentWithNeighbour(m_CanValidate_XNeg, m_CanValidate_XPos, p_CurrentElementValiationData.m_Result))
            {
                if(ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, modified_X, p_Z))
                {
                    return true;
                }
            }

            // Check X - 1, Z 
            modified_X = p_X - 1 - m_AxisModifier;
            if (p_X > m_AxisModifier 
                && CanValidateCurrentWithNeighbour(m_CanValidate_XPos, m_CanValidate_XNeg, p_CurrentElementValiationData.m_Result))
            {
                if (ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, modified_X, p_Z))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateCurrentWithNeighbour_ZAxis(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        {
            int modified_Z;
            // Check X, Z + 1
            modified_Z = p_Z + 1 + m_AxisModifier;
            if (m_TemporaryElements.Count > (p_X + m_AxisModifier)
                && (m_TemporaryElements[p_X].Count > modified_Z)
                && CanValidateCurrentWithNeighbour(m_CanValidate_ZPos, m_CanValidate_ZNeg, p_CurrentElementValiationData.m_Result))
            {
                if(ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, p_X, modified_Z))
                {
                    return true;
                }
            }

            // Check X, Z - 1 
            modified_Z = p_Z - 1 - m_AxisModifier;
            if (p_Z > m_AxisModifier
                && CanValidateCurrentWithNeighbour(m_CanValidate_ZNeg, m_CanValidate_ZPos, p_CurrentElementValiationData.m_Result))
            {
                if (ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, p_X, modified_Z))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateCurrentWithNeighbour_MixedAxis(ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, in int p_X, in int p_Z)
        {
            int modified_X, modified_Z;

            // Check X + 1
            modified_X = p_X + 1;
            if (m_TemporaryElements.Count > modified_X)
            {
                // Check X + 1, Z + 1
                modified_Z = p_Z + 1;
                if (m_TemporaryElements[modified_X].Count > modified_Z
                    && CanValidateCurrentWithNeighbour(m_CanValidate_XNegZNeg, m_CanValidate_XPosZPos, p_CurrentElementValiationData.m_Result))
                {
                    if(ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, modified_X, modified_Z))
                    {
                        return true;
                    }
                }

                // Check X + 1, Z - 1
                modified_Z = p_Z - 1;
                if (p_Z > 0
                    && CanValidateCurrentWithNeighbour(m_CanValidate_XNegZPos, m_CanValidate_XPosZNeg, p_CurrentElementValiationData.m_Result))
                {
                    if (ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, modified_X, modified_Z))
                    {
                        return true;
                    }
                }
            }

            // Check X - 1
            modified_X = p_X - 1;
            if (p_X > 0)
            {
                // Check X - 1, Z + 1
                modified_Z = p_Z + 1;
                if (m_TemporaryElements[modified_X].Count > modified_Z
                    && CanValidateCurrentWithNeighbour(m_CanValidate_XPosZNeg, m_CanValidate_XNegZPos, p_CurrentElementValiationData.m_Result))
                {
                    if (ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, modified_X, modified_Z))
                    {
                        return true;
                    }
                }

                // Check X - 1, Z - 1
                modified_Z = p_Z - 1;
                if (p_Z > 0 && CanValidateCurrentWithNeighbour(m_CanValidate_XPosZPos, m_CanValidate_XNegZNeg, p_CurrentElementValiationData.m_Result))
                {
                    if (ValidateCurrentWithNeighbour(p_CurrentElementValiationData, p_X, p_Z, modified_X, modified_Z))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanValidateCurrentWithNeighbour(bool p_CanValdiate, bool p_CanValdiateInverse, ABS_PositionValidationResult p_ElementResult)
        {
            if (p_CanValdiate && p_CanValdiateInverse)
            {
                return true;
            }
            else
            {
                p_ElementResult.DragBuildingValidation_BreakPositionRules = ABS_PositionValidationResult.ResultOptions.Failed;
                return false;
            }
        }

        private bool ValidateCurrentWithNeighbour(
            in ABS_PositionValidationData_AdvancedGrid p_CurrentElementValiationData, 
            in int p_ValidatedX, 
            in int p_ValidatedZ, 
            in int p_ValidatorX, 
            in int p_ValidatorZ)
        {
            ABS_PositionValidationData_AdvancedGrid validatorNeighbour = 
                m_TemporaryElements[p_ValidatorX][p_ValidatorZ].ValidationData as ABS_PositionValidationData_AdvancedGrid;
            if (validatorNeighbour != null && CanValdiatedTheNeighbourWithCurrent(validatorNeighbour))
            {
                p_CurrentElementValiationData.m_Result.DragBuildingValidation_ValidatedByNeighbour =  ABS_PositionValidationResult.ResultOptions.Validated;

                p_CurrentElementValiationData.m_DragBuildingTemporaryState = ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.CheckNeeded;
                CheckStability(validatorNeighbour, p_ValidatorX, p_ValidatorZ, p_CurrentElementValiationData, p_ValidatedX, p_ValidatedZ);

                ParentNeighbourValidationReq(p_CurrentElementValiationData, p_ValidatedX, p_ValidatedZ);
                return true;
            }
            return false;
        }

        private bool IsRotatedPosition (int p_X, int p_Z)
        {
            return (m_AllowMixedAxisDragBuilding && ((p_X % 2 == 0 || p_Z % 2 != 0)));
        }

        private ABS_PositionValidationData_AdvancedGrid GetValidationData(int p_X, int p_Z)
        {
            ABS_TemporaryBuildingElement tmpElement = m_TemporaryElements[p_X][p_Z];
            return tmpElement == null ? null : tmpElement.ValidationData as ABS_PositionValidationData_AdvancedGrid;
        }
    }
}
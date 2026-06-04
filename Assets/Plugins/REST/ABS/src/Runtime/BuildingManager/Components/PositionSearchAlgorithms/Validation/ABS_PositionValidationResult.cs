//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_PositionValidationResult
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum ResultOptions : ushort
        {
            Unkown = 0,
            Validated = 1,
            Failed = 2
        };

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool m_ParentCachedResult = false;
        private bool m_BuilderCachedResult = false;

        //Single Element Valdiation -------------------------
        private ResultOptions m_BaseElementValidation_UnderGround = ResultOptions.Unkown;
        private ResultOptions m_BaseElementValidation_GroundedCheck = ResultOptions.Unkown;
        private ResultOptions m_BaseElementValidation_BuildableGround = ResultOptions.Unkown;
        private ResultOptions m_BaseElementValidation_Collision = ResultOptions.Unkown;
        private ResultOptions m_BaseElementValidation_ElementCollision = ResultOptions.Unkown;
        private ResultOptions m_BaseElementValidation_AirHeightLimit = ResultOptions.Unkown;
        private ResultOptions m_BaseElementValidation_BuildOnTopOfElement = ResultOptions.Unkown;

        //FreeBuilding
        private ResultOptions m_SpecialElementValidation_RotationMaximumAngle = ResultOptions.Unkown;
        private ResultOptions m_SpecialElementValidation_ShouldAttached = ResultOptions.Unkown;
        //AdvancedGridBuilding
        private ResultOptions m_SpecialElementValidation_ForbiddenAxis = ResultOptions.Unkown;
        private ResultOptions m_SpecialElementValidation_Stability = ResultOptions.Unkown;

        //custom Validation
        private ResultOptions m_CustomElementValidation = ResultOptions.Unkown;

        //Parent Building Validation ------------------------
        private ResultOptions m_ParentBuildingValidation_UsedPosition = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_ValidatedByPreBuilt = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_ValidatedByOverrideSettings = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_SnappingToFoundation = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_InvalidPosition = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_BreakRangeLimitRules = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_BreakPositionRules = ResultOptions.Unkown;
        private ResultOptions m_ParentBuildingValidation_BreakPositionRules_Denied = ResultOptions.Unkown;

        //Drag Building Validation --------------------------
        private ResultOptions m_DragBuildingValidation_ValidatedByNeighbour_UnderGround = ResultOptions.Unkown;
        private ResultOptions m_DragBuildingValidation_UnderGround = ResultOptions.Unkown;
        private ResultOptions m_DragBuildingValidation_ValidatedByNeighbour = ResultOptions.Unkown;
        private ResultOptions m_DragBuildingValidation_BreakPositionRules = ResultOptions.Unkown;
        private ResultOptions m_DragBuildingValidation_BreakPositionRules_Denied = ResultOptions.Unkown;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Specific Getters and Check functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool IsFailed()
        {
            return !IsSuccessFull();
        }

        public bool IsSuccessFull(in bool p_IgnoreCollisionCheck = false,
                                  in bool p_IgnoreElementCollisionCheck  = false,
                                  in bool p_IgnoreBuildableGroundCheck = false,
                                  in bool p_IgnorePositionRules = false,
                                  in bool p_IgnoreShouldSnapToFoundation = false,
                                  in bool p_IgnoreShouldAttached = false,
                                  in bool p_IgnoreGroundedCheck = false,
                                  in bool p_IgnoreStabiltyCheck = false)
        {
            return CheckSingleElementValidation(
                        p_IgnoreCollisionCheck, 
                        p_IgnoreElementCollisionCheck, 
                        p_IgnoreBuildableGroundCheck,
                        p_IgnoreShouldAttached,
                        p_IgnoreGroundedCheck,
                        p_IgnoreStabiltyCheck)
                 && CheckParentBuildingValidation(
                     false, 
                     p_IgnorePositionRules, 
                     p_IgnoreShouldSnapToFoundation);
        }

        public bool CheckSingleElementValidation (in bool p_IgnoreCollisionCheck = false,
                                                  in bool p_IgnoreElementCollisionCheck = false,
                                                  in bool p_IgnoreBuildableGroundCheck = false,
                                                  in bool p_IgnoreShouldAttached = false,
                                                  in bool p_IgnoreGroundedCheck = false,
                                                  in bool p_IgnoreStabiltyCheck = false)
        {
            return CheckBaseElementValidation(p_IgnoreCollisionCheck, p_IgnoreElementCollisionCheck, p_IgnoreBuildableGroundCheck, p_IgnoreGroundedCheck)
                   && m_SpecialElementValidation_RotationMaximumAngle != ResultOptions.Failed
                   && (p_IgnoreShouldAttached || m_SpecialElementValidation_ShouldAttached != ResultOptions.Failed)
                   && m_SpecialElementValidation_ForbiddenAxis != ResultOptions.Failed
                   && Check_Stability(p_IgnoreStabiltyCheck)
                   && m_CustomElementValidation != ResultOptions.Failed;
        }

        public bool CheckParentBuildingValidation (
            in bool p_IgnoreInvalidPosition, 
            in bool p_IgnorePositionRules,
            in bool p_IgnoreShouldSnapToFoundation)
        {
            return (p_IgnorePositionRules || m_ParentBuildingValidation_BreakPositionRules_Denied != ResultOptions.Failed)
                && (p_IgnorePositionRules || m_ParentBuildingValidation_BreakPositionRules != ResultOptions.Failed)
                && (p_IgnoreShouldSnapToFoundation || m_ParentBuildingValidation_SnappingToFoundation != ResultOptions.Failed)
                && m_ParentBuildingValidation_BreakRangeLimitRules != ResultOptions.Failed
                && CheckParentBuildingValidation_UsedPosition()
                && (p_IgnoreInvalidPosition || Check_ParentValidation_InvalidPosition());
        }

        public bool Check_Stability(in bool p_IgnoreStabiltyCheck = false)
        {
            //If the stability feature is on then the elements can only validate their neighbour if their stability is at least 2
            //So the currrent element's stability minus 1 should be at least 1
            //So the neighbour's stability problem should be fixed by the neighbour
            return p_IgnoreStabiltyCheck
                    || m_SpecialElementValidation_Stability != ResultOptions.Failed
                    || m_DragBuildingValidation_ValidatedByNeighbour == ResultOptions.Validated;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  private implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool CheckBaseElementValidation(
            in bool p_IgnoreCollisionCheck,
            in bool p_IgnoreElementCollisionCheck,
            in bool p_IgnoreBuildableGroundCheck,
            in bool p_IgnoreGroundedCheck)
        {
            return CheckGroundedAndAirScenarios(p_IgnoreGroundedCheck)
                   && m_BaseElementValidation_UnderGround != ResultOptions.Failed
                   && m_BaseElementValidation_BuildOnTopOfElement != ResultOptions.Failed
                   && (p_IgnoreCollisionCheck || m_BaseElementValidation_Collision != ResultOptions.Failed)
                   && (p_IgnoreElementCollisionCheck || m_BaseElementValidation_ElementCollision != ResultOptions.Failed)
                   && (p_IgnoreBuildableGroundCheck || m_BaseElementValidation_BuildableGround != ResultOptions.Failed);
        }

        private bool CheckParentBuildingValidation_UsedPosition()
        {
            //Failed if the ABS_Building's position is already used and
            //it wasn't the element's prebuilt version or 
            //it wasn't validated by the override logic
            if (m_ParentBuildingValidation_UsedPosition != ResultOptions.Failed)
            {
                return true;
            }
            else
            {
                return m_ParentBuildingValidation_ValidatedByPreBuilt == ResultOptions.Validated
                        || m_ParentBuildingValidation_ValidatedByOverrideSettings == ResultOptions.Validated;
            }
        }

        private bool CheckGroundedAndAirScenarios (in bool p_IgnoreGroundedCheck)
        {
            return m_BaseElementValidation_AirHeightLimit == ResultOptions.Validated
                    || (m_BaseElementValidation_AirHeightLimit == ResultOptions.Failed || m_BaseElementValidation_GroundedCheck == ResultOptions.Validated)
                    || (p_IgnoreGroundedCheck || m_BaseElementValidation_GroundedCheck != ResultOptions.Failed)
                    || (m_BaseElementValidation_GroundedCheck == ResultOptions.Failed && m_BaseElementValidation_AirHeightLimit == ResultOptions.Validated);
        }

        private bool Check_ParentValidation_InvalidPosition()
        {
            //Failed if the position is invalid but the none of the neighbour didn't validated it.
            return m_ParentBuildingValidation_InvalidPosition != ResultOptions.Failed
                    || m_DragBuildingValidation_ValidatedByNeighbour == ResultOptions.Validated;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool ParentCachedResult
        {
            get { return m_ParentCachedResult; }
            set { m_ParentCachedResult = value; }
        }
        public bool BuilderCachedResult
        {
            get { return m_BuilderCachedResult; }
            set { m_BuilderCachedResult = value; }
        }
        public ResultOptions BaseElementValidation_UnderGround
        {
            get { return m_BaseElementValidation_UnderGround; }
            set { m_BaseElementValidation_UnderGround = value; }
        }
        public ResultOptions BaseElementValidation_GroundedCheck
        {
            get { return m_BaseElementValidation_GroundedCheck; }
            set { m_BaseElementValidation_GroundedCheck = value; }
        }
        public ResultOptions BaseElementValidation_Collision
        {
            get { return m_BaseElementValidation_Collision; }
            set { m_BaseElementValidation_Collision = value; }
        }
        public ResultOptions BaseElementValidation_ElementCollision
        {
            get { return m_BaseElementValidation_ElementCollision; }
            set { m_BaseElementValidation_ElementCollision = value; }
        }
        public ResultOptions BaseElementValidation_AirHeightLimit
        {
            get { return m_BaseElementValidation_AirHeightLimit; }
            set { m_BaseElementValidation_AirHeightLimit = value; }
        }
        public ResultOptions BaseElementValidation_BuildOnTopOfElement
        {
            get { return m_BaseElementValidation_BuildOnTopOfElement; }
            set { m_BaseElementValidation_BuildOnTopOfElement = value; }
        }
        public ResultOptions BaseElementValidation_BuildableGround
        {
            get { return m_BaseElementValidation_BuildableGround; }
            set { m_BaseElementValidation_BuildableGround = value; }
        }
        public ResultOptions SpecialElementValidation_RotationMaximumAngle
        {
            get { return m_SpecialElementValidation_RotationMaximumAngle; }
            set { m_SpecialElementValidation_RotationMaximumAngle = value; }
        }
        public ResultOptions SpecialElementValidation_ShouldAttached
        {
            get { return m_SpecialElementValidation_ShouldAttached; }
            set { m_SpecialElementValidation_ShouldAttached = value; }
        }
        public ResultOptions SpecialElementValidation_ForbiddenAxis
        {
            get { return m_SpecialElementValidation_ForbiddenAxis; }
            set { m_SpecialElementValidation_ForbiddenAxis = value; }
        }
        public ResultOptions SpecialElementValidation_Stability
        {
            get { return m_SpecialElementValidation_Stability; }
            set { m_SpecialElementValidation_Stability = value; }
        }
        public ResultOptions CustomElementValidation
        {
            get { return m_CustomElementValidation; }
            set { m_CustomElementValidation = value; }
        }
        public ResultOptions ParentBuildingValidation_ValidatedByPreBuilt
        {
            get { return m_ParentBuildingValidation_ValidatedByPreBuilt; }
            set { m_ParentBuildingValidation_ValidatedByPreBuilt = value; }
        }
        public ResultOptions ParentBuildingValidation_ValidatedByOverrideSettings
        {
            get { return m_ParentBuildingValidation_ValidatedByOverrideSettings; }
            set { m_ParentBuildingValidation_ValidatedByOverrideSettings = value; }
        }
        public ResultOptions ParentBuildingValidation_SnappingToFoundation
        {
            get { return m_ParentBuildingValidation_SnappingToFoundation; }
            set { m_ParentBuildingValidation_SnappingToFoundation = value; }
        }
        public ResultOptions ParentBuildingValidation_UsedPosition
        {
            get { return m_ParentBuildingValidation_UsedPosition; }
            set { m_ParentBuildingValidation_UsedPosition = value; }
        }
        public ResultOptions ParentBuildingValidation_InvalidPosition
        {
            get { return m_ParentBuildingValidation_InvalidPosition; }
            set { m_ParentBuildingValidation_InvalidPosition = value; }
        }
        public ResultOptions ParentBuildingValidation_BreakRangeLimitRules
        {
            get { return m_ParentBuildingValidation_BreakRangeLimitRules; }
            set { m_ParentBuildingValidation_BreakRangeLimitRules = value; }
        }
        public ResultOptions ParentBuildingValidation_BreakPositionRules
        {
            get { return m_ParentBuildingValidation_BreakPositionRules; }
            set { m_ParentBuildingValidation_BreakPositionRules = value; }
        }
        public ResultOptions ParentBuildingValidation_BreakPositionRules_Denied
        {
            get { return m_ParentBuildingValidation_BreakPositionRules_Denied; }
            set { m_ParentBuildingValidation_BreakPositionRules_Denied = value; }
        }
        public ResultOptions DragBuildingValidation_ValidatedByNeighbour_UnderGround
        {
            get { return m_DragBuildingValidation_ValidatedByNeighbour_UnderGround; }
            set { m_DragBuildingValidation_ValidatedByNeighbour_UnderGround = value; }
        }
        public ResultOptions DragBuildingValidation_UnderGround
        {
            get { return m_DragBuildingValidation_UnderGround; }
            set { m_DragBuildingValidation_UnderGround = value; }
        }
        public ResultOptions DragBuildingValidation_ValidatedByNeighbour
        {
            get { return m_DragBuildingValidation_ValidatedByNeighbour; }
            set { m_DragBuildingValidation_ValidatedByNeighbour = value; }
        }
        public ResultOptions DragBuildingValidation_BreakPositionRules
        {
            get { return m_DragBuildingValidation_BreakPositionRules; }
            set { m_DragBuildingValidation_BreakPositionRules = value; }
        }
        public ResultOptions DragBuildingValidation_BreakPositionRules_Denied
        {
            get { return m_DragBuildingValidation_BreakPositionRules_Denied; }
            set { m_DragBuildingValidation_BreakPositionRules_Denied = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Merge(in ABS_PositionValidationResult p_Other)
        {
            m_BaseElementValidation_UnderGround |= p_Other.m_BaseElementValidation_UnderGround;
            m_BaseElementValidation_GroundedCheck |= p_Other.m_BaseElementValidation_GroundedCheck;
            m_BaseElementValidation_BuildableGround |= p_Other.m_BaseElementValidation_BuildableGround;
            m_BaseElementValidation_Collision |= p_Other.m_BaseElementValidation_Collision;
            m_BaseElementValidation_ElementCollision |= p_Other.m_BaseElementValidation_ElementCollision;
            m_BaseElementValidation_AirHeightLimit |= p_Other.m_BaseElementValidation_AirHeightLimit;
            m_BaseElementValidation_BuildOnTopOfElement |= p_Other.m_BaseElementValidation_BuildOnTopOfElement;

            m_SpecialElementValidation_RotationMaximumAngle |= p_Other.m_SpecialElementValidation_RotationMaximumAngle;
            m_SpecialElementValidation_ShouldAttached |= p_Other.m_SpecialElementValidation_ShouldAttached;
            m_SpecialElementValidation_ForbiddenAxis |= p_Other.m_SpecialElementValidation_ForbiddenAxis;
            m_SpecialElementValidation_Stability |= p_Other.m_SpecialElementValidation_Stability;

            m_CustomElementValidation |= p_Other.m_CustomElementValidation;

            m_ParentBuildingValidation_UsedPosition |= p_Other.m_ParentBuildingValidation_UsedPosition;
            m_ParentBuildingValidation_ValidatedByPreBuilt |= p_Other.m_ParentBuildingValidation_ValidatedByPreBuilt;
            m_ParentBuildingValidation_ValidatedByOverrideSettings |= p_Other.m_ParentBuildingValidation_ValidatedByOverrideSettings;
            m_ParentBuildingValidation_SnappingToFoundation |= p_Other.m_ParentBuildingValidation_SnappingToFoundation;

            m_ParentBuildingValidation_InvalidPosition |= p_Other.m_ParentBuildingValidation_InvalidPosition;

            m_ParentBuildingValidation_BreakRangeLimitRules |= p_Other.m_ParentBuildingValidation_BreakRangeLimitRules;
            m_ParentBuildingValidation_BreakPositionRules |= p_Other.m_ParentBuildingValidation_BreakPositionRules;
            m_ParentBuildingValidation_BreakPositionRules_Denied |= p_Other.m_ParentBuildingValidation_BreakPositionRules_Denied;

            m_DragBuildingValidation_ValidatedByNeighbour_UnderGround |= p_Other.m_DragBuildingValidation_ValidatedByNeighbour_UnderGround;
            m_DragBuildingValidation_UnderGround |= p_Other.m_DragBuildingValidation_UnderGround;

            m_DragBuildingValidation_ValidatedByNeighbour |= p_Other.m_DragBuildingValidation_ValidatedByNeighbour;

            m_DragBuildingValidation_BreakPositionRules |= p_Other.m_DragBuildingValidation_BreakPositionRules;
            m_DragBuildingValidation_BreakPositionRules_Denied |= p_Other.m_DragBuildingValidation_BreakPositionRules_Denied;
        }

        public ABS_SimpleBuildingProcessErrorCode GetBlockReason ()
        {
            if (!Check_ParentValidation_InvalidPosition())
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Building_InvalidPosition;
            }
            else if (m_BaseElementValidation_UnderGround == ResultOptions.Failed
                    || (m_DragBuildingValidation_ValidatedByNeighbour_UnderGround == ResultOptions.Failed
                        && m_DragBuildingValidation_UnderGround != ResultOptions.Validated))
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_UnderGround;
            }
            else if (m_BaseElementValidation_GroundedCheck == ResultOptions.Failed
                && m_BaseElementValidation_AirHeightLimit != ResultOptions.Validated)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_GroundedCheck;
            }
            else if (m_BaseElementValidation_AirHeightLimit == ResultOptions.Failed
                && m_BaseElementValidation_GroundedCheck != ResultOptions.Validated)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_AirHeightLimit;
            }
            else if (!CheckParentBuildingValidation_UsedPosition())
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Building_UsedPosition;
            }
            else if (m_BaseElementValidation_Collision == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_Collision;
            }
            else if (m_BaseElementValidation_ElementCollision == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_ElementCollision;
            }
            else if (m_CustomElementValidation == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_Custom;
            }
            else if (m_SpecialElementValidation_ForbiddenAxis == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_AdvancedGridBuidling_ForbiddenAxis;
            }
            else if (!Check_Stability(false))
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_AdvancedGridBuidling_Stability;
            }
            else if (m_BaseElementValidation_BuildOnTopOfElement == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_BuildOnTopOfElement;
            }
            else if (m_BaseElementValidation_BuildableGround == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_BuildableGround;
            }
            else if (m_ParentBuildingValidation_BreakRangeLimitRules == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Building_RangeRules;
            }
            else if (m_ParentBuildingValidation_SnappingToFoundation == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_Validation_ShouldSnapToFoundation;
            }
            else if (m_ParentBuildingValidation_BreakPositionRules_Denied == ResultOptions.Failed
                    || m_DragBuildingValidation_BreakPositionRules_Denied == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_AdvancedGridBuidling_PositionRuleset_Denied;
            }
            else if (m_SpecialElementValidation_RotationMaximumAngle == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_FreeBuidling_RotationMaximumAngle;
            }
            else if (m_SpecialElementValidation_ShouldAttached == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_FreeBuidling_ShouldAttached;
            }
            //This should be tested last because this is means that all of the posiiton is blocked
            //If it is before anything it can hide that what was actually the problem of the positions
            else if (m_ParentBuildingValidation_BreakPositionRules == ResultOptions.Failed)
            {
                return ABS_SimpleBuildingProcessErrorCode.RuleBreak_AdvancedGridBuidling_PositionRuleset;
            }
            else
            {
                return ABS_SimpleBuildingProcessErrorCode.Unkown;
            }
        }

        //For debug purposes
        public override string ToString ()
        {
            bool ignored = false;

            string MSG = $"ParentCachedResult  : {REST_Logging.ColorizeString(m_ParentCachedResult.ToString(), m_ParentCachedResult ? REST_Logging.Colors.Green : REST_Logging.Colors.Red)}\n";
            MSG += $"BuilderCachedResult  : {REST_Logging.ColorizeString(m_BuilderCachedResult.ToString(), m_BuilderCachedResult ? REST_Logging.Colors.Green : REST_Logging.Colors.Red)}\n";
            MSG += "\n";
            MSG += $"CustomElementValidation : {GetToStringValueValidation(m_CustomElementValidation)}\n";
            MSG += "\n";
            MSG += $"BaseElementValidation_UnderGround : {GetToStringValueValidation(m_BaseElementValidation_UnderGround)}\n";
            ignored = m_BaseElementValidation_GroundedCheck == ResultOptions.Failed
                        && m_BaseElementValidation_AirHeightLimit == ResultOptions.Validated;
            MSG += $"BaseElementValidation_GroundedCheck : {GetToStringValueValidation(m_BaseElementValidation_GroundedCheck, ignored)}\n";
            MSG += $"BaseElementValidation_BuildableGround : {GetToStringValueValidation(m_BaseElementValidation_BuildableGround)}\n";
            MSG += $"BaseElementValidation_Collision : {GetToStringValueValidation(m_BaseElementValidation_Collision)}\n";
            MSG += $"BaseElementValidation_ElementCollision : {GetToStringValueValidation(m_BaseElementValidation_ElementCollision)}\n";
            ignored = m_BaseElementValidation_AirHeightLimit == ResultOptions.Failed
                        && m_BaseElementValidation_GroundedCheck == ResultOptions.Validated;
            MSG += $"BaseElementValidation_AirHeightLimit : {GetToStringValueValidation(m_BaseElementValidation_AirHeightLimit, ignored)}\n";
            MSG += $"BaseElementValidation_BuildOnTopOfElement : {GetToStringValueValidation(m_BaseElementValidation_BuildOnTopOfElement)}\n";
            MSG += "\n";
            MSG += $"SpecialElementValidation_RotationMaximumAngle : {GetToStringValueValidation(m_SpecialElementValidation_RotationMaximumAngle)}\n";
            MSG += $"SpecialElementValidation_ShouldAttached : {GetToStringValueValidation(m_SpecialElementValidation_ShouldAttached)}\n";
            MSG += $"SpecialElementValidation_ForbiddenAxis : {GetToStringValueValidation(m_SpecialElementValidation_ForbiddenAxis)}\n";
            ignored = m_SpecialElementValidation_Stability == ResultOptions.Failed
                        && m_DragBuildingValidation_ValidatedByNeighbour == ResultOptions.Validated;
            MSG += $"SpecialElementValidation_Stability : {GetToStringValueValidation(m_SpecialElementValidation_Stability, ignored)}\n";
            MSG += "\n";
            ignored = m_ParentBuildingValidation_UsedPosition == ResultOptions.Failed
                        && (m_ParentBuildingValidation_ValidatedByPreBuilt == ResultOptions.Validated
                            || m_ParentBuildingValidation_ValidatedByOverrideSettings == ResultOptions.Validated);
            MSG += $"ParentBuildingValidation_UsedPosition : {GetToStringValueValidation(m_ParentBuildingValidation_UsedPosition, ignored)}\n";
            MSG += $"ParentBuildingValidation_ValidatedByPreBuilt : {GetToStringValueValidation(m_ParentBuildingValidation_ValidatedByPreBuilt)}\n";
            MSG += $"ParentBuildingValidation_ValidatedByOverrideSettings : {GetToStringValueValidation(m_ParentBuildingValidation_ValidatedByOverrideSettings)}\n";
            MSG += $"ParentBuildingValidation_SnappingToFoundation : {GetToStringValueValidation(m_ParentBuildingValidation_SnappingToFoundation)}\n";
            ignored = m_ParentBuildingValidation_InvalidPosition == ResultOptions.Failed
                        && m_DragBuildingValidation_ValidatedByNeighbour == ResultOptions.Validated;
            MSG += $"ParentBuildingValidation_InvalidPosition : {GetToStringValueValidation(m_ParentBuildingValidation_InvalidPosition, ignored)}\n";
            MSG += $"ParentBuildingValidation_BreakRangeLimitRules : {GetToStringValueValidation(m_ParentBuildingValidation_BreakRangeLimitRules)}\n";
            MSG += $"ParentBuildingValidation_BreakPositionRules : {GetToStringValueValidation(m_ParentBuildingValidation_BreakPositionRules)}\n";
            MSG += $"ParentBuildingValidation_BreakPositionRules_Denied : {GetToStringValueValidation(m_ParentBuildingValidation_BreakPositionRules_Denied)}\n";
            MSG += "\n";
            MSG += $"DragBuildingValidation_ValidatedByNeighbour_UnderGround : {GetToStringValueValidation(m_DragBuildingValidation_ValidatedByNeighbour_UnderGround)}\n";
            MSG += $"DragBuildingValidation_ValidatedByNeighbour : {GetToStringValueValidation(m_DragBuildingValidation_ValidatedByNeighbour)}\n";
            MSG += $"DragBuildingValidation_BreakPositionRules : {GetToStringValueValidation(m_DragBuildingValidation_BreakPositionRules)}\n";
            MSG += $"DragBuildingValidation_BreakPositionRules_Denied : {GetToStringValueValidation(m_DragBuildingValidation_BreakPositionRules_Denied)}\n";
            MSG += $"DragBuildingValidation_UnderGround : {GetToStringValueValidation(m_DragBuildingValidation_UnderGround)}\n";

            return MSG;
        }

        private string GetToStringValueValidation (ResultOptions p_Option, bool p_Ignored = false)
        {
            switch (p_Option)
            {
                case ResultOptions.Validated : 
                    return $"{REST_Logging.ColorizeString(" Validated", REST_Logging.Colors.Green)}" +
                        $" {(p_Ignored ? REST_Logging.s_Literal_Ignored : "" )}";
                case ResultOptions.Failed: 
                    return $"{REST_Logging.ColorizeString(" Failed", REST_Logging.Colors.Red)}" +
                        $" {(p_Ignored ? REST_Logging.s_Literal_Ignored : "")}";
                case ResultOptions.Unkown:
                default:
                    return $"{REST_Logging.ColorizeString(" Unkown", REST_Logging.Colors.Blue)}" +
                        $" {(p_Ignored ? REST_Logging.s_Literal_Ignored : "")}";
            }
        }

    }
}
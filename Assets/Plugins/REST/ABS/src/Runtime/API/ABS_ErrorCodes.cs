//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_SimpleBuildingProcessErrorCode
    {
        Unkown,
        Successful,

        HiddenElement,

        RuleBreak_AreaRules,
        RuleBreak_FoundationLogic,
        RuleBreak_ShouldOverride,
        RuleBreak_AllowBuildingInTheAir,
        RuleBreak_AboveMaximumElementCount,
        RuleBreak_PlayerCollision,

        //Building
        RuleBreak_Building_MaximumElementCount,
        RuleBreak_Building_InvalidPosition,
        RuleBreak_Building_UsedPosition,
        RuleBreak_Building_RangeRules,

        //Validation
        RuleBreak_Validation_UnderGround,
        RuleBreak_Validation_GroundedCheck,
        RuleBreak_Validation_BuildableGround,
        RuleBreak_Validation_Collision,
        RuleBreak_Validation_ElementCollision,
        RuleBreak_Validation_AirHeightLimit,
        RuleBreak_Validation_BuildOnTopOfElement,
        RuleBreak_Validation_ShouldSnapToFoundation,
        RuleBreak_Validation_Custom,

        //Specific AdvancedGridBuidling cases
        RuleBreak_AdvancedGridBuidling_ForbiddenAxis,
        RuleBreak_AdvancedGridBuidling_Stability,
        RuleBreak_AdvancedGridBuidling_PositionRuleset,
        RuleBreak_AdvancedGridBuidling_PositionRuleset_Denied,

        //Specific FreeBuidling cases
        RuleBreak_FreeBuidling_RotationMaximumAngle,
        RuleBreak_FreeBuidling_ShouldAttached
    }

    public enum ABS_ActionHistoryErrorCodes
    {
        Unkown,

        Success,
        Success_EmptyHistroy,
        Success_NoMoreAction,
        Success_FeatureDisabled,

        Failed_BuildingElementNotAvailable,
        Failed_BuildingNotAvailable,
        Failed_BuildingParentNotAvailable,
        Failed_DeniedByTracker,

        Failed_PositionValidation_UsedPosition,
        Failed_PositionValidation_NotEnoughSpace,

        Failed_DataValidation_UnkownGUID,
        Failed_DataValidation_WrongParentObject,
        Failed_DataValidation_WrongInstanceGuid,
        Failed_DataValidation_WrongPrefabGuid,
        Failed_DataValidation_WrongLocalPosition,
        Failed_DataValidation_WrongLocalEulerAngles
    }

    public enum ABS_PersistencyLoadErrorCode
    {
        Unkown,
        Successful,

        JSON_NullOrEmptyString,
        JSON_ErrorDuringLoad,

        PersistedData_NullInput,
        PersistedData_WrongDataType,

        BuildingElementList_NullInput,
        BuildingElementList_MissingElement,

        BuildingElementConnection_MissingBuilding,
        BuildingElementConnection_MissingBuildingElement,

        Building_UnkownInstantiateError,
        Building_AlreadyUsedPosition,

        BuildingElement_UnkownInstantiateError,
        BuildingElement_UnkownComponentError,
        BuildingElement_NullPrefab,
        BuildingElement_AddToBuildingFailed,

        Update_WrongBasicObjectValues_BuildingParent,
        Update_WrongBasicObjectValues_Building,
        Update_WrongBasicObjectValues_BuildingElement,
    }
}
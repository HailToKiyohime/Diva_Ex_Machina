//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_SnapPointBasedBuilding : ABS_Building
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Main Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_SnapPointBasedBuilding() : base (true, true, true)
        {
            PositionSearchAlgorithmType = ABS_PositionSearchAlgorithm.SnapPointBased;
        }

        protected override void ValidatePositionImpl(ABS_PositionValidationData p_ResultData,
                                                     in Vector3 p_LocalPosition,
                                                     in Quaternion p_LocalRotation,
                                                     in ABS_BuildingElement p_ElementForBuild)
        {
            CheckUsedPosition(p_LocalPosition, p_ElementForBuild, p_ResultData);
        }

        protected override void ElementIsPlaced(ABS_BuildingElement p_Element)
        {
            //nothing
        }
        protected override void ElementWillBeRemoved(
            ABS_DestroyActionElementData p_BaseDestroyActionData,
            ABS_BuildingManagerTracker p_Tracker,
            bool p_TriggeredByHistory,
            bool p_IgnoreStability,
            ABS_BuildingElement p_ElementToRemove)
        {
            //nothing
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation ISaveable
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [System.Serializable]
        public class ABS_SnapPointBasedBuildingPersistedData : ABS_Building.ABS_BuildingPersistedData
        {
            public override string ToJSON(in bool p_PrettyPrint)
            {
                return ABS_PersistencyManager.ToJson(this, p_PrettyPrint);
            }
        }

        public override string ToJSON(in bool p_PrettyPrint)
        {
            return GetPersistedData().ToJSON(p_PrettyPrint);
        }

        public ABS_SnapPointBasedBuildingPersistedData GetPersistedData()
        {
            ABS_SnapPointBasedBuildingPersistedData data = new ABS_SnapPointBasedBuildingPersistedData();
            GetBasePersistedData(data);
            return data;
        }

        protected override ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl(ABS_BuildingPersistedData p_Data)
        {
            return ABS_PersistencyLoadErrorCode.Successful;
        }
    }
}
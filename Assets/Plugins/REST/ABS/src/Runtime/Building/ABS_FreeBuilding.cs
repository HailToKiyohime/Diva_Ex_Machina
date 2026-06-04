//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************


namespace REST.AdvancedBuildSystem
{
    public class ABS_FreeBuilding : ABS_Building
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Main Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_FreeBuilding() : base (true, true, true)
        {
            PositionSearchAlgorithmType = ABS_PositionSearchAlgorithm.Free;
        }

        protected override void ValidatePositionImpl(ABS_PositionValidationData p_ResultData,
                                                     in Vector3 p_LocalPosition,
                                                     in Quaternion p_LocalRotation,
                                                     in ABS_BuildingElement p_ElementForBuild)
        {
            //nothing
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
        public class ABS_FreeBuildingPersistedData : ABS_Building.ABS_BuildingPersistedData
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


        public ABS_FreeBuildingPersistedData GetPersistedData()
        {
            ABS_FreeBuildingPersistedData data = new ABS_FreeBuildingPersistedData();
            GetBasePersistedData(data);
            return data;
        }

        protected override ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl(ABS_BuildingPersistedData p_Data)
        {
            return ABS_PersistencyLoadErrorCode.Successful;
        }
    }
}
//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IBuildingManagerStatisticsTracker
    {
        public delegate void StatisticsDataCallbackDelegate(ABS_BuildingManagerStatisticsData p_StatisticsData);

        public void StatisticsDataCallback(ABS_BuildingManagerStatisticsData p_StatisticsData);

    }
}
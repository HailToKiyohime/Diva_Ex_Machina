//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_PositionerManager : ABS_BuildingManagerComponentBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BuildingElement m_ActiveBuildingElement = null;

        private ABS_BuilderBase m_PositionSearchAlgorithm = null;

        private ABS_SnapPointBasedBuilder m_SnapPointBasedBuilder = null;
        private ABS_AdvancedGridBuilder m_AdvancedGridBuilder = null;
        private ABS_BasicGridBuilder m_BasicGridBuilder = null;
        private ABS_FreeBuilder m_FreeBuilder = null;

#if UNITY_EDITOR
        private ABS_ProjectSettings m_ProjectSettings = null;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_PositionerManager(ABS_IBuildingManagerInternalInterface p_Manager, ABS_BuildingManagerTracker p_Tracker)
            : base(p_Manager, p_Tracker)
        {
#if UNITY_EDITOR
            if (m_ProjectSettings == null)
            {
                m_ProjectSettings = ABS_ProjectSettingsGetter.GetSettings();
            }
#endif

            m_FreeBuilder = new ABS_FreeBuilder(p_Manager, p_Tracker);
            m_BasicGridBuilder = new ABS_BasicGridBuilder(p_Manager, p_Tracker);
            m_AdvancedGridBuilder = new ABS_AdvancedGridBuilder(p_Manager, p_Tracker);
            m_SnapPointBasedBuilder = new ABS_SnapPointBasedBuilder(p_Manager, p_Tracker);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void ResetBuildingElement(in ABS_BuildingElement p_BuildingElement)
        {
            m_ActiveBuildingElement = p_BuildingElement;
            ABS_BuilderBaseSettings algorithmSettings = m_ActiveBuildingElement.PositionAlgorithmSettings;

            switch (m_ActiveBuildingElement.PositionSearchAlgorithm)
            {
                case ABS_PositionSearchAlgorithm.SnapPointBased:
                    {
                        m_PositionSearchAlgorithm = m_SnapPointBasedBuilder;
                        m_SnapPointBasedBuilder.Settings = algorithmSettings;
                        m_FreeBuilder.Settings = algorithmSettings;

                        m_SnapPointBasedBuilder.ResetActiveBuildingElement(m_ActiveBuildingElement);
                        m_FreeBuilder.ResetActiveBuildingElement(m_ActiveBuildingElement);
                    }
                    break;
                case ABS_PositionSearchAlgorithm.AdvancedGrid:
                    {
                        m_PositionSearchAlgorithm = m_AdvancedGridBuilder;
                        m_AdvancedGridBuilder.Settings = algorithmSettings;
                        m_FreeBuilder.Settings = algorithmSettings;

                        m_AdvancedGridBuilder.ResetActiveBuildingElement(m_ActiveBuildingElement);
                        m_FreeBuilder.ResetActiveBuildingElement(m_ActiveBuildingElement);
                    }
                    break;
                case ABS_PositionSearchAlgorithm.BasicGrid:
                    {
                        m_PositionSearchAlgorithm = m_BasicGridBuilder;
                        m_BasicGridBuilder.Settings = algorithmSettings;

                        m_BasicGridBuilder.ResetActiveBuildingElement(m_ActiveBuildingElement);
                    }
                    break;
                case ABS_PositionSearchAlgorithm.Free:
                    {
                        m_PositionSearchAlgorithm = m_FreeBuilder;
                        m_FreeBuilder.Settings = algorithmSettings;

                        m_FreeBuilder.ResetActiveBuildingElement(m_ActiveBuildingElement);
                    }
                    break;
            }
        }

        public void ValidatePosition(ABS_PositionValidationData p_Result,
                                     in ABS_Building p_TargetBuilding, 
                                     in Vector3 p_GlobalPosition,
                                     in Quaternion p_Rotation,
                                     in bool p_IsElementAlignedToGround,
                                     in bool p_SkipUndergroundCheck)
        {
            m_PositionSearchAlgorithm.BaseElementValidation(
                p_Result, 
                p_TargetBuilding, 
                p_GlobalPosition, 
                p_Rotation, 
                p_IsElementAlignedToGround, 
                p_SkipUndergroundCheck);
        }

        public void ClearCache()
        {
            m_SnapPointBasedBuilder.ClearCache();
            m_AdvancedGridBuilder.ClearCache();
            m_BasicGridBuilder.ClearCache();
            m_FreeBuilder.ClearCache();
        }

        public ABS_PositionSearchResult SearchPosition(in bool p_ForcedFallback)
        {
            ABS_PositionSearchResult result = new ABS_PositionSearchResult();
            if (p_ForcedFallback
                && (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid
                    || m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.SnapPointBased))
            {

                m_FreeBuilder.SearchPosition(false, result);
                result.IsFallbackResult = true;

#if UNITY_EDITOR
                if (m_ProjectSettings && m_ProjectSettings.PositionSearchProcess_Result)
                {
                    REST_Logging.Debug($"ForcedFallback results : \n{result}");
                }
#endif

            }
            else
            {
                m_PositionSearchAlgorithm.SearchPosition(true, result);

#if UNITY_EDITOR
                if (m_ProjectSettings && m_ProjectSettings.PositionSearchProcess_Result)
                {
                    REST_Logging.Debug($"Main algorithm results : \n{result}");
                }
#endif
                if (result.Result == ABS_PositionSearchResult.ResultType.FallbackIsNeeded)
                {
                    result = new ABS_PositionSearchResult();
                    m_FreeBuilder.SearchPosition(false, result);
                    result.IsFallbackResult = true;

#if UNITY_EDITOR
                    if (m_ProjectSettings && m_ProjectSettings.PositionSearchProcess_Result)
                    {
                        REST_Logging.Debug($"Fallback algorithm results : \n{result}");
                    }
#endif

                }
            }

            if (result.IsFallbackResult)
            {
                PostProcessing(result);
            }
            return result;
        }

        public void PostProcessing(ABS_PositionSearchResult p_Result)
        {
            switch (m_ActiveBuildingElement.PositionSearchAlgorithm)
            {
                case ABS_PositionSearchAlgorithm.AdvancedGrid:
                    {
                        ABS_PositionValidationData_AdvancedGrid advancedResults = 
                            new ABS_PositionValidationData_AdvancedGrid(p_Result.ValidationResult);
                        p_Result.ValidationResult = advancedResults;

                        ABS_BuildingParent parent = m_Manager.GetBuildingParent();
                        if (!parent.AdvancedGridStabilityEnabled)
                        {
                            return;
                        }

                        ABS_AdvancedGridBuilder.CheckStabilityFeature(advancedResults, parent, m_ActiveBuildingElement);
                    }
                    break;
                case ABS_PositionSearchAlgorithm.Free:
                case ABS_PositionSearchAlgorithm.BasicGrid:
                case ABS_PositionSearchAlgorithm.SnapPointBased:
                default: return;
            }
        }

        public void MouseWheelChanged(in float p_Value)
        {
            m_PositionSearchAlgorithm.MouseWheelChanged(p_Value);
            if (m_ActiveBuildingElement.PositionSearchAlgorithm != ABS_PositionSearchAlgorithm.Free)
            {
                m_FreeBuilder.MouseWheelChanged(p_Value);
            }
        }

        public Vector3 GetParentPositionAlignment(ABS_BuildingElement p_TargetElement)
        {
            switch (p_TargetElement.PositionSearchAlgorithm)
            {
                case ABS_PositionSearchAlgorithm.SnapPointBased:
                    return m_SnapPointBasedBuilder.GetParentPositionAlignment(p_TargetElement); ;
                case ABS_PositionSearchAlgorithm.AdvancedGrid:
                    return m_AdvancedGridBuilder.GetParentPositionAlignment(p_TargetElement);
                case ABS_PositionSearchAlgorithm.BasicGrid:
                    return m_BasicGridBuilder.GetParentPositionAlignment(p_TargetElement);
                case ABS_PositionSearchAlgorithm.Free:
                    return m_FreeBuilder.GetParentPositionAlignment(p_TargetElement);
            }
            return Vector3.zero;
        }

        public void ResetManager()
        {
            m_ActiveBuildingElement = null;
            m_PositionSearchAlgorithm = null;
            ClearCache();
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Statistics
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public ulong StatisticsCounterRaycast
        {
            get 
            {
                return m_SnapPointBasedBuilder.StatisticsCounterRaycast
                       + m_AdvancedGridBuilder.StatisticsCounterRaycast
                       + m_BasicGridBuilder.StatisticsCounterRaycast
                       + m_FreeBuilder.StatisticsCounterRaycast;
            }
        }
        public ulong StatisticsCounterOverlapCheck
        {
            get
            {
                return m_SnapPointBasedBuilder.StatisticsCounterOverlapCheck
                       + m_AdvancedGridBuilder.StatisticsCounterOverlapCheck
                       + m_BasicGridBuilder.StatisticsCounterOverlapCheck
                       + m_FreeBuilder.StatisticsCounterOverlapCheck;
            }
        }

        public void StatisticsReset()
        {
            m_SnapPointBasedBuilder.StatisticsReset();
            m_AdvancedGridBuilder.StatisticsReset();
            m_BasicGridBuilder.StatisticsReset();
            m_FreeBuilder.StatisticsReset();
        }

        public void TriggerStatisticsPrint()
        {
            m_SnapPointBasedBuilder.TriggerStatisticsPrint();
            m_AdvancedGridBuilder.TriggerStatisticsPrint();
            m_BasicGridBuilder.TriggerStatisticsPrint();
            m_FreeBuilder.TriggerStatisticsPrint();
        }
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public void OnDrawGizmos(in ABS_ProjectSettings p_ProjectSettings, in ABS_PositionSearchResult p_PositionSearchResult)
        {
            if (m_PositionSearchAlgorithm != m_FreeBuilder)
            {
                m_PositionSearchAlgorithm.OnDrawGizmos(p_ProjectSettings, p_PositionSearchResult);
            }

            m_FreeBuilder.OnDrawGizmos(p_ProjectSettings, p_PositionSearchResult);
        }
#endif
    }
}


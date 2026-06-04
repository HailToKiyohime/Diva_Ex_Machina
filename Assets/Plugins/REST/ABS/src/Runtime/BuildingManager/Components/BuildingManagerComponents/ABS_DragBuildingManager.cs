//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_DragBuildingManager : ABS_BuildingManagerComponentBaseMonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        ABS_TemporaryBuildingElementManager m_TemporaryBuildingElementManager = null;
        private ABS_DragBuildingAdvancedGridHelper m_AdvancedGridHelper = null;
        private bool m_AllowMixedAxisDragBuilding = false;

        private ABS_BuilderBaseSettings m_Settings = null;
        private ABS_ActionHistory m_ActionHistory = null; 
        private ABS_BuildingParent m_BuildingParent = null;

        private ABS_BuildingElement m_ActiveBuildingElement = null;
        private ABS_PositionSearchResult m_PositionSearchResult = null;
        private Vector3 m_DragGridSize = Vector3.one;

        private int m_LastNumberOfXDimension = 1;
        private int m_LastNumberOfZDimension = 1;
        private uint m_MaxElementCount = 0;

#if UNITY_EDITOR
        private ulong m_StatisticsCounterRaycast = 0;
        private ulong m_StatisticsCounterOverlapCheck = 0;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters / Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_PositionSearchResult PositionSearchResult
        {
            set
            {
                m_PositionSearchResult = value;
            }
        }

#if UNITY_EDITOR
        public ulong StatisticsCounterRaycast
        {
            get { return m_StatisticsCounterRaycast; }
        }
        public ulong StatisticsCounterOverlapCheck
        {
            get { return m_StatisticsCounterOverlapCheck; }
        }

        public void StatisticsReset()
        {
            m_StatisticsCounterRaycast = 0;
            m_StatisticsCounterOverlapCheck = 0;
        }
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_DragBuildingManager() : base()
        {
        }

        public void Init(
            ABS_IBuildingManagerInternalInterface p_Manager, 
            ABS_BuildingManagerTracker p_Tracker, 
            ABS_ActionHistory p_ActionHistory,
            ABS_BuildingParent p_BuildingParent,
            ABS_TemporaryBuildingElementManager p_TemporaryBuildingElementManager)
        {
            base.Init(p_Manager, p_Tracker);
            m_ActionHistory = p_ActionHistory;
            m_BuildingParent = p_BuildingParent;
            m_TemporaryBuildingElementManager = p_TemporaryBuildingElementManager;

            m_AdvancedGridHelper = new ABS_DragBuildingAdvancedGridHelper(m_TemporaryBuildingElementManager.Elements);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  BuildingElenet Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void ResetPendingBuildingElement(ABS_BuildingElement p_TargetBuidlignElement)
        {
            m_TemporaryBuildingElementManager.ResetTemoraryElementsList(false, false);
            m_TemporaryBuildingElementManager.ResetTemporaryBuildingElement(p_TargetBuidlignElement);

            m_AllowMixedAxisDragBuilding = false;

            //Save new ABS_Building Element
            m_ActiveBuildingElement = p_TargetBuidlignElement;
            if (m_ActiveBuildingElement != null)
            {
                if (m_Manager.IsSandbox)
                {
                    m_MaxElementCount = uint.MaxValue;
                }
                else
                {
                    m_MaxElementCount = m_Tracker.GetMaximumCountOfBuildingElements(m_ActiveBuildingElement);
                }

                m_Settings = m_ActiveBuildingElement.PositionAlgorithmSettings;

                if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.BasicGrid)
                {
                    m_DragGridSize = (m_Settings as ABS_BasicGridBuilderSettings).GridSize;
                }
                else if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    m_AdvancedGridHelper.ActiveBuildingElement = m_ActiveBuildingElement;

                    m_AllowMixedAxisDragBuilding = m_ActiveBuildingElement.AllowMixedAxisDragBuilding
                        && (m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.Wall
                            || m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.EdgeHorizontal);

                    if (m_AllowMixedAxisDragBuilding)
                    {
                        m_DragGridSize = (m_Settings as ABS_AdvancedGridBuilderSettings).MixedAxisGridSize;
                    }
                    else
                    {
                        m_DragGridSize = (m_Settings as ABS_AdvancedGridBuilderSettings).GridSize;
                    }
                }
                else
                {
                    m_DragGridSize = m_ActiveBuildingElement.Dimension;
                }

                m_TemporaryBuildingElementManager.AllowMixedAxisDragBuilding = m_AllowMixedAxisDragBuilding;
                m_TemporaryBuildingElementManager.CreateFirstElement(m_MaxElementCount);
            }
            else
            {
                m_MaxElementCount = 0;
                m_Settings = null;
            }

            m_LastNumberOfXDimension = m_TemporaryBuildingElementManager.GetDimensionX();
            m_LastNumberOfZDimension = m_TemporaryBuildingElementManager.GetDimensionZ(0);
        }

        public void RefreshMaxBuildingElementCount(in uint p_MaxElementCount)
        {
            if (m_MaxElementCount == p_MaxElementCount)
            {
                return;
            }

            CheckBlocking();
        }

        public void Build()
        {
            int xByHitpoint = 0, zByHitpoint = 0;
            GetTargetDimension(out xByHitpoint, out zByHitpoint);

            if (m_LastNumberOfXDimension == xByHitpoint && m_LastNumberOfZDimension == zByHitpoint)
            {
                return;
            }
            else
            {
                int xByHitpointAligned = (xByHitpoint < 0 ? (xByHitpoint - m_TemporaryBuildingElementManager.FirstElementXIndex) : (xByHitpoint + m_TemporaryBuildingElementManager.FirstElementXIndex));
                int xByHitpoint_ABS = Math.Abs(xByHitpointAligned);
                int zByHitpoint_ABS = Math.Abs(zByHitpoint);

                RemoveAndRefreshDimension(xByHitpoint, zByHitpoint, xByHitpoint_ABS, zByHitpoint_ABS);
                m_TemporaryBuildingElementManager.AddColumnsUntil(xByHitpoint_ABS);

                switch (m_ActiveBuildingElement.DragBuildingBehaviour)
                {
                    case ABS_DragBuildingBehaviour.FilledUp:

                        BuildFilledUp(xByHitpointAligned, zByHitpoint, xByHitpoint_ABS, zByHitpoint_ABS);
                        break;
                    case ABS_DragBuildingBehaviour.Frame:
                        BuildFrame(xByHitpointAligned, zByHitpoint, xByHitpoint_ABS, zByHitpoint_ABS);
                        break;
                    case ABS_DragBuildingBehaviour.HalfFrame:
                        BuildHalfFrame(xByHitpointAligned, zByHitpoint, xByHitpoint_ABS, zByHitpoint_ABS);
                        break;
                    case ABS_DragBuildingBehaviour.OneLine:
                        BuildOneLine(xByHitpointAligned, zByHitpoint, xByHitpoint_ABS, zByHitpoint_ABS);
                        break;
                }

                //Finalize the building process
                if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    m_AdvancedGridHelper.ParentNeighbourValidation(IsStabilityEnabled());
                }
                else if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.SnapPointBased)
                {
                    Debug.LogError("Not Implemented");
                }

                CheckBlocking();
            }
        }

        private void BuildFrame(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_XByHitpoint_ABS, in int p_ZByHitpoint_ABS)
        {
            m_TemporaryBuildingElementManager.RemoveInnerElements(p_XByHitpoint_ABS, p_ZByHitpoint_ABS);

            for (int x = 0; x < p_XByHitpoint_ABS; ++x)
            {
                if (x == 0 || x == p_XByHitpoint_ABS - 1)
                {
                    FillUpZColumn(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS);
                }
                else
                {
                    m_TemporaryBuildingElementManager.FillUpColumnWithNull(x, p_ZByHitpoint_ABS);

                    //check first element
                    CreateElement(p_XByHitpoint, p_ZByHitpoint, x, 0);

                    //check Last element
                    if (p_ZByHitpoint_ABS == 0)
                    {
                        continue;
                    }
                    CreateElement(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS - 1);
                }
            }
        }

        private void BuildOneLine(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_XByHitpoint_ABS, in int p_ZByHitpoint_ABS)
        {
            for (int x = 0; x < p_XByHitpoint_ABS; ++x)
            {
                FillUpZColumn(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS);
                if (p_ZByHitpoint_ABS > p_XByHitpoint_ABS
                    && m_ActiveBuildingElement.DragBuildingBehaviour == ABS_DragBuildingBehaviour.OneLine
                    && m_AllowMixedAxisDragBuilding)
                {
                    m_TemporaryBuildingElementManager.RemoveZElements(1, 0, p_ZByHitpoint_ABS);
                }
            }
        }

        private void BuildHalfFrame(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_XByHitpoint_ABS, in int p_ZByHitpoint_ABS)
        {
            m_TemporaryBuildingElementManager.RemoveInnerElements(p_XByHitpoint_ABS, p_ZByHitpoint_ABS);
            for (int x = 0; x < p_XByHitpoint_ABS; ++x)
            {
                m_TemporaryBuildingElementManager.FillUpColumnWithNull(x, p_ZByHitpoint_ABS);

                if (x == 0)
                {
                    if (p_XByHitpoint_ABS < p_ZByHitpoint_ABS)
                    {
                        FillUpZColumn(p_XByHitpoint, p_ZByHitpoint, 0, p_ZByHitpoint_ABS);
                    }
                    else
                    {
                        m_TemporaryBuildingElementManager.RemoveZElements(0, 1, p_ZByHitpoint_ABS);
                    }
                }
                else if (x == p_XByHitpoint_ABS - 1)
                {
                    if (p_XByHitpoint_ABS < p_ZByHitpoint_ABS)
                    {
                        m_TemporaryBuildingElementManager.RemoveZElements(x, 0, p_ZByHitpoint_ABS - 1);

                        //check Last element
                        if (p_ZByHitpoint_ABS == 0)
                        {
                            continue;
                        }
                        CreateElement(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS - 1);
                    }
                    else
                    {
                        FillUpZColumn(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS);
                    }
                }
                else
                {
                    if (p_XByHitpoint_ABS < p_ZByHitpoint_ABS)
                    {
                        m_TemporaryBuildingElementManager.RemoveZElements(x, 0, p_ZByHitpoint_ABS - 1);

                        //check Last element
                        if (p_ZByHitpoint_ABS == 0)
                        {
                            continue;
                        }
                        CreateElement(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS - 1);
                    }
                    else
                    {
                        m_TemporaryBuildingElementManager.RemoveZElements(x, 1, p_ZByHitpoint_ABS);
                        CreateElement(p_XByHitpoint, p_ZByHitpoint, x, 0);
                    }
                }
            }
        }

        private void BuildFilledUp(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_XByHitpoint_ABS, in int p_ZByHitpoint_ABS)
        {
            for (int x = 0; x < p_XByHitpoint_ABS; ++x)
            {
                FillUpZColumn(p_XByHitpoint, p_ZByHitpoint, x, p_ZByHitpoint_ABS);
            }
        }

        private void FillUpZColumn(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_CurrentXIndex, in int p_ZByHitpoint_ABS)
        {
            for (int z = 0; z < p_ZByHitpoint_ABS; ++z)
            {
                //Ignore the first element becasue that must be available already
                if ((!(p_CurrentXIndex == m_TemporaryBuildingElementManager.FirstElementXIndex && z == 0)) 
                    && m_TemporaryBuildingElementManager.GetDimensionZ(p_CurrentXIndex) <= z)
                {
                    m_TemporaryBuildingElementManager.AddRowElement(p_CurrentXIndex);
                    CreateElementImpl(p_XByHitpoint, p_ZByHitpoint, p_CurrentXIndex, z);
                }
                else
                {
                    CreateElement(p_XByHitpoint, p_ZByHitpoint, p_CurrentXIndex, z);
                }
            }
        }

        private void CreateElement(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_X, in int p_Z)
        {
            ABS_TemporaryBuildingElement element = m_TemporaryBuildingElementManager.Elements[p_X][p_Z];
            if (element == null)
            {
                CreateElementImpl(p_XByHitpoint, p_ZByHitpoint, p_X, p_Z);
            }
            else
            {
                element.ValidationData.m_Result.DragBuildingValidation_ValidatedByNeighbour = ABS_PositionValidationResult.ResultOptions.Unkown;

                if (element.TargetBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    ABS_PositionValidationData_AdvancedGrid validationData = element.ValidationData as ABS_PositionValidationData_AdvancedGrid;
                    validationData.m_DragBuildingTemporaryState = ABS_PositionValidationData_AdvancedGrid.DragBuildingTemporaryState.CheckNeeded;

                    if (m_PositionSearchResult.TargetBuilding != null)
                    {
                        ABS_AdvancedGridBuilding building = m_PositionSearchResult.TargetBuilding as ABS_AdvancedGridBuilding;
                        if (building.EnableStability)
                        {
                            validationData.m_DragBuildingTemporaryStability = validationData.m_Stability;
                        }
                    }
                    else
                    {
                        if (m_BuildingParent.AdvancedGridStabilityEnabled)
                        {
                            validationData.m_DragBuildingTemporaryStability = m_BuildingParent.AdvancedGridStabilityLevel;
                        }
                    }
                }
            }
        }

        private void CreateElementImpl(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_CurrentXIndex, in int p_CurrentZIndex)
        {
            bool res = m_TemporaryBuildingElementManager.CreateElement(p_XByHitpoint, p_ZByHitpoint, p_CurrentXIndex, p_CurrentZIndex, m_DragGridSize);

            if (res && m_Settings.CheckUnderGroundPosition)
            {
                UndergroundNeighbourValidation(p_CurrentXIndex, p_CurrentZIndex);
            }
        }

        private void RemoveAndRefreshDimension(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_XByHitpoint_ABS, in int p_ZByHitpoint_ABS)
        {
            int dimensionX = m_TemporaryBuildingElementManager.GetDimensionX();
            int dimensionZ = m_TemporaryBuildingElementManager.GetDimensionZ(m_TemporaryBuildingElementManager.FirstElementXIndex);

            //First remove the X dimension line if it is needed
            if ((p_XByHitpoint < 0 && m_LastNumberOfXDimension > 0) || (p_XByHitpoint > 0 && m_LastNumberOfXDimension < 0))
            {
                if (m_AllowMixedAxisDragBuilding)
                {
                    m_TemporaryBuildingElementManager.ClearColumn(0);
                }
                //Remove every line but left one or two.
                //If the m_AllowMixedAxisDragBuilding is true then the m_TemporaryBuildingElementManager.FirstElementXIndex is 1
                //  and in that case 2 line should be left.
                m_TemporaryBuildingElementManager.RemoveColumns(dimensionX - 1 - m_TemporaryBuildingElementManager.FirstElementXIndex);
            }
            else if (p_XByHitpoint_ABS < dimensionX)
            {
                m_TemporaryBuildingElementManager.RemoveColumns(dimensionX - p_XByHitpoint_ABS);
            }

            //Second remove the Z dimension line if it is needed 
            if ((p_ZByHitpoint < 0 && m_LastNumberOfZDimension > 0) || (p_ZByHitpoint > 0 && m_LastNumberOfZDimension < 0))
            {
                m_TemporaryBuildingElementManager.RemoveRows(dimensionZ - 1);
            }
            else if (p_ZByHitpoint_ABS < dimensionZ)
            {
                m_TemporaryBuildingElementManager.RemoveRows(dimensionZ - p_ZByHitpoint_ABS);
            }

            //reset by the new dimensions
            m_LastNumberOfXDimension = p_XByHitpoint;
            m_LastNumberOfZDimension = p_ZByHitpoint;
        }

        //return true if the element is NOT underground from the Neighbours POV 
        private void UndergroundNeighbourValidation(in int p_X, in int p_Z)
        {
            ABS_PositionValidationData resultData = m_TemporaryBuildingElementManager.Elements[p_X][p_Z].ValidationData;
            if (resultData == null)
            {
                return;
            }
            ABS_PositionValidationResult result = resultData.m_Result;
            if (result == null)
            {
                return;
            }

            Vector3 targetPosition = m_TemporaryBuildingElementManager.Elements[p_X][p_Z].transform.position;
            if (targetPosition == null)
            {
                return;
            }

            //Check X, Z - 1
            if (p_Z > 0)
            {
                IsUnderGroundFromNegihborPOV(p_X, p_Z - 1, targetPosition, result);
            }

            //Check X - 1, Z
            if (p_X > 0)
            {
                IsUnderGroundFromNegihborPOV(p_X - 1, p_Z, targetPosition, result);
            }

            //Check X + 1, Z
            if (m_TemporaryBuildingElementManager.GetDimensionX() > p_X + 1
                && m_TemporaryBuildingElementManager.GetDimensionZ(p_X + 1) >= p_Z + 1)
            {
                IsUnderGroundFromNegihborPOV(p_X + 1, p_Z, targetPosition, result);
            }

            //Check X, Z + 1
            if (m_TemporaryBuildingElementManager.GetDimensionZ(p_X) > p_Z + 1)
            {
                IsUnderGroundFromNegihborPOV(p_X, p_Z + 1, targetPosition, result);
            }
        }

        //Return true id the element is underground fromt he Neighbour POV
        private void IsUnderGroundFromNegihborPOV(in int p_NegihborX, in int p_NegihborZ, Vector3 p_TargetGlobalPos, ABS_PositionValidationResult p_Result)
        {
            ABS_PositionValidationData negihborResultData = m_TemporaryBuildingElementManager.GetValidationResult(p_NegihborX, p_NegihborZ);
            if (negihborResultData == null)
            {
                return;
            }
            ABS_PositionValidationResult negihborResult = negihborResultData.m_Result;
            if (negihborResult == null)
            {
                return;
            }

            ABS_TemporaryBuildingElement negihborElement = m_TemporaryBuildingElementManager.GetElement(p_NegihborX, p_NegihborZ);
            if (negihborElement == null)
            {
                return;
            }

            if (p_Result.IsSuccessFull()
                && negihborResult.CheckSingleElementValidation()
                && negihborResult.CheckParentBuildingValidation(
                        p_IgnoreInvalidPosition : true,
                        p_IgnorePositionRules : false,
                        p_IgnoreShouldSnapToFoundation : false)
                && (negihborResult.DragBuildingValidation_ValidatedByNeighbour_UnderGround == ABS_PositionValidationResult.ResultOptions.Validated)
                    || negihborResult.DragBuildingValidation_UnderGround != ABS_PositionValidationResult.ResultOptions.Failed)
            {
                Vector3 neighbourPosition = negihborElement.transform.position;

#if UNITY_EDITOR
                ++m_StatisticsCounterRaycast;
#endif
                RaycastHit hit = new RaycastHit();
                bool underground = ABS_Raycaster.IsUnderground(p_TargetGlobalPos, ref neighbourPosition, ref hit, m_Settings.LayerCollection.LayerOfGround);
                if (underground)
                {
                    negihborResult.DragBuildingValidation_UnderGround = ABS_PositionValidationResult.ResultOptions.Failed;
                }
                else
                {
                    negihborResult.DragBuildingValidation_ValidatedByNeighbour_UnderGround = ABS_PositionValidationResult.ResultOptions.Validated;
                }
            }
        }

        private void CheckBlocking()
        {
            uint FreeSpace = (m_PositionSearchResult != null 
                                && m_PositionSearchResult.TargetBuilding != null
                                && m_PositionSearchResult.TargetBuilding.FreeSpace < m_MaxElementCount)
                                ? m_PositionSearchResult.TargetBuilding.FreeSpace : m_MaxElementCount;

            uint unblockedBECounter = 0;
            for (int x = 0; x < m_TemporaryBuildingElementManager.GetDimensionX(); ++x)
            {
                for (int z = 0; z < m_TemporaryBuildingElementManager.GetDimensionZ(x); ++z)
                {
                    ABS_TemporaryBuildingElement element = m_TemporaryBuildingElementManager.GetElement(x, z);
                    if (element == null)
                    {
                        continue;
                    }
                    ABS_PositionValidationResult result = m_TemporaryBuildingElementManager.GetValidationResult(x, z).m_Result;

                    if (result.IsFailed()
                        || unblockedBECounter >= FreeSpace
                        || (m_ActiveBuildingElement.ShouldOverride 
                            && result.ParentBuildingValidation_ValidatedByOverrideSettings != ABS_PositionValidationResult.ResultOptions.Validated))
                    {
                        element.SetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_LOGIC);
                    }
                    else
                    {
                        element.UnSetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_LOGIC);
                    }

                    if (m_Manager.IsSandbox || unblockedBECounter < m_MaxElementCount)
                    {
                        element.UnSetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_NOT_ENOUGH_MATERIAL);
                    }
                    else
                    {
                        element.SetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_NOT_ENOUGH_MATERIAL);
                    }
                    
                    if (element.Avaliable())
                    {
                        ++unblockedBECounter;
                    }
                }
            }

            m_Tracker.CurrentValidBuildingElements(unblockedBECounter);
        }

        private void GetTargetDimension(out int p_X, out int p_Z)
        {
            Vector3 localHitPoint = m_TemporaryBuildingElementManager.GetLocalRaycastPosition();
            Vector3 halfDragGridSize = m_DragGridSize / 2f;
            bool oneline = m_ActiveBuildingElement.DragBuildingBehaviour == ABS_DragBuildingBehaviour.OneLine;
            float absLocalX = Math.Abs(localHitPoint.x);
            float absLocalZ = Math.Abs(localHitPoint.z);

            //Calcualte X -----------------------------------------------------------------------------------------------
            if (!m_ActiveBuildingElement.EnabledDragBuildingX 
                || absLocalX <= halfDragGridSize.x 
                || (oneline && absLocalX < absLocalZ))
            {
                p_X = 1;
            }
            else
            {
                float localHitPointRange = (absLocalX - halfDragGridSize.x) / m_DragGridSize.x;
                int result = 2 + (int)Math.Floor(localHitPointRange);
                p_X = (localHitPoint.x < 0) ? -1 * result : result;
            }

            if (m_Settings.DragBuildingLimitX && Math.Abs(p_X) > m_Settings.DragBuildingLimitXAmount)
            {
                p_X = (int)m_Settings.DragBuildingLimitXAmount * (p_X < 0 ? -1 : 1);
            }

            if (m_AllowMixedAxisDragBuilding && !oneline && p_X % 2 != 0)
            {
                ++p_X;
            }

            //Calcualte Z -----------------------------------------------------------------------------------------------
            if (!m_ActiveBuildingElement.EnabledDragBuildingZ 
                || absLocalZ <= halfDragGridSize.z 
                || (oneline && absLocalX >= absLocalZ))
            {
                p_Z = 1;
            }
            else
            {
                float localHitPointRange = (absLocalZ - halfDragGridSize.z) / m_DragGridSize.z;
                int result = 2 + (int)Math.Floor(localHitPointRange);
                p_Z = (localHitPoint.z < 0) ? -1 * result : result;
            }

            if (m_Settings.DragBuildingLimitZ && Math.Abs(p_Z) > m_Settings.DragBuildingLimitZAmount)
            {
                p_Z = (int)m_Settings.DragBuildingLimitZAmount * (p_Z < 0 ? -1 : 1);
            }

            if (m_AllowMixedAxisDragBuilding && !oneline && p_Z % 2 == 0)
            {
                ++p_Z;
            }
        }

        public void Place()
        {
            if (m_PositionSearchResult == null)
            {
                REST_Logging.Error($"{this}", "Missing position result.");
                return;
            }

            //Get Building
            ABS_Building building = null;
            bool isNewBuilding = GetTartegBuilding(out building);

            //Setup the action for the history feature
            ABS_BuildActionBuildingData actionBuildingData = new ABS_BuildActionBuildingData();
            actionBuildingData.Init(building);
            ABS_BuildAction action = new ABS_BuildAction(m_Tracker, m_ActiveBuildingElement, actionBuildingData, isNewBuilding);

            List<ABS_BuildingElement> placedBuildingElements = new List<ABS_BuildingElement>();
            List<(ABS_TemporaryBuildingElement, ABS_PositionValidationResult)> canBePlacedBuildingElements = new List<(ABS_TemporaryBuildingElement, ABS_PositionValidationResult)>();
            for (int i = 0; i < m_TemporaryBuildingElementManager.GetDimensionX(); ++i)
            {
                for (int j = 0; j < m_TemporaryBuildingElementManager.GetDimensionZ(i); ++j)
                {
                    ABS_TemporaryBuildingElement element = m_TemporaryBuildingElementManager.GetElement(i, j);
                    if (element == null 
                        || !element.Avaliable() 
                        || element.ValidationData.IsFailed() 
                        || !TrackerCustomValidation(element, building))
                    {
                        continue;
                    }

                    ABS_BuildingElement newElement = null;
                    ABS_BuildActionElementData buildActionData = PlaceImpl(element, building, out newElement);
                    if (buildActionData == null || newElement == null)
                    {
                        REST_Logging.Error($"{this}", $"Error during placement of the element : {element.name}" +
                            $"\n x : {i}  z : {j}" +
                            $"\n Building: {building.name}");
                        continue;
                    }

                    placedBuildingElements.Add(newElement);

                    CheckAttachmentConnection(newElement, buildActionData, element.ValidationData);

                    //Save build step to the History's build action
                    buildActionData.AddBuildingElement(newElement, actionBuildingData);
                    action.AddData(buildActionData);
                }
            }

            m_ActionHistory.AddAction(action);

            if (placedBuildingElements.Count > 0)
            {
                m_Tracker.BuildingElementPlaced(placedBuildingElements);
            }

            m_TemporaryBuildingElementManager.ResetTemoraryElementsList(false, true);
            if (m_Manager.BuildMode == ABS_BuildingManagerBuildMode.Continues)
            {
                m_TemporaryBuildingElementManager.CreateFirstElement(m_MaxElementCount);
            }
        }

        public ABS_BuildActionElementData PlaceImpl(
            ABS_TemporaryBuildingElement p_TMPElement, 
            ABS_Building p_Parent,
            out ABS_BuildingElement p_NewElement)
        {
            ABS_BuildingElement targetBuildingElement = p_TMPElement.TargetBuildingElement;

            if (targetBuildingElement.FinalElement == null)
            {
                p_NewElement = targetBuildingElement;
            }
            else
            {
                p_NewElement = Instantiate(targetBuildingElement.FinalElement, this.transform);
                p_NewElement.State = ABS_BuildingElementState.PENDING;
            }

            //The TemporaryBuildingManager will be plalced into the position so first element will be in the (0;0;0) position
            //So the element's world position shoould be transformed into the parents localposition
            Vector3 parentLocalPosition = p_Parent.Transform.InverseTransformPoint(p_TMPElement.transform.position);
            Vector3 parentLocalRotation = REST_QuaternionHelper.ConvertGlobalRotationIntoLocal(p_Parent.Transform, p_TMPElement.transform.eulerAngles);

            SetStability(p_NewElement, p_Parent, p_TMPElement.ValidationData);

            return p_Parent.AddBuildingElement(
                        p_Tracker: m_Tracker,
                        p_TriggeredByHistory: false,
                        p_NewElement: p_NewElement,
                        p_LocalPosition: parentLocalPosition,
                        p_LocalEulerAngles: parentLocalRotation,
                        p_Force: true,
                        p_DestroyOld: true);
        }

        private void SetStability(
            in ABS_BuildingElement p_BuildingElement,
            in ABS_Building p_Buildling,
            in ABS_PositionValidationData p_ValidationResult)
        {
            if (m_ActiveBuildingElement.PositionSearchAlgorithm != ABS_PositionSearchAlgorithm.AdvancedGrid)
            {
                return;
            }

            ABS_AdvancedGridBuilding advancedGridBuilding = (p_Buildling as ABS_AdvancedGridBuilding);
            if (!advancedGridBuilding.EnableStability)
            {
                return;
            }

            ABS_PositionValidationData_AdvancedGrid resultData = p_ValidationResult as ABS_PositionValidationData_AdvancedGrid;
            if (resultData != null)
            {
                p_BuildingElement.Stable = resultData.m_Stable;
                p_BuildingElement.StabilityLevel = resultData.m_DragBuildingTemporaryStability;
            }
        }

        private void CheckAttachmentConnection(
            in ABS_BuildingElement p_BuildingElement,
            in ABS_BuildActionElementData p_BuildActionData,
            in ABS_PositionValidationData p_ValidationResult)
        {
            if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.Free
                    && (m_ActiveBuildingElement.PositionAlgorithmSettings as ABS_FreeBuilderSettings).EnableAttachementConnection
                    && p_ValidationResult.m_ElementTarget_BuildOnTopOfElement != null)
            {
                p_ValidationResult.m_ElementTarget_BuildOnTopOfElement.ConnectElement(ABS_BuildingElementConnectionType.Attachment, p_BuildingElement);

                ABS_ActionElementConnectionData connectionData = new ABS_ActionElementConnectionData(ABS_BuildingElementConnectionType.Attachment);
                connectionData.AddBuildingElement(p_ValidationResult.m_ElementTarget_BuildOnTopOfElement);
                p_BuildActionData.AddConnectionTargetData(connectionData);
            }
        }

        private bool GetTartegBuilding(out ABS_Building p_Building)
        {
            if (m_PositionSearchResult == null)
            {
                p_Building = null;
                return false;
            }
            else if (m_PositionSearchResult.TargetBuilding != null)
            {
                p_Building = m_PositionSearchResult.TargetBuilding;
                return false;
            }
            else
            {
                p_Building = m_Manager.GetParentForNewBuildingElement();
                return true;
            }
        }

        private bool IsStabilityEnabled ()
        {
            if (m_PositionSearchResult != null && m_PositionSearchResult.TargetBuilding != null)
            {
                return (m_PositionSearchResult.TargetBuilding as ABS_AdvancedGridBuilding).EnableStability;
            }
            else
            {
                return m_BuildingParent.AdvancedGridStabilityEnabled;
            }
        }

        private bool TrackerCustomValidation(ABS_TemporaryBuildingElement p_Element, ABS_Building p_Building)
        {
            return m_Tracker.BeforePlace(p_Element.TargetBuildingElement,
                                        p_Element.TargetBuildingElement.transform.position,
                                        transform.localRotation,
                                        p_Building);
        }
    }
}

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
    public class ABS_BasicGridBuilder : ABS_BuilderBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BasicGridBuilderSettings m_BasicGridBuilderSettings = null;
        private ABS_BasicGridBuilding m_ParentBuilding = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BasicGridBuilder(ABS_IBuildingManagerInternalInterface p_Manager, ABS_BuildingManagerTracker p_Tracker)
            : base(p_Manager, p_Tracker) 
        {
            m_ParentBuilding = p_Manager.GlobalBasicGridParent;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public override void StatisticsResetImpl()
        {
            //nothing
        }

        public override void TriggerStatisticsPrint()
        {
            //nothing
        }
        public override void OnDrawGizmosImpl(in ABS_ProjectSettings p_ProjectSettings, in ABS_PositionSearchResult p_PositionSearchResult)
        {
            //nothing
        }
#endif

        public override Vector3 GetParentPositionAlignment(ABS_BuildingElement p_Element)
        {
            return Vector3.zero;
        }

        public override ABS_BuilderBaseSettings Settings
        {
            set 
            {
                m_BasicGridBuilderSettings = (value as ABS_BasicGridBuilderSettings); 
                base.Settings = value;
            }
        }

        protected override void SearchPositionImpl(in bool m_IsMain, in ABS_PositionSearchResult p_ResultData)
        {
            ABS_PositionValidationData validationResult = new ABS_PositionValidationData();
            p_ResultData.ValidationResult = validationResult;

            if (m_ParentBuilding == null)
            {
                m_ParentBuilding = m_Manager.GetParentForNewBuildingElement() as ABS_BasicGridBuilding;
            }

            Vector3 raycastPosition = m_Manager.GetRaycastHitOrEndPosition();

            HashSet<ABS_Building> buildings = new HashSet<ABS_Building>();
            List<ABS_BuildingElement> buildingElements = new List<ABS_BuildingElement>();
            ABS_BuildingElement preBuiltTarget = null;
            bool isPreBuiltFound = SearchForNearBuildingElements(
                ABS_PositionSearchAlgorithm.BasicGrid,
                raycastPosition,
                m_Settings.SearchRadius,
                ref buildingElements,
                ref buildings,
                ref preBuiltTarget);

            if (isPreBuiltFound)
            {
                GetPreBuiltPositionResultData(preBuiltTarget, p_ResultData);
                return;
            }

            bool isHalfWayX = false;
            bool isHalfWayZ = false;
            p_ResultData.WorldPosition = ABS_BasicGridBuilder.GetGridPosition(raycastPosition, 
                                                                         m_ActiveBuildingElement, 
                                                                         m_BasicGridBuilderSettings, 
                                                                         ref p_ResultData.IsAlignedToGroundRef, 
                                                                         out isHalfWayX, 
                                                                         out isHalfWayZ);

            p_ResultData.Rotation = UnityEngine.Quaternion.Euler(Vector3.up * CalcualteMixedRotation(90.0f));

            BaseElementValidation(p_ResultData.ValidationResult, m_ParentBuilding, p_ResultData.WorldPosition, p_ResultData.Rotation, false, true);
            if (CheckValidationResult(p_ResultData.ValidationResult.m_Result))
            {
                p_ResultData.Result = ABS_PositionSearchResult.ResultType.Success;
            }
            else
            {
                p_ResultData.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
            }

            if (p_ResultData.Result == ABS_PositionSearchResult.ResultType.Success)
            {
                Vector3 checkedPosition = m_ParentBuilding.transform.InverseTransformPoint(p_ResultData.WorldPosition);
                ABS_PositionValidationResult parentValidationResult = new ABS_PositionValidationResult();
                m_ParentBuilding.ValidatePosition(p_ResultData.ValidationResult, checkedPosition, Quaternion.identity, m_ActiveBuildingElement);
                if (CheckValidationResult(parentValidationResult))
                {
                    p_ResultData.ValidationResult.m_Result.Merge(parentValidationResult);
                    p_ResultData.Result = ABS_PositionSearchResult.ResultType.Success;
                }
                else
                {
                    p_ResultData.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;

                    //There is a chance when the raycast hit perfectily on the grid edge.
                    //In this case because of the round of the integer numbers the algorithm cannot decide where it should place the element.
                    //For example there is a grid with 1 size and a cube with size 1.
                    // the raycast will hit the cube's surface at the edge of the grid.
                    //Then even if you looking at an element because of this unfortunate situation the calcualtion will said that the right position
                    //will be the same as teh cube what you looking.
                    //We want that when I'm looking at an element I want to place the next one next to that element.
                    //But in this case the element tiring to snap right into the other cube.
                    //When it happens so the raycast is perfectly on the edge we check that which position is empty and we positioning there.
                    if (isHalfWayX)
                    { 
                        Vector3 correctedPosition = raycastPosition.x > p_ResultData.WorldPosition.x ?
                            p_ResultData.WorldPosition + (Vector3.right * m_BasicGridBuilderSettings.GridSize.x) :
                            p_ResultData.WorldPosition - (Vector3.right * m_BasicGridBuilderSettings.GridSize.x);
                        Vector3 correctedLocalPosition = m_ParentBuilding.transform.InverseTransformPoint(correctedPosition);
                        m_ParentBuilding.ValidatePosition(p_ResultData.ValidationResult, correctedLocalPosition, Quaternion.identity, m_ActiveBuildingElement);
                        if (p_ResultData.ValidationResult.m_Result.IsSuccessFull())
                        {
                            p_ResultData.WorldPosition = correctedPosition;
                            p_ResultData.Result = ABS_PositionSearchResult.ResultType.Success;
                        }
                    }
                    else if (isHalfWayZ)
                    {
                        Vector3 correctedPosition = raycastPosition.z > p_ResultData.WorldPosition.z ?
                            p_ResultData.WorldPosition + (Vector3.forward * m_BasicGridBuilderSettings.GridSize.z) :
                            p_ResultData.WorldPosition - (Vector3.forward * m_BasicGridBuilderSettings.GridSize.z);

                        Vector3 correctedLocalPosition = m_ParentBuilding.transform.InverseTransformPoint(correctedPosition);
                        m_ParentBuilding.ValidatePosition(p_ResultData.ValidationResult, correctedLocalPosition, Quaternion.identity, m_ActiveBuildingElement);
                        if (p_ResultData.ValidationResult.m_Result.IsSuccessFull())
                        {
                            p_ResultData.WorldPosition = correctedPosition;
                            p_ResultData.Result = ABS_PositionSearchResult.ResultType.Success;
                        }
                    }
                }
            }

            if (p_ResultData.Result == ABS_PositionSearchResult.ResultType.Success)
            {
                if (!m_Tracker.PositionCustomValidation(m_ActiveBuildingElement, p_ResultData.WorldPosition, raycastPosition, p_ResultData.Rotation))
                {
                    p_ResultData.ValidationResult.m_Result.CustomElementValidation = ABS_PositionValidationResult.ResultOptions.Failed;
                    //if the custom validation failed the result will be overwritten
                    p_ResultData.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
                }
                else if (IsBlockingNeededBasedOnValidationLogic(p_ResultData.ValidationResult.m_Result))
                {
                    p_ResultData.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
                }
            }
        }

        protected override bool CanSnapToElement(ABS_BuildingElement p_Element)
        {
            return true;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Static functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static Vector3 GetGridPosition(Vector3 p_RaycastPosition, in ABS_BuildingElement p_BuildingElement, ref bool p_IsAlignedToGround)
        {
            ABS_BasicGridBuilderSettings settings = p_BuildingElement.PositionAlgorithmSettings as ABS_BasicGridBuilderSettings;
            if (settings == null)
            {
                REST_Logging.Error("ABS_BasicGridBuilder", $"Can not get the setting of ABS_BuildingElement : {p_BuildingElement.name}");
                return p_RaycastPosition;
            }

            bool isHalfWayX = false;
            bool isHalfWayZ = false;
            return GetGridPosition(p_RaycastPosition, p_BuildingElement, settings, ref p_IsAlignedToGround, out isHalfWayX, out isHalfWayZ);
        }

        public static Vector3 GetGridPosition(in Vector3 p_RaycastPosition, 
                                              in ABS_BuildingElement p_BuildingElement, 
                                              in ABS_BasicGridBuilderSettings p_Settings, 
                                              ref bool p_IsAlignedToGround, 
                                              out bool p_IsHalfWayX, 
                                              out bool p_IsHalfWayZ)
        {
            Vector3 gridSize = p_Settings.GridSize;
            float x = p_RaycastPosition.x;
            float z = p_RaycastPosition.z;
            float y = 0f;

            x = gridSize.x * (float)Math.Round(x / gridSize.x, 0);
            z = gridSize.z * (float)Math.Round(z / gridSize.z, 0);

            switch (p_Settings.VerticalGridPlacement)
            {
                case VerticalGridPlacement.FixedPosition:
                    y = p_Settings.VerticalGridFixedPosition;
                    break;
                case VerticalGridPlacement.RaycastPosition:
                    y = p_RaycastPosition.y + p_BuildingElement.Shifting.y;
                    break;
                case VerticalGridPlacement.AlignToGround:
                    y = GetAlignedPosition(p_RaycastPosition, ref p_IsAlignedToGround, p_Settings) + p_BuildingElement.Shifting.y;
                    break;
            }

            p_IsHalfWayX = Math.Abs(Math.Abs(p_RaycastPosition.x % gridSize.x) - (gridSize.x / 2)) < 0.001f;
            p_IsHalfWayZ = Math.Abs(Math.Abs(p_RaycastPosition.z % gridSize.z) - (gridSize.z / 2)) < 0.001f;

            return new Vector3(x, y, z);
        }

        private static float GetAlignedPosition(Vector3 p_RaycastPosition, ref bool p_IsAlignedToGround, in ABS_BasicGridBuilderSettings p_Settings)
        {
            Vector3 groundPosition = Vector3.zero;
            RaycastHit hit;
            if (ABS_Raycaster.GetGroundPosition(p_RaycastPosition + (Vector3.up * 0.001f), out groundPosition, p_Settings.LayerCollection.LayerOfGround, out hit))
            {
                p_IsAlignedToGround = true;
                return groundPosition.y;
            }
            else
            {
                return p_RaycastPosition.y;
            }
        }
    }
}
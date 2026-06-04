//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_SnapPointBasedBuilder : ABS_BuilderBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_SnapPointBasedBuilderSettings m_SnapPointBasedBuilderSettings;
        private ABS_SnapPointManager m_SnapPointManager = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_SnapPointBasedBuilder(ABS_IBuildingManagerInternalInterface p_Manager , ABS_BuildingManagerTracker p_Tracker)
            : base(p_Manager, p_Tracker) 
        {
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override ABS_BuilderBaseSettings Settings
        {
            set
            {
                m_SnapPointBasedBuilderSettings = (value as ABS_SnapPointBasedBuilderSettings);
                m_SnapPointManager = new ABS_SnapPointManager(m_SnapPointBasedBuilderSettings.ABS_SnapRelationshipList);
                base.Settings = value;
            }
        }

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
            Vector3 raycstPosition = m_Manager.GetRaycastHitOrEndPosition();
            if (p_ProjectSettings.PositionSearch_BuildCollider)
            {
                if (p_ProjectSettings.PositionSearch_CheckedBESnapPoints)
                {
                    Gizmos.color = p_ProjectSettings.PositionSearch_CheckedBESnapPointsColor;
                    float radius = m_SnapPointBasedBuilderSettings.BuildRadius * p_ProjectSettings.PositionSearch_SnapPointsArea / 100f;
                    Gizmos.DrawWireSphere(raycstPosition, radius);

                   /*Dictionary<Vector3, Color> snapPointsForGizmos = new Dictionary<Vector3, Color>();

                    List<ABS_BuildingElement> buildingElements = SearchForNearBuildingElements(raycstPosition, radius);
                    foreach (ABS_BuildingElement be in buildingElements)
                    {
                        Vector3[] tmpSnapPoints = AdvancedGridSnapPoints.GetSnapPointsForElements(be.ABS_AdvancedGridType, m_ActiveBuildingElement.AdvancedGridType);
                        if (tmpSnapPoints != null)
                        {
                            foreach (Vector3 pos in tmpSnapPoints)
                            {
                                Vector3 alignedPosition = be.transform.TransformPoint(pos * m_AdvancedGridBuilderSettings.GridSize);
                                float distance = Vector3.Distance(alignedPosition, raycstPosition);
                                if (distance <= radius)
                                {
                                    if (distance <= m_AdvancedGridBuilderSettings.m_BuildRadius)
                                    {
                                        snapPointsForGizmos[alignedPosition] = Color.green;
                                    }
                                    else
                                    {
                                        snapPointsForGizmos[alignedPosition] = Color.red;
                                    }
                                }
                            }
                        }
                    }

                    foreach (ABS_BuildingElement be in buildingElements)
                    {
                        if (be.PreBuilt && be.Guid == m_ActiveBuildingElement.Guid)
                        {
                            snapPointsForGizmos[be.transform.position] = Color.green;
                        }
                        else
                        {
                            snapPointsForGizmos[be.transform.position] = Color.red;
                        }
                    }

                    foreach ((Vector3 pos, Color color) in snapPointsForGizmos)
                    {
                        Gizmos.color = color;
                        Gizmos.DrawSphere(pos, 0.05f);
                    }*/
                }
            }
        }
#endif

        public override Vector3 GetParentPositionAlignment(ABS_BuildingElement p_Element)
        {
            return Vector3.zero;
        }

        protected override void SearchPositionImpl(in bool m_IsMain, in ABS_PositionSearchResult p_ResultData)
        {
            if (m_Manager.Raycaster.HitTransform == null && !m_Settings.AllowPositionSearchAtRaycastEndPosition)
            {
                p_ResultData.Result = ABS_PositionSearchResult.ResultType.FallbackIsNeeded;
                return;
            }
            else
            {
                Vector3 raycastPosition = m_Manager.GetRaycastHitOrEndPosition();
                FindSnapPosition(in raycastPosition, p_ResultData);
                return;
            }
        }
        private void FindSnapPosition(in Vector3 p_RaycastPosition, ABS_PositionSearchResult p_Result)
        {
            ABS_BuildingElementSnapPointType spType = m_ActiveBuildingElement.SnapPointType;
            float minDistance = float.MaxValue;

            HashSet<ABS_Building> buildings = new HashSet<ABS_Building>();
            List<ABS_BuildingElement> buildingElements = new List<ABS_BuildingElement>();
            ABS_BuildingElement preBuiltTarget = null;
            bool isPreBuiltFound = SearchForNearBuildingElements(
                ABS_PositionSearchAlgorithm.SnapPointBased,
                p_RaycastPosition,
                m_Settings.SearchRadius,
                ref buildingElements,
                ref buildings,
                ref preBuiltTarget);

            if (isPreBuiltFound)
            {
                GetPreBuiltPositionResultData(preBuiltTarget, p_Result);
                return;
            }

            foreach (ABS_BuildingElement element in buildingElements)
            {
                ABS_SnapPointBasedBuilding building = element.ParentBuilding as ABS_SnapPointBasedBuilding;
                Transform buildingTransform = building.transform;
                List<(Vector3, Vector3)> snapPoints = m_SnapPointManager.GetSnapPositions(m_ActiveBuildingElement, element);

                List<(Vector3, Vector3)> localSnapPoints = m_SnapPointManager.ValidateSnapPoints(
                    m_ActiveBuildingElement, 
                    element, 
                    snapPoints, 
                    building);

                foreach ((Vector3 pos, Vector3 rot) in localSnapPoints)
                {
                    //if (sp.m_SnapPointType == spType)
                    {
                        Vector3 worldPosition = buildingTransform.TransformPoint(pos);
                        float distance = Vector3.Distance(p_RaycastPosition, worldPosition);
                        if (distance <= m_SnapPointBasedBuilderSettings.BuildRadius && distance < minDistance)
                        {
                            Quaternion snappedEulerAngles = Quaternion.Euler(rot);
                            Quaternion worldRotation = buildingTransform.localRotation * snappedEulerAngles;

                            ABS_PositionValidationData tmpResult = ValidateSnapPoint(building, buildingTransform, worldPosition, worldRotation);
                            if (CheckValidationResult(tmpResult.m_Result))
                            {
                                minDistance = distance;
                                p_Result.WorldPosition = worldPosition;
                                p_Result.Rotation = worldRotation;
                                p_Result.TargetBuilding = element.ParentBuilding;
                                p_Result.ValidationResult = tmpResult;

                            }
                        }
                    }
                }
            }

            if (minDistance < float.MaxValue)
            {
                p_Result.Result = ABS_PositionSearchResult.ResultType.Success;
                if (IsBlockingNeededBasedOnValidationLogic(p_Result.ValidationResult.m_Result))
                {
                    p_Result.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
                }
            }
            else
            {
                p_Result.Result = ABS_PositionSearchResult.ResultType.FallbackIsNeeded;
            }
        }

        private ABS_PositionValidationData ValidateSnapPoint(in ABS_SnapPointBasedBuilding p_Building,
                                                       in Transform p_BuildingTransform,
                                                       in Vector3 p_WorldPosition,
                                                       in Quaternion p_SnappedRotation)
        {
            ABS_PositionValidationData validationResultData = new ABS_PositionValidationData();
            BaseElementValidation(validationResultData, p_Building, p_WorldPosition, p_SnappedRotation, false, false);

            //If something failed what can not be ignored then return the results.
            if (!CheckValidationResult(validationResultData.m_Result))
            {
                return validationResultData;
            }

            Vector3 parentLocalPosition = p_BuildingTransform.InverseTransformPoint(p_WorldPosition);
            p_Building.ValidatePosition(validationResultData, in parentLocalPosition, Quaternion.identity, m_ActiveBuildingElement);

            return validationResultData;
        }

        protected override bool CanSnapToElement(ABS_BuildingElement p_Element)
        {
            return true;
        }
    }
}
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
    public class ABS_AdvancedGridBuilder : ABS_BuilderBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_AdvancedGridBuilderSettings m_AdvancedGridBuilderSettings;

#if UNITY_EDITOR
        private ulong m_StatisticsCheckedBuildingCounter = 0;

        private ulong m_StatisticsCheckedElementCounter = 0;

        private ulong m_StatisticsHighImpactSnapPointCounter = 0;

        private ulong m_StatisticsFailedValidation = 0;
        private ulong m_StatisticsFailedCustomValidation = 0;
        private ulong m_StatisticsValidatedSnapPointCounter = 0;

        private ulong m_StatisticsSuccessFirstSnapPointCheckCounter = 0;
        private ulong m_StatisticsSuccessPositioningUsingCacheCounter = 0;

        private static string s_StatisticsMessageFormat =
            "\n-------------------------------------" +
            "\n Basics " +
            "\n-------------------------------------" +
            "\nSummary Of Position Process Count : {0}" +
            "\nSummary Of Position Process Time : {1}ms" +
            "\nAVG Of Position Process Time : {2}ms" +
            "\nMaximum Position Process Time : {3}ms" +
            "\n-------------------------------------" +
            "\nABS_Building Statistics " +
            "\n-------------------------------------" +
            "\nSummary of checked Buildings : {4}" +
            "\nAVG of checked Buildings : {5}" +
            "\n-------------------------------------" +
            "\nABS_BuildingElement Statistics " +
            "\n-------------------------------------" +
            "\nSummary of checked BuildingElements : {6}" +
            "\nAVG of checked BuildingElements : {7}" +
            "\n-------------------------------------" +
            "\nSnapPoint Statistics " +
            "\n-------------------------------------" +
            "\nSummary of High Impact SnapPoints : {8}" +
            "\nAVG of High Impact SnapPoints : {9}" +
            "\nSummary of Successful Validation SnapPoint : {10}" +
            "\nAVG of Successful Validation SnapPoint : {11}" +
            "\nSummary of Failed Validation SnapPoint : {12}" +
            "\nAVG of Failed Validation SnapPoint : {13}" +
            "\nSummary of Failed Custom Validation SnapPoint : {14}" +
            "\nAVG of Failed Custom Validation SnapPoint : {15}" +
            "\n-------------------------------------" +
            "\nShortcut Statistics " +
            "\n-------------------------------------" +
            "\nSuccessful first SnapPoint check: {16}/{17} (Checked Building Count / Successful First Positioning)" +
            "\nSuccessful Positioning Using Cache : {18}/{19}  (Checked Building Count / Successful Cache Positioning)";

#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_AdvancedGridBuilder(ABS_IBuildingManagerInternalInterface p_Manager, ABS_BuildingManagerTracker p_Tracker)
            : base(p_Manager, p_Tracker) { }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override Vector3 GetParentPositionAlignment(ABS_BuildingElement p_Element)
        {
            return ABS_AdvancedGirdBuilderGridHelper.GetParentPositionAlignment(p_Element);
        }

        public static void CheckStabilityFeature (
            ABS_PositionValidationData_AdvancedGrid p_ValidationData, 
            ABS_BuildingParent p_Parent, 
            ABS_BuildingElement p_ActiveBuildingElement)
        {
            CheckIfElementIsStable(p_ValidationData, p_ActiveBuildingElement);

            //The first element should always be stable
            if (p_ValidationData.m_Stable)
            {
                p_ValidationData.m_Result.SpecialElementValidation_Stability = ABS_PositionValidationResult.ResultOptions.Validated;

                p_ValidationData.m_Stability = p_Parent.AdvancedGridStabilityLevel;
                p_ValidationData.m_DragBuildingTemporaryStability = p_Parent.AdvancedGridStabilityLevel;
            }
            else
            {
                p_ValidationData.m_Result.SpecialElementValidation_Stability = ABS_PositionValidationResult.ResultOptions.Failed;
            }
        }

        public static void CheckIfElementIsStable(
            ABS_PositionValidationData_AdvancedGrid p_ValidationData,
            ABS_BuildingElement p_ActiveBuildingElement)
        {
            if (p_ActiveBuildingElement.PositionAlgorithmSettings.UseFoundationLogic)
            {
                if (p_ActiveBuildingElement.Foundation)
                {
                    p_ValidationData.m_Stable = true;
                }
                else
                {
                    p_ValidationData.m_Stable = p_ActiveBuildingElement.StableElement;
                }
            }
            else
            {
                p_ValidationData.m_Stable =
                    p_ActiveBuildingElement.StableElement
                    || p_ValidationData.m_PositionWasAlignedToGround
                    || p_ValidationData.m_Result.BaseElementValidation_GroundedCheck == ABS_PositionValidationResult.ResultOptions.Validated
                    || p_ValidationData.m_Result.BaseElementValidation_AirHeightLimit == ABS_PositionValidationResult.ResultOptions.Validated;
            }
        }

        public override ABS_BuilderBaseSettings Settings
        {
            set
            {
                m_AdvancedGridBuilderSettings = (value as ABS_AdvancedGridBuilderSettings);
                base.Settings = value;
            }
        }

        protected override void SearchPositionImpl(in bool m_IsMain, in ABS_PositionSearchResult p_ResultData)
        {
            if (m_Manager.Raycaster.HitTransform == null && !m_Settings.AllowPositionSearchAtRaycastEndPosition)
            {
                p_ResultData.Result = ABS_PositionSearchResult.ResultType.FallbackIsNeeded;
            }

            Vector3 raycastHitPosition = m_Manager.GetRaycastHitOrEndPosition();

            HashSet<ABS_Building> buildings = new HashSet<ABS_Building>();
            List<ABS_BuildingElement> buildingElements = new List<ABS_BuildingElement>();
            ABS_BuildingElement preBuiltTarget = null;
            bool isPreBuiltFound = SearchForNearBuildingElements(
                ABS_PositionSearchAlgorithm.AdvancedGrid,
                raycastHitPosition,
                m_Settings.SearchRadius,
                ref buildingElements,
                ref buildings,
                ref preBuiltTarget);

            if (isPreBuiltFound)
            {
                GetPreBuiltPositionResultData(preBuiltTarget, p_ResultData);
                return;
            }

#if UNITY_EDITOR
            m_StatisticsCheckedElementCounter += (ulong)buildingElements.Count;
            m_StatisticsCheckedBuildingCounter += (ulong)buildings.Count;
#endif

            bool positionFound = false;
            float minDistance = float.MaxValue;
            foreach (ABS_AdvancedGridBuilding building in buildings)
            {
                positionFound |= ProcessBuilding(building, p_ResultData, raycastHitPosition, ref minDistance);
            }

            //If the distance is smaller then the base value than the algorithm found a position
            if (positionFound)
            {
                if (IsBlockingNeededBasedOnValidationLogic(p_ResultData.ValidationResult.m_Result))
                {
                    p_ResultData.Result = ABS_PositionSearchResult.ResultType.SuccessBlockNeeded;
                }
                else
                {
                    p_ResultData.Result = ABS_PositionSearchResult.ResultType.Success;
                }

                //Find the Override element if it was override result
                if (p_ResultData.ValidationResult.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings == ABS_PositionValidationResult.ResultOptions.Validated)
                {
                    p_ResultData.TargetOverrideElement = p_ResultData.TargetBuilding.FindBuildingElement(p_ResultData.WorldPosition, true);
                    p_ResultData.IsOverrideSnapping = true;
                    if (p_ResultData.TargetOverrideElement == null)
                    {
                        REST_Logging.Error("ABS_AdvancedGridBuilder", "FindSnapPosition", "In case of Overriding the Target Override Element can't be null!");
                    }
                }

            }
            else
            {
                p_ResultData.Result = ABS_PositionSearchResult.ResultType.FallbackIsNeeded;
            }
        }

        private bool ProcessBuilding (ABS_AdvancedGridBuilding p_Building,
                                      ABS_PositionSearchResult p_Result,
                                      Vector3 p_RaycastHitPosition,
                                      ref float p_MinDistance)
        {
            Vector3 gridSize = m_AdvancedGridBuilderSettings.GridSize;
            Vector3 playerRotation = GetPlayerRotation();
            Vector3 gridSizeByType = GetGridSizeBasedOnType();
            bool rotationByPositionIsNeeded = true;
            
            UnityEngine.Quaternion finalQuaternionRotation = Quaternion.identity;

            Transform buildingTransform = p_Building.transform;
            Vector3 buildingModifier = p_Building.BuildingPositionModifier;

            Vector3 parentLocalPosition = buildingTransform.InverseTransformPoint(p_RaycastHitPosition);
            Vector3 hitNormal = m_Manager.Raycaster.Hit.normal;
            parentLocalPosition = ABS_AdvancedGirdBuilderGridHelper.FixGridEdgeCase(parentLocalPosition, gridSize, hitNormal);

            Vector3 parentLocalNearestGridPosition = ABS_AdvancedGirdBuilderGridHelper.GetGridPosition(parentLocalPosition - buildingModifier,
                                                                                                    m_ActiveBuildingElement,
                                                                                                    m_AdvancedGridBuilderSettings);
            Vector3 parentLocalNearestGridPositionAligned = parentLocalNearestGridPosition + buildingModifier;

            if (m_Manager.IsCachingEnabled)
            {
                //I'm using the parentLocalNearestGridPositionAligned for caching
                //This can cause that sometimes the result will out of the building range.
                ABS_BuilderCacheResultData cachedData = m_Cache.FindSuccessResultCache(p_Building, 
                                                                                        parentLocalNearestGridPositionAligned, 
                                                                                        playerRotation);
                if (cachedData != null)
                {
#if UNITY_EDITOR
                    ++m_StatisticsSuccessPositioningUsingCacheCounter;
#endif

                    Vector3 localPosition = buildingTransform.InverseTransformPoint(cachedData.m_Result.WorldPosition);
                    rotationByPositionIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(
                        m_ActiveBuildingElement.AdvancedGridType,
                        localPosition,
                        buildingModifier,
                        gridSize);

                    finalQuaternionRotation = GetRotation(
                        buildingTransform,
                        playerRotation,
                        rotationByPositionIsNeeded);

                    p_MinDistance = cachedData.m_Distance;
                    p_Result.WorldPosition = cachedData.m_Result.WorldPosition;
                    p_Result.Rotation = finalQuaternionRotation;
                    p_Result.TargetBuilding = p_Building;
                    p_Result.ValidationResult = cachedData.m_Result.ValidationResult;
                    //Continue needed for continue the processing the buildings
                    //break would be a wrong decision here
                    return true;
                }
            }

            //We should check the first Element too because there can be an another building what had already a valid closer snappoint
            //First validate the closest grid position.
            float distance = Vector3.Distance(parentLocalNearestGridPositionAligned, parentLocalPosition);
            if (distance <= m_AdvancedGridBuilderSettings.BuildRadius)
            {
                if (p_MinDistance > distance || Math.Abs(p_MinDistance - distance) < 0.00003f)
                {
#if UNITY_EDITOR
                    ++m_StatisticsHighImpactSnapPointCounter;
#endif

                    Vector3 worldPosition = buildingTransform.TransformPoint(parentLocalNearestGridPositionAligned);

                    rotationByPositionIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(
                        m_ActiveBuildingElement.AdvancedGridType,
                        parentLocalNearestGridPositionAligned,
                        buildingModifier,
                        gridSize);

                    finalQuaternionRotation = GetRotation(
                        buildingTransform,
                        playerRotation,
                        rotationByPositionIsNeeded);

                    ABS_PositionValidationData validationResult = ValidateSnapPoint(
                        p_Building,
                        parentLocalNearestGridPositionAligned,
                        rotationByPositionIsNeeded,
                        worldPosition,
                        p_RaycastHitPosition,
                        ref finalQuaternionRotation
                    );

                    if (CheckValidationResult(validationResult.m_Result))
                    {
#if UNITY_EDITOR
                        ++m_StatisticsSuccessFirstSnapPointCheckCounter;
                        ++m_StatisticsValidatedSnapPointCounter;
#endif
                        p_MinDistance = distance;
                        p_Result.WorldPosition = worldPosition;
                        p_Result.Rotation = finalQuaternionRotation;
                        p_Result.TargetBuilding = p_Building;
                        p_Result.ValidationResult = validationResult;

                        if (m_Manager.IsCachingEnabled)
                        {
                            m_Cache.AddCache(parentLocalNearestGridPositionAligned, playerRotation, distance, p_Result);
                        }
                        return true;
                    }
                    else
                    {
#if UNITY_EDITOR
                        ++m_StatisticsFailedValidation;
#endif

                        if (m_Manager.IsCachingEnabled)
                        {
                            ABS_PositionSearchResult cachedResult = new ABS_PositionSearchResult();
                            cachedResult.WorldPosition = worldPosition;
                            cachedResult.Rotation = finalQuaternionRotation;
                            cachedResult.TargetBuilding = p_Building;
                            cachedResult.ValidationResult = validationResult;

                            m_Cache.AddCache(parentLocalNearestGridPositionAligned, playerRotation, distance, p_Result);
                        }
                    }
                }
                else
                {
                    //If the closest snap point has a bigger distance than an another building's successful snappoint
                    //then the process of this building can be stoped.
                    return false;
                }
            }

            // int BuildRange = (int)Math.Ceiling(m_AdvancedGridBuilderSettings.BuildRadius / GetBuildRangeBasedOnType());
            int BuildRange = (int)Math.Ceiling(m_AdvancedGridBuilderSettings.BuildRadius);
            Vector3 starterPosition = parentLocalNearestGridPositionAligned - gridSize;

            bool isElementAWallOrHorizontalEdge = m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.Wall
                                                    || m_ActiveBuildingElement.AdvancedGridType == ABS_AdvancedGridType.EdgeHorizontal;
            List<Tuple<Vector3, float>> snapPositions = new List<Tuple<Vector3, float>>();
            for (int i = 0; i < BuildRange + 1; ++i)
            {
                for (int j = 0; j < BuildRange + 1; ++j)
                {
                    for (int k = 0; k < BuildRange + 1; ++k)
                    {
                        Vector3 checkedPosition = (new Vector3(i * gridSize.x, j * gridSize.y, k * gridSize.z)) + starterPosition;
                        distance = Vector3.Distance(checkedPosition, parentLocalPosition);
                        if (distance <= m_AdvancedGridBuilderSettings.BuildRadius
                            && (p_MinDistance > distance || Math.Abs(p_MinDistance - distance) < 0.00003f))
                        {
                            snapPositions.Add(new Tuple<Vector3, float>(checkedPosition, distance));
                        }

                        if (isElementAWallOrHorizontalEdge)
                        {
                            checkedPosition += new Vector3(gridSizeByType.x, 0, gridSizeByType.z);
                            distance = Vector3.Distance(checkedPosition, parentLocalPosition);
                            if (distance <= m_AdvancedGridBuilderSettings.BuildRadius
                                && (p_MinDistance > distance || Math.Abs(p_MinDistance - distance) < 0.00003f))
                            {
                                snapPositions.Add(new Tuple<Vector3, float>(checkedPosition, distance));
                            }
                        }
                    }
                }
            }

            snapPositions.Sort((Tuple<Vector3, float> obj1, Tuple<Vector3, float> obj2) => obj1.Item2.CompareTo(obj2.Item2));
            //Skip the first one because that already checked
            int idx = 1;
            while (idx < snapPositions.Count)
            {

                distance = snapPositions[idx].Item2;
                if (p_MinDistance > distance || Math.Abs(p_MinDistance - distance) < 0.00003f)
                {
                    Vector3 checkedPosition = snapPositions[idx].Item1;

                    if (m_Manager.IsCachingEnabled)
                    {
                        ABS_BuilderCacheResultData cachedData = m_Cache.FindInCache(p_Building,
                                                                                    checkedPosition,
                                                                                    playerRotation);

                        if (cachedData != null)
                        {
                            if (CheckValidationResult(cachedData.m_Result.ValidationResult.m_Result))
                            {
#if UNITY_EDITOR
                                ++m_StatisticsSuccessPositioningUsingCacheCounter;
#endif
                                Vector3 localPosition = buildingTransform.InverseTransformPoint(cachedData.m_Result.WorldPosition);
                                rotationByPositionIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(
                                    m_ActiveBuildingElement.AdvancedGridType,
                                    localPosition,
                                    buildingModifier,
                                    gridSize);

                                finalQuaternionRotation = GetRotation(
                                    buildingTransform,
                                    playerRotation,
                                    rotationByPositionIsNeeded);

                                p_MinDistance = cachedData.m_Distance;
                                p_Result.WorldPosition = cachedData.m_Result.WorldPosition;
                                p_Result.Rotation = finalQuaternionRotation;
                                p_Result.TargetBuilding = p_Building;
                                p_Result.ValidationResult = cachedData.m_Result.ValidationResult;
                                //Continue needed for continue the processing the buildings
                                //break would be a wrong decision here
                                return true;
                            }
                        }
                    }

#if UNITY_EDITOR
                    ++m_StatisticsHighImpactSnapPointCounter;
#endif

                    rotationByPositionIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(
                        m_ActiveBuildingElement.AdvancedGridType,
                        checkedPosition,
                        buildingModifier,
                        gridSize);

                    finalQuaternionRotation = GetRotation(
                        buildingTransform,
                        playerRotation,
                        rotationByPositionIsNeeded);

                    Vector3 worldPosition = buildingTransform.TransformPoint(checkedPosition);

                    ABS_PositionValidationData validationResult = ValidateSnapPoint(
                        p_Building,
                        checkedPosition,
                        rotationByPositionIsNeeded,
                        worldPosition,
                        p_RaycastHitPosition,
                        ref finalQuaternionRotation
                    );

                    if (CheckValidationResult(validationResult.m_Result))
                    {
#if UNITY_EDITOR
                        ++m_StatisticsValidatedSnapPointCounter;
#endif
                        p_MinDistance = distance;
                        p_Result.WorldPosition = worldPosition;
                        p_Result.Rotation = finalQuaternionRotation;
                        p_Result.TargetBuilding = p_Building;
                        p_Result.ValidationResult = validationResult;

                        if (m_Manager.IsCachingEnabled)
                        {
                            m_Cache.AddCache(parentLocalNearestGridPositionAligned, playerRotation, distance, p_Result);
                        }
                        return true;
                    }
                    else
                    {
#if UNITY_EDITOR
                        ++m_StatisticsFailedValidation;
#endif
                        if (m_Manager.IsCachingEnabled)
                        {
                            ABS_PositionSearchResult cachedResult = new ABS_PositionSearchResult();
                            cachedResult.WorldPosition = worldPosition;
                            cachedResult.Rotation = finalQuaternionRotation;
                            cachedResult.TargetBuilding = p_Building;
                            cachedResult.ValidationResult = validationResult;

                            m_Cache.AddCache(parentLocalNearestGridPositionAligned, playerRotation, distance, cachedResult);
                        }
                    }
                }
                else
                {
                    return false;
                }
                ++idx;
            }
            return false;
        }
        protected override bool CanSnapToElement(ABS_BuildingElement p_Element)
        {
            return m_VectorCompareator.Equals( 
                (p_Element.PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings).GridSize,
                 m_AdvancedGridBuilderSettings.GridSize);
        }

        private ABS_PositionValidationData ValidateSnapPoint(in ABS_AdvancedGridBuilding p_Building,
                                                       in Vector3 p_BuildingLocalPosition,
                                                       in bool p_RotationByPositionIsNeeded,
                                                       in Vector3 p_WorldPosition,
                                                       in Vector3 p_RaycastPosition,
                                                       ref UnityEngine.Quaternion p_FinalQuaternionRotation)
        {
            ABS_PositionValidationData_AdvancedGrid validationResultData = new ABS_PositionValidationData_AdvancedGrid();
            if (!m_ActiveBuildingElement.IsPositionValidBasedOnAxis(!p_RotationByPositionIsNeeded))
            {
                validationResultData.m_Result.SpecialElementValidation_ForbiddenAxis = ABS_PositionValidationResult.ResultOptions.Failed;
                return validationResultData;
            }

            BaseElementValidation(validationResultData, p_Building, p_WorldPosition, p_FinalQuaternionRotation, false, false);
            //If something failed what can not be ignored then return the results.
            if (!CheckValidationResult(validationResultData.m_Result))
            {
                return validationResultData;
            }

            CheckIfElementIsStable(validationResultData, m_ActiveBuildingElement);

            Quaternion localRotation = Quaternion.Inverse(p_Building.transform.rotation) * p_FinalQuaternionRotation;
            p_Building.ValidatePosition(validationResultData, in p_BuildingLocalPosition, localRotation, m_ActiveBuildingElement);
            //If something failed what can not be ignored then return the results.
            if (validationResultData.m_Result.IsFailed())
            {
                return validationResultData;
            }

            if (!m_Tracker.PositionCustomValidation(m_ActiveBuildingElement, p_WorldPosition, p_RaycastPosition, p_FinalQuaternionRotation))
            {
#if UNITY_EDITOR
                ++m_StatisticsFailedCustomValidation;
#endif
                validationResultData.m_Result.CustomElementValidation = ABS_PositionValidationResult.ResultOptions.Failed;
            }

            return validationResultData;
        }

        private UnityEngine.Quaternion GetRotation(
            in Transform p_BuildingTransform,
            in Vector3 p_PlayerRotation,
            in bool p_RotationByPositionIsNeeded)
        {
            return UnityEngine.Quaternion.Euler(p_BuildingTransform.eulerAngles 
                + p_PlayerRotation 
                + (p_RotationByPositionIsNeeded ? ABS_AdvancedGirdBuilderGridHelper.s_RotationModifier : Vector3.zero));
        }

        private Vector3 GetPlayerRotation()
        {
            float baseRotation = 90.0f;
            switch (m_ActiveBuildingElement.AdvancedGridType)
            {
                case ABS_AdvancedGridType.Floor:
                case ABS_AdvancedGridType.Center:
                case ABS_AdvancedGridType.EdgeVertical:
                case ABS_AdvancedGridType.Corner:
                    baseRotation = 90.0f; break;
                case ABS_AdvancedGridType.Wall:
                case ABS_AdvancedGridType.EdgeHorizontal:
                    baseRotation = 180.0f; break;
            }
            
            return Vector3.up * CalcualteMixedRotation(baseRotation);
        }

        private Vector3 GetGridSizeBasedOnType()
        {
            switch (m_ActiveBuildingElement.AdvancedGridType)
            {
                case ABS_AdvancedGridType.Wall:
                case ABS_AdvancedGridType.EdgeHorizontal:
                    return m_AdvancedGridBuilderSettings.HalfGridSize;
                case ABS_AdvancedGridType.Floor:
                case ABS_AdvancedGridType.Center:
                case ABS_AdvancedGridType.EdgeVertical:
                case ABS_AdvancedGridType.Corner:
                default:
                    return m_AdvancedGridBuilderSettings.GridSize;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public override void OnDrawGizmosImpl(in ABS_ProjectSettings p_ProjectSettings, in ABS_PositionSearchResult p_PositionSearchResult)
        {
            if (!p_ProjectSettings.PositionSearch_BuildCollider || !p_ProjectSettings.PositionSearch_CheckedBESnapPoints)
            {
                return;
            }

            //Draw line to snappoint
            //GizmosUtils.DrawLine(raycastPosition, p_PositionSearchResult.WorldPosition, m_Settings.);

            Vector3 raycastPosition = m_Manager.GetRaycastHitOrEndPosition();
            float radius = m_AdvancedGridBuilderSettings.BuildRadius * p_ProjectSettings.PositionSearch_SnapPointsArea / 100f;
            REST_GizmosUtils.DrawWireSphere(raycastPosition, radius, p_ProjectSettings.PositionSearch_CheckedBESnapPointsColor);

            Dictionary<Vector3, UnityEngine.Color> snapPointsForGizmos = new Dictionary<Vector3, UnityEngine.Color>();

            HashSet<ABS_Building> buildings = new HashSet<ABS_Building>();
            List<ABS_BuildingElement> buildingElements = new List<ABS_BuildingElement>();
            ABS_BuildingElement preBuiltTarget = null;
            bool isPreBuiltFound = SearchForNearBuildingElements(
                ABS_PositionSearchAlgorithm.AdvancedGrid,
                raycastPosition,
                m_Settings.SearchRadius,
                ref buildingElements,
                ref buildings,
                ref preBuiltTarget);

            foreach (ABS_BuildingElement be in buildingElements)
            {
                ABS_AdvancedGridSnapPoint[] tmpSnapPoints = 
                    ABS_AdvancedGridSnapPointCollection.GetSnapPointsForElements(be.AdvancedGridType, m_ActiveBuildingElement.AdvancedGridType);
                if (tmpSnapPoints != null)
                {
                    foreach (ABS_AdvancedGridSnapPoint snappoint in tmpSnapPoints)
                    {
                        Vector3 alignedPosition = be.transform.TransformPoint(new Vector3(
                            snappoint.m_Position.x * m_AdvancedGridBuilderSettings.GridSize.x,
                            snappoint.m_Position.y * m_AdvancedGridBuilderSettings.GridSize.y,
                            snappoint.m_Position.z * m_AdvancedGridBuilderSettings.GridSize.z));
                        float distance = Vector3.Distance(alignedPosition, raycastPosition);
                        if (distance <= radius)
                        {
                            if (distance <= m_AdvancedGridBuilderSettings.BuildRadius
                                && be.PreBuilt && be.PrefabGuid == m_ActiveBuildingElement.PrefabGuid)
                            {
                                snapPointsForGizmos[alignedPosition] = UnityEngine.Color.green;
                            }
                            else
                            {
                                snapPointsForGizmos[alignedPosition] = UnityEngine.Color.red;
                            }
                        }
                    }
                }
            }
            
            foreach ((Vector3 pos, UnityEngine.Color color) in snapPointsForGizmos)
            {
                REST_GizmosUtils.DrawSphere(pos, 0.05f, color);
            }
        }
#endif


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Statistics
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public override void TriggerStatisticsPrint()
        {
            REST_Logging.Debug($"{this}",
                   String.Format(
                       s_StatisticsMessageFormat,
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsPositioningCounter),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round(m_StatisticsSearchProcessTimeCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round(m_StatisticsSearchProcessTimeCounter / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round(m_StatisticsSearchProcessTimeMaximum, 2)),
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsCheckedBuildingCounter),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round((double)m_StatisticsCheckedBuildingCounter / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsCheckedElementCounter),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round((double)m_StatisticsCheckedElementCounter / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsHighImpactSnapPointCounter),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round((double)m_StatisticsHighImpactSnapPointCounter / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsValidatedSnapPointCounter),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round((double)m_StatisticsValidatedSnapPointCounter / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsFailedValidation),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round((double)m_StatisticsFailedValidation / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, m_StatisticsFailedCustomValidation),
                       String.Format(s_StatisticsNumberColorFormat, Math.Round((double)m_StatisticsFailedCustomValidation / m_StatisticsPositioningCounter, 2)),
                       String.Format(s_StatisticsNumberColorFormat, (m_StatisticsCheckedBuildingCounter)),
                       String.Format(s_StatisticsNumberColorFormat, (m_StatisticsSuccessFirstSnapPointCheckCounter)),
                       String.Format(s_StatisticsNumberColorFormat, (m_StatisticsCheckedBuildingCounter)),
                       String.Format(s_StatisticsNumberColorFormat, (m_StatisticsSuccessPositioningUsingCacheCounter))
                   ));
        }

        public override void StatisticsResetImpl()
        {
            m_StatisticsPositioningCounter = 0;
            m_StatisticsSearchProcessTimeCounter = 0d;
            m_StatisticsCheckedBuildingCounter = 0;
            m_StatisticsCheckedElementCounter = 0;
            m_StatisticsFailedValidation = 0;
            m_StatisticsFailedCustomValidation = 0;
            m_StatisticsHighImpactSnapPointCounter = 0;
            m_StatisticsValidatedSnapPointCounter = 0;
            m_StatisticsSuccessFirstSnapPointCheckCounter = 0;
            m_StatisticsSuccessPositioningUsingCacheCounter = 0;
            m_StatisticsSearchProcessTimeMaximum = 0;
        }
#endif
    }
}
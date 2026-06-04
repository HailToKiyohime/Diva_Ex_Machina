//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_RepositionState
    {
        Stable,
        Moving,
        FixPosition
    }

    public class ABS_SimpleBuildingManager : ABS_BuildingManagerComponentBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BuildingElement m_ActiveBuildingElement = null;
        private ABS_BuilderBaseSettings m_AlgorithmSettings = null;
        private ABS_PositionSearchAlgorithm m_PositionSearchStrategy = ABS_PositionSearchAlgorithm.Free;

        private ABS_TemporaryBuildingElementManager m_TemporaryBuildingElementManager = null;
        private GameObject m_TemporaryBuildingElementManagerObject = null;
        private Transform m_TemporaryBuildingElementManagerTransform = null;
        private ABS_PositionerManager m_PositionerManager = null;

        private ABS_PositionSearchResult m_PositionSearchResult = null;
        private ABS_RepositionState m_RepositionState = ABS_RepositionState.Stable;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_SimpleBuildingManager(
            ABS_IBuildingManagerInternalInterface p_Manager,
            ABS_BuildingManagerTracker p_Tracker,
            ABS_PositionerManager p_m_PositionerManager,
            ABS_TemporaryBuildingElementManager p_TemporaryBuildingElementManager)
            : base(p_Manager, p_Tracker)
        {
            m_PositionerManager = p_m_PositionerManager;

            m_TemporaryBuildingElementManager = p_TemporaryBuildingElementManager;
            m_TemporaryBuildingElementManagerObject = m_TemporaryBuildingElementManager.gameObject;
            m_TemporaryBuildingElementManagerTransform = m_TemporaryBuildingElementManagerObject.transform;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_PositionSearchResult PositionSearchResult
        {
            get { return m_PositionSearchResult; }
        }

        public ABS_RepositionState RepositionState
        {
            get { return m_RepositionState; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void SearchAndMovePosition(ref ABS_SimpleBuildingProcessErrorCode p_SimpleBuildingProcessErrorCode)
        {
            ABS_PositionSearchResult result = m_PositionerManager.SearchPosition(m_Manager.IsForcedFallbackIsOn());

            RepositionTheElement(result);

            Block(CheckBlockState(ref p_SimpleBuildingProcessErrorCode, result));

            HanldePrebuildElementsSpecialNeeds(result);

            HanldeOverrideElementsSpecialNeeds(result);

            //Save the results
            //It is important to save at the and because of the prebuilt logic uses the earlier results too.
            m_PositionSearchResult = result;
            m_TemporaryBuildingElementManager.PositionSearchResult = m_PositionSearchResult;
            m_Tracker.FirstBuildingElementCurrentPosition(
                m_TemporaryBuildingElementManagerTransform.position, 
                m_TemporaryBuildingElementManagerTransform.rotation);
        }

        public void ResetBuildingElement(ABS_BuildingElement p_Element)
        {
            m_ActiveBuildingElement = p_Element;
            if (m_ActiveBuildingElement == null)
            {
                m_AlgorithmSettings = null;
            }
            else
            {
                m_PositionSearchStrategy = m_ActiveBuildingElement.PositionSearchAlgorithm;
                m_AlgorithmSettings = m_ActiveBuildingElement.PositionAlgorithmSettings;
            }
        }

        public void Reset()
        {
            if (m_PositionSearchResult != null
                && m_PositionSearchResult.Result == ABS_PositionSearchResult.ResultType.Success)
            {
                if (m_PositionSearchResult.IsPreBuiltSnapping
                    && m_PositionSearchResult.TargetPreBuiltElement != null)
                {
                    m_PositionSearchResult.TargetPreBuiltElement.EnableMeshRenderers(true);
                }

                if (m_PositionSearchResult.IsOverrideSnapping
                    && m_PositionSearchResult.TargetOverrideElement != null)
                {
                    m_PositionSearchResult.TargetOverrideElement.EnableMeshRenderers(true);
                }
            }

            m_PositionSearchResult = null;

            ResetBuildingElement(null);
        }

        public ABS_PositionValidationData ValidatePosition(in Vector3 p_Position, in UnityEngine.Quaternion p_Rotation)
        {
            ABS_PositionValidationData resultData = ABS_PositionValidationData.PositionValidationDataFactory(m_ActiveBuildingElement);
            ABS_Building targetBuilding = m_PositionSearchResult == null ? null : m_PositionSearchResult.TargetBuilding;
            switch (m_PositionSearchStrategy)
            {
                case ABS_PositionSearchAlgorithm.Free:
                    targetBuilding = m_Manager.GlobalFreeParent; break;
                case ABS_PositionSearchAlgorithm.BasicGrid:
                    targetBuilding = m_Manager.GlobalBasicGridParent; break;
            }

            m_PositionerManager.ValidatePosition(resultData, targetBuilding, p_Position, p_Rotation, false, true);
            if (resultData.IsFailed())
            {
                return resultData;
            }

            if (targetBuilding != null)
            {
                //Handle special validation case for the AdvanceGridBuilding only if the targetBuilding is null
                if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    ABS_PositionValidationData_AdvancedGrid advancedResults = resultData as ABS_PositionValidationData_AdvancedGrid;

                    ABS_BuildingParent parent = m_Manager.GetBuildingParent();
                    if (parent.AdvancedGridStabilityEnabled)
                    {
                        ABS_AdvancedGridBuilder.CheckIfElementIsStable(advancedResults, m_ActiveBuildingElement);
                    }
                }

                Vector3 checkedPosition = targetBuilding.transform.InverseTransformPoint(p_Position);
                Quaternion localRotation = m_PositionSearchStrategy == ABS_PositionSearchAlgorithm.AdvancedGrid 
                    ? Quaternion.Inverse(targetBuilding.transform.rotation) * p_Rotation 
                    : Quaternion.identity;
                targetBuilding.ValidatePosition(resultData, checkedPosition, localRotation, m_ActiveBuildingElement);
            }
            else
            {
                resultData.m_Result.ParentBuildingValidation_InvalidPosition = ABS_PositionValidationResult.ResultOptions.Failed;

                //Handle special validation case for the AdvanceGridBuilding only if the targetBuilding is null
                if (m_ActiveBuildingElement.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    ABS_PositionValidationData_AdvancedGrid advancedResults = resultData as ABS_PositionValidationData_AdvancedGrid;

                    ABS_BuildingParent parent = m_Manager.GetBuildingParent();
                    if (parent.AdvancedGridStabilityEnabled)
                    {
                        ABS_AdvancedGridBuilder.CheckStabilityFeature(advancedResults, parent, m_ActiveBuildingElement);
                    }
                }
            }

            if (resultData.IsSuccessFull() 
                && !m_Tracker.PositionCustomValidation(m_ActiveBuildingElement, p_Position, m_Manager.GetRaycastHitOrEndPosition(), p_Rotation))
            {
                resultData.m_Result.CustomElementValidation = ABS_PositionValidationResult.ResultOptions.Failed;
            }

            return resultData;
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Block(in bool p_Block)
        {
            if (p_Block)
            {
                m_TemporaryBuildingElementManager.Block();
            }
            else
            {
                m_TemporaryBuildingElementManager.UnBlock();
            }
        }

        private void RepositionTheElement(ABS_PositionSearchResult p_Result)
        {
            if (p_Result.Result == ABS_PositionSearchResult.ResultType.Success 
                || p_Result.Result == ABS_PositionSearchResult.ResultType.SuccessBlockNeeded)
            {
                if (!p_Result.IsFallbackResult
                    && m_PositionSearchStrategy != ABS_PositionSearchAlgorithm.Free
                    && m_AlgorithmSettings.RepositionStrategy == ABS_RepositionStrategy.Smooth)
                {
                    if (REST_Vector3EqualityComparer.Static_Equals(p_Result.WorldPosition, m_TemporaryBuildingElementManagerTransform.position)
                        && REST_QuaternionEqualityComparer.Static_Equals(p_Result.Rotation, m_TemporaryBuildingElementManagerTransform.rotation))
                    {
                        m_RepositionState = ABS_RepositionState.Stable;
                    }
                    else
                    {
                        m_TemporaryBuildingElementManagerTransform.position = Vector3.MoveTowards(m_TemporaryBuildingElementManagerTransform.position,
                                                                                    p_Result.WorldPosition,
                                                                                    m_AlgorithmSettings.RepositionMoveSpeed * Time.deltaTime);
                        m_TemporaryBuildingElementManagerTransform.rotation = Quaternion.RotateTowards(m_TemporaryBuildingElementManagerTransform.rotation,
                                                                                        p_Result.Rotation,
                                                                                        m_AlgorithmSettings.RepositionRotateSpeed * Time.deltaTime * 10f);
                        m_RepositionState = ABS_RepositionState.Moving;
                    }
                }
                else
                {
                    m_RepositionState = ABS_RepositionState.FixPosition;
                    m_TemporaryBuildingElementManagerTransform.position = p_Result.WorldPosition;
                    m_TemporaryBuildingElementManagerTransform.rotation = p_Result.Rotation;
                }
            }
        }

        private void HanldePrebuildElementsSpecialNeeds(ABS_PositionSearchResult p_Result)
        {
            if (m_PositionSearchResult != null
                && m_PositionSearchResult.TargetOverrideElement != null
                && m_PositionSearchResult.Result == ABS_PositionSearchResult.ResultType.Success
                && m_PositionSearchResult.IsOverrideSnapping)
            {
                if (p_Result.Result == ABS_PositionSearchResult.ResultType.Success && p_Result.IsOverrideSnapping)
                {
                    if (m_PositionSearchResult.TargetOverrideElement != p_Result.TargetOverrideElement)
                    {
                        m_PositionSearchResult.TargetOverrideElement.EnableMeshRenderers(true);
                        p_Result.TargetOverrideElement.EnableMeshRenderers(false);
                    }
                }
                else
                {
                    m_PositionSearchResult.TargetOverrideElement.EnableMeshRenderers(true);
                }
            }
            else if (p_Result.Result == ABS_PositionSearchResult.ResultType.Success
                && p_Result.IsOverrideSnapping
                && p_Result.TargetOverrideElement != null)
            {
                p_Result.TargetOverrideElement.EnableMeshRenderers(false);
            }
        }

        private void HanldeOverrideElementsSpecialNeeds(ABS_PositionSearchResult p_Result)
        {
            if (m_PositionSearchResult != null
                && m_PositionSearchResult.TargetPreBuiltElement != null
                && m_PositionSearchResult.Result == ABS_PositionSearchResult.ResultType.Success
                && m_PositionSearchResult.IsPreBuiltSnapping)
            {
                if (p_Result.Result == ABS_PositionSearchResult.ResultType.Success && p_Result.IsPreBuiltSnapping)
                {
                    if (m_PositionSearchResult.TargetPreBuiltElement != p_Result.TargetPreBuiltElement)
                    {
                        m_PositionSearchResult.TargetPreBuiltElement.EnableMeshRenderers(true);
                        p_Result.TargetPreBuiltElement.EnableMeshRenderers(false);
                    }
                }
                else
                {
                    m_PositionSearchResult.TargetPreBuiltElement.EnableMeshRenderers(true);
                }
            }
            else if (p_Result.Result == ABS_PositionSearchResult.ResultType.Success
                && p_Result.IsPreBuiltSnapping
                && p_Result.TargetPreBuiltElement != null)
            {
                p_Result.TargetPreBuiltElement.EnableMeshRenderers(false);
            }
        }

        private bool CheckBlockState(ref ABS_SimpleBuildingProcessErrorCode p_SimpleBuildingProcessErrorCode, ABS_PositionSearchResult p_Result)
        {
            if (p_Result == null)
            {
                REST_Logging.Error($"{this}", "The result was null!");
                p_SimpleBuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.Unkown;
                return true;
            }

            if (p_Result.Result != ABS_PositionSearchResult.ResultType.Success)
            {
                p_SimpleBuildingProcessErrorCode = p_Result.ValidationResult.m_Result.GetBlockReason();
                return true;
            }

            if (p_Result.IsFallbackResult
                && m_ActiveBuildingElement.PositionAlgorithmSettings.UseFoundationLogic
                && !m_ActiveBuildingElement.Foundation)
            {
                p_SimpleBuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.RuleBreak_FoundationLogic;
                return true;
            }

            if (m_ActiveBuildingElement.ShouldOverride
                && p_Result.ValidationResult.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings != ABS_PositionValidationResult.ResultOptions.Validated)
            {
                p_SimpleBuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.RuleBreak_ShouldOverride;
                return true;
            }

            if ((m_Manager.HitTransform == null
                && p_Result.TargetBuilding == null
                && !m_AlgorithmSettings.AllowBuildingInTheAir)
                && !(m_AlgorithmSettings.AlignPositionToGround && p_Result.IsAlignedToGround)
                && !(m_PositionSearchStrategy == ABS_PositionSearchAlgorithm.BasicGrid))
            {
                p_SimpleBuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.RuleBreak_AllowBuildingInTheAir;
                return true;
            }

            if (p_Result.TargetBuilding != null && p_Result.TargetBuilding.FreeSpace == 0)
            {
                p_SimpleBuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.RuleBreak_Building_MaximumElementCount;
                return true;
            }

            p_SimpleBuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.Successful;
            return false;
        }

    }
}

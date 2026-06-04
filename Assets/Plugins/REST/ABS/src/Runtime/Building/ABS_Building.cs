//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_Building : ABS_SaveableMonobehaviour, ABS_IBuildingExternalInterface
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BuildingParent m_Parent = null;

        private List<string> m_DefaultElements = null;
        protected Dictionary<Vector3, ABS_BuildingElement> m_Elements = null;
        protected REST_Vector3EqualityComparer m_Vector3Comparer = null;
        [SerializeField] private uint m_MaximumElementCount = 1000;

        protected ABS_BuildingCache m_Cache = null;
        [SerializeField] protected bool m_EnableCache = true;

        protected Transform m_BuildingTransform = null;

        protected ABS_PositionSearchAlgorithm m_PositionSearchAlgorithmType = ABS_PositionSearchAlgorithm.Free;

        [SerializeField] private bool m_UpperRangeLimitEnabled = false;
        [SerializeField] private bool m_UnderRangeLimitEnabled = false;
        [SerializeField] private bool m_SideRangetLimitEnabled = false;
        [SerializeField] private float m_UpperRangeLimit = 1000f;
        [SerializeField] private float m_UnderRangeLimit = -1000f;
        [SerializeField] private float m_SideRangetLimit = 1000f;

        private bool m_DontDestroyWhenEmpty = false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  getters / setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public Transform Transform => m_BuildingTransform;

        public ABS_BuildingParent Parent
        {
            get => m_Parent;
            set { m_Parent = value; }
        }

        public ReadOnlyDictionary<Vector3, ABS_BuildingElement> Elements
        {
            get
            {
                ReadOnlyDictionary<Vector3, ABS_BuildingElement> elements = new ReadOnlyDictionary<Vector3, ABS_BuildingElement>(m_Elements);
                return elements;
            }
        }

        public ABS_PositionSearchAlgorithm PositionSearchAlgorithmType
        {
            get { return m_PositionSearchAlgorithmType; }
            set { m_PositionSearchAlgorithmType = value; }
        }

        public uint MaximumElementCount
        {
            get { return m_MaximumElementCount; }
            set { m_MaximumElementCount = value; }
        }

        public uint FreeSpace
        {
            get { return m_MaximumElementCount - (uint)m_Elements.Count; }
        }

        public bool EnableCache
        {
            get { return m_EnableCache; }
            set 
            { 
                m_EnableCache = value;
                if (!m_EnableCache)
                {
                    ClearCache();
                }
            }
        }

        public bool UpperRangeLimitEnabled
        {
            get { return m_UpperRangeLimitEnabled; }
            set { m_UpperRangeLimitEnabled = value; }
        }

        public bool UnderRangeLimitEnabled
        {
            get { return m_UnderRangeLimitEnabled; }
            set { m_UnderRangeLimitEnabled = value; }
        }

        public bool SideRangetLimitEnabled
        {
            get { return m_SideRangetLimitEnabled; }
            set { m_SideRangetLimitEnabled = value; }
        }

        public float UpperRangeLimit
        {
            get { return m_UpperRangeLimit; }
            set { m_UpperRangeLimit = value; }
        }

        public float UnderRangeLimit
        {
            get { return m_UnderRangeLimit; }
            set { m_UnderRangeLimit = value; }
        }

        public float SideRangetLimit
        {
            get { return m_SideRangetLimit; }
            set { m_SideRangetLimit = value; }
        }

        public bool DontDestroyWhenEmpty
        {
            get { return m_DontDestroyWhenEmpty; }
            set { m_DontDestroyWhenEmpty = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion getters / setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void ElementIsPlaced(ABS_BuildingElement p_Element);
        protected abstract void ElementWillBeRemoved(ABS_DestroyActionElementData p_BaseDestroyActionData, 
                                                    ABS_BuildingManagerTracker p_Tracker,
                                                    bool p_TriggeredByHistory,
                                                    bool p_IgnoreStability,
                                                    ABS_BuildingElement p_ElementToRemove);

        protected abstract void ValidatePositionImpl(ABS_PositionValidationData p_ResultData,
                                                     in Vector3 p_LocalPosition,
                                                     in Quaternion p_LocalRotation,
                                                     in ABS_BuildingElement p_ElementForBuild);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_Building(in bool p_CheckX, in bool p_CheckY, in bool p_CheckZ) : base()
        {
            m_Vector3Comparer = new REST_Vector3EqualityComparer(p_CheckX, p_CheckY, p_CheckZ);
            m_Elements = new Dictionary<Vector3, ABS_BuildingElement>(m_Vector3Comparer);
            m_DefaultElements = new List<string>();
            m_Cache = new ABS_BuildingCache(m_Vector3Comparer);
            m_ISaveableType = ABS_SaveableMonobehaviour.DataType.Building;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Main Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void AwakeImpl()
        {
            m_BuildingTransform = gameObject.transform;

            foreach (Transform child in transform)
            {
                ABS_BuildingElement be = child.GetComponent<ABS_BuildingElement>();
                if (be != null)
                {
                    be.ParentBuilding = this;
                    m_Elements[be.transform.localPosition] = be;
                    m_DefaultElements.Add(be.InstanceGuid);
                    ElementIsPlaced(be);
                }
            }

#if UNITY_EDITOR
            if (m_MaximumElementCount < m_Elements.Count)
            {
                REST_Logging.Warrning("ABS_Building",
                    $"More element (count:{m_Elements.Count}) " +
                    $"under the Building ({this.name}) " +
                    $"than the maximum count({m_MaximumElementCount}).");
            }
#endif
        }

        public void OnDestroy()
        {
            if (m_Parent != null)
            {
                m_Parent.BuildingWillBeDestroyed(this);
            }
        }

        private List<ABS_BuildingElement> ChangePreBuiltState(in bool p_State)
        {
            List<ABS_BuildingElement> changeElements = new List<ABS_BuildingElement>();

            foreach ((Vector3 pos, ABS_BuildingElement element) in GetElementsList())
            {
                if (element.PreBuilt ^ p_State)
                {
                    element.PreBuilt = p_State;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(element.gameObject);
                    }
#endif
                    changeElements.Add(element);
                }
            }

            ClearCache();
            return changeElements;
        }

        protected Dictionary<Vector3, ABS_BuildingElement> GetElementsList()
        {
            Dictionary<Vector3, ABS_BuildingElement> elements = m_Elements;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_BuildingTransform = transform;

                elements = new Dictionary<Vector3, ABS_BuildingElement>(m_Vector3Comparer);
                foreach (Transform child in m_BuildingTransform)
                {
                    ABS_BuildingElement be = child.GetComponent<ABS_BuildingElement>();
                    if (be != null)
                    {
                        elements[be.transform.localPosition] = be;
                        ElementIsPlaced(be);
                    }
                }

                m_Elements = elements;

                if (m_MaximumElementCount < m_Elements.Count)
                {
                    REST_Logging.Debug("ABS_Building", $"More element (count:{m_Elements.Count}) under the ABS_Building ({this.name}) than the maximum count({m_MaximumElementCount}).");
                }
            }
#endif
            return elements;
        }

        public int CheckCacheSize()
        {
            return m_Cache.CheckCacheSize();
        }

        public void ValidatePosition(ABS_PositionValidationData p_ResultData, in Vector3 p_LocalPosition, in Quaternion p_LocalRotation, ABS_BuildingElement p_ElementForBuild)
        {
            if ((m_UpperRangeLimitEnabled && p_LocalPosition.y > m_UpperRangeLimit)
                || (m_UnderRangeLimitEnabled && p_LocalPosition.y < m_UnderRangeLimit)
                || (m_SideRangetLimitEnabled && m_SideRangetLimit < Vector3.Distance(new Vector3(p_LocalPosition.x, 0f, p_LocalPosition.z), Vector3.zero)))
            {
                p_ResultData.m_Result.ParentBuildingValidation_BreakRangeLimitRules = ABS_PositionValidationResult.ResultOptions.Failed;
            }

            if (m_EnableCache)
            {
                string guid = p_ElementForBuild.PrefabGuid;
                ABS_PositionValidationData res = m_Cache.GetCacheData(p_LocalPosition, p_LocalRotation.eulerAngles, guid);
                if (res != null)
                {
                    p_ResultData.m_Result.ParentCachedResult = true;
                    p_ResultData.m_Result.ParentBuildingValidation_ValidatedByPreBuilt = res.m_Result.ParentBuildingValidation_ValidatedByPreBuilt;
                    p_ResultData.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings = res.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings;
                    p_ResultData.m_Result.ParentBuildingValidation_UsedPosition = res.m_Result.ParentBuildingValidation_UsedPosition;
                    p_ResultData.m_Result.ParentBuildingValidation_InvalidPosition = res.m_Result.ParentBuildingValidation_InvalidPosition;
                    p_ResultData.m_Result.ParentBuildingValidation_BreakRangeLimitRules = res.m_Result.ParentBuildingValidation_BreakRangeLimitRules;
                    p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules = res.m_Result.ParentBuildingValidation_BreakPositionRules;
                    p_ResultData.m_Result.ParentBuildingValidation_BreakPositionRules_Denied = res.m_Result.ParentBuildingValidation_BreakPositionRules_Denied;
                    return;
                }

                ValidatePositionImpl(p_ResultData, p_LocalPosition, p_LocalRotation, p_ElementForBuild);
                m_Cache.SaveResultToCache(p_LocalPosition, p_LocalRotation.eulerAngles, guid, p_ResultData);
            }
            else
            {
                ValidatePositionImpl(p_ResultData, p_LocalPosition, p_LocalRotation, p_ElementForBuild);
            }
        }

        //If this function return false then the Parent Building validation failed
        //If it is true then the validation can be continued
        protected bool CheckUsedPosition(in Vector3 p_LocalPosition,
                                         in ABS_BuildingElement p_ElementForBuild,
                                         in ABS_PositionValidationData p_ResultData)
        {
            if (m_Elements.ContainsKey(p_LocalPosition))
            {
                p_ResultData.m_Result.ParentBuildingValidation_UsedPosition = ABS_PositionValidationResult.ResultOptions.Failed;
                ABS_BuildingElement conflictElement = m_Elements[p_LocalPosition];

                if (conflictElement.PreBuilt
                    && (conflictElement.PrefabGuid == p_ElementForBuild.PrefabGuid
                        || (p_ElementForBuild.FinalElement != null
                            && p_ElementForBuild.SnapToPreBuiltFinalElement
                            && conflictElement.PrefabGuid == p_ElementForBuild.FinalElement.PrefabGuid)))
                {
                    //the original element can be swaped because of the prebuilt Logic
                    p_ResultData.m_Result.ParentBuildingValidation_ValidatedByPreBuilt = ABS_PositionValidationResult.ResultOptions.Validated;
                }
                else
                {
                    //check if the the original element can be swaped because of the override Logic
                    if (CheckOverridePossibility(p_ElementForBuild, conflictElement))
                    {
                        p_ResultData.m_ElementTarget_Override = conflictElement;
                        p_ResultData.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings = ABS_PositionValidationResult.ResultOptions.Validated;
                    }
                    else
                    {
                        p_ResultData.m_Result.ParentBuildingValidation_ValidatedByOverrideSettings = ABS_PositionValidationResult.ResultOptions.Failed;
                        return false;
                    }
                }
            }
            else
            {
                p_ResultData.m_Result.ParentBuildingValidation_UsedPosition = ABS_PositionValidationResult.ResultOptions.Validated;
            }
            return true;
        }

        protected bool CheckOverridePossibility(ABS_BuildingElement p_ElementForBuild, ABS_BuildingElement p_OverrideTarget)
        {
            ABS_BuilderBaseSettings settings = p_ElementForBuild.PositionAlgorithmSettings;
            switch (settings.OverrideStrategy)
            {
                case ABS_OverrideStrategy.Ruleset:
                    if(settings.OverrideElementRuleset == null)
                    {
                        REST_Logging.Warrning("ABS_Building", "Rulset Override strategy with null OverrideRuleset.");
                        return false;
                    }
                    return settings.OverrideElementRuleset.CanOverride(p_OverrideTarget, p_ElementForBuild);
                case ABS_OverrideStrategy.None:
                default:
                    return false;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Main Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Implementation ABS_IBuildingExternalInterface
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildActionElementData AddBuildingElement(
            ABS_BuildingManagerTracker p_Tracker,
            bool p_TriggeredByHistory,
            ABS_BuildingElement p_NewElement,
            Vector3 p_LocalPosition,
            Vector3 p_LocalEulerAngles,
            bool p_Force,
            bool p_DestroyOld)
        {
            if (FreeSpace == 0)
            {
                REST_Logging.Error($"{this}", $"No more free space left for Building : {this.name}");
                return null;
            }

            ABS_BuildActionElementData actionElementData = new ABS_BuildActionElementData();

            ABS_BuildingElement oldElement = null;
            if (m_Elements.TryGetValue(p_LocalPosition, out oldElement) && oldElement != null)
            {
                if (p_Force)
                {
                    ElementWillBeRemoved(null, p_Tracker, p_TriggeredByHistory, true, oldElement);
                    if (p_DestroyOld)
                    {
                        ABS_BuildingElement.TransferConnection(oldElement, p_NewElement);
                        ABS_DestroyActionElementData destroyActionData = oldElement.Destroy(p_Tracker, p_TriggeredByHistory, true, true);
                        actionElementData.AddModifiedElement(destroyActionData);
                    }

                    //the element in the m_Elements will be overriden so it does not need to be cleared at this point
                    oldElement.ParentBuilding = null;
                }
                else
                {
                    REST_Logging.Error($"{this}", $"Couldn't add element to ABS_Building." +
                        $"\n Element : {p_NewElement.name}" +
                        $"\n InstanceID : {p_NewElement.InstanceGuid}" +
                        $"\n PrefabGuid : {p_NewElement.PrefabGuid}" +
                        $"\n p_LocalPosition : {p_LocalPosition}" +
                        $"\n p_LocalEulerAngles : {p_LocalEulerAngles}" + 
                        $"\n p_Force : {p_Force}" + 
                        $"\n p_DestroyOld : {p_DestroyOld}" + 
                        $"\n Building : {this.name}");
                    return null;
                }
            }

            Transform elementTransform = p_NewElement.gameObject.transform;
            elementTransform.parent = m_BuildingTransform;
            elementTransform.localPosition = p_LocalPosition;
            elementTransform.localEulerAngles = p_LocalEulerAngles;

            m_Elements[elementTransform.localPosition] = p_NewElement;

            p_NewElement.ParentBuilding = this;
            p_NewElement.State = ABS_BuildingElementState.NORMAL;
            p_NewElement.EnableCollider(true);

            ElementIsPlaced(p_NewElement);
            ClearCache();

            return actionElementData;
        }


        public void RemoveBuildingElement(
            ABS_DestroyActionElementData p_BaseDestroyActionData, 
            ABS_BuildingManagerTracker p_Tracker,
            bool p_TriggeredByHistory,
            bool p_IgnoreStability,
            in ABS_BuildingElement p_ElementToRemove,
            bool p_DontDestroyTheBuidling,
            out bool p_IsBuildingDestroyed)
        {
            ElementWillBeRemoved(p_BaseDestroyActionData, p_Tracker, p_TriggeredByHistory, p_IgnoreStability, p_ElementToRemove);
            m_Elements.Remove(p_ElementToRemove.transform.localPosition);
            m_DefaultElements.Remove(p_ElementToRemove.InstanceGuid);
            ClearCache();
            p_ElementToRemove.ParentBuilding = null;

            if (m_Elements.Count == 0 && !m_DontDestroyWhenEmpty && !p_DontDestroyTheBuidling)
            {
                p_IsBuildingDestroyed = true;
                if (p_Tracker != null)
                {
                    p_Tracker.BuildingWillBeDestroyed(this);
                }
                Destroy(p_Tracker, p_TriggeredByHistory);
            }
            else
            {
                p_IsBuildingDestroyed = false;
            }
        }

        public ABS_BuildingElement FindBuildingElement(in Vector3 p_Position, in bool p_TransformPositionToLocal)
        {
            Vector3 localPosition = p_TransformPositionToLocal ? m_BuildingTransform.InverseTransformPoint(p_Position) : p_Position;
            Dictionary<Vector3, ABS_BuildingElement> elements = GetElementsList();

            ABS_BuildingElement element = null;
            elements.TryGetValue(localPosition, out element);
            return element;
        }

        public bool ContainsBuildingElement(in ABS_BuildingElement p_Element)
        {
            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (element == p_Element)
                {
                    return true;
                }
            }
            return false;
        }

        public bool GetPositionOfBuildingElement(in ABS_BuildingElement p_Element, out Vector3 p_PositionResult)
        {
            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (element == p_Element)
                {
                    p_PositionResult = pos;
                    return true;
                }
            }
            p_PositionResult = Vector3.zero;
            return false;
        }

        public ABS_BuildingElement FindBuildingElementBasedInstanceGuid(in string p_InstanceGuid)
        {
            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (string.Compare(element.InstanceGuid, p_InstanceGuid) == 0)
                {
                    return element;
                }
            }

            return null;
        }

        public List<ABS_BuildingElement> FindAllBuildingElementBasedPrefab(in string p_PrefabGuid)
        {
            List<ABS_BuildingElement> result = new List<ABS_BuildingElement>();

            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (string.Compare(element.PrefabGuid, p_PrefabGuid) == 0)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public List<ABS_BuildingElement> FindAllBuildingElement(in ABS_BuilderBaseSettings p_Settings)
        {
            List<ABS_BuildingElement> result = new List<ABS_BuildingElement>();

            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (element.PositionAlgorithmSettings == p_Settings)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public List<ABS_BuildingElement> FindAllBuildingElement(in ABS_BuildingElementAreaType p_AreaType)
        {
            List<ABS_BuildingElement> result = new List<ABS_BuildingElement>();

            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (element.AreaType == p_AreaType)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public List<ABS_BuildingElement> FindAllPreBuiltBuildingElement()
        {
            List<ABS_BuildingElement> result = new List<ABS_BuildingElement>();

            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if (element.PreBuilt)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public List<ABS_BuildingElement> FindAllFoundationBuildingElement(in bool p_Inverse = false)
        {
            List<ABS_BuildingElement> result = new List<ABS_BuildingElement>();

            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                if ((p_Inverse && !element.Foundation) || (!p_Inverse && element.Foundation))
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public List<(ABS_BuildingElement, ABS_BuildingElement)> FindAllAndReplaceElements(in ABS_BuildingElement p_ReplaceTarget, in ABS_BuildingElement p_ReplaceElement, in bool p_DestroyOld)
        {
            if (!CheckFindAndReplaceParameters(p_ReplaceTarget, p_ReplaceElement))
            {
                return null;
            }

            string targetGuid = p_ReplaceTarget.PrefabGuid;

            List<(ABS_BuildingElement, ABS_BuildingElement)> replacedElements = new List<(ABS_BuildingElement, ABS_BuildingElement)>();

            Dictionary<Vector3, ABS_BuildingElement> elements = GetElementsList();

            foreach ((Vector3 pos, ABS_BuildingElement oldElement) in elements)
            {
                if (oldElement.PrefabGuid == targetGuid)
                {
                    ABS_BuildingElement newElement = Instantiate(p_ReplaceElement, m_BuildingTransform);
                    replacedElements.Add((oldElement, newElement));
                }
            }

            foreach ((ABS_BuildingElement, ABS_BuildingElement) pair in replacedElements)
            {
                ABS_BuildingElement oldElement = pair.Item1;
                ABS_BuildingElement newElement = pair.Item2;

                Transform oldElementTransform = oldElement.transform;

                AddBuildingElement(
                        p_Tracker: null,
                        p_TriggeredByHistory: false,
                        p_NewElement: newElement,
                        p_LocalPosition: oldElementTransform.localPosition,
                        p_LocalEulerAngles: oldElementTransform.localEulerAngles,
                        p_Force: true,
                        p_DestroyOld: false);

                if (p_DestroyOld)
                {
                    oldElement.Destroy(null, false, true, true);
                }
            }

            //In case of destroy the first elements will be null!!
            ClearCache();
            return replacedElements;
        }

        private bool CheckFindAndReplaceParameters(in ABS_BuildingElement p_ReplaceTarget, in ABS_BuildingElement p_ReplaceElement)
        {
            if (p_ReplaceTarget == null 
                || p_ReplaceElement == null
                || p_ReplaceTarget.PositionSearchAlgorithm != PositionSearchAlgorithmType
                || p_ReplaceElement.PositionSearchAlgorithm != PositionSearchAlgorithmType
                || p_ReplaceTarget.PrefabGuid == p_ReplaceElement.PrefabGuid)
            {
                return false;
            }

            if (PositionSearchAlgorithmType == ABS_PositionSearchAlgorithm.AdvancedGrid)
            {
                ABS_AdvancedGridType fromType = p_ReplaceTarget.AdvancedGridType;
                ABS_AdvancedGridType toType = p_ReplaceElement.AdvancedGridType;
                if (fromType != toType)
                {
                    return false;
                }

                if (fromType == ABS_AdvancedGridType.Wall || fromType == ABS_AdvancedGridType.EdgeHorizontal)
                {
                    ABS_AdvancedGridAxisType fromAxisType = p_ReplaceTarget.AdvancedGridAxisType;
                    ABS_AdvancedGridAxisType toAxisType = p_ReplaceElement.AdvancedGridAxisType;
                    if (fromAxisType != toAxisType)
                    {
                        return false;
                    }
                }

                if (p_ReplaceTarget.SnapPointRuleSet != null && p_ReplaceTarget.SnapPointRuleSet != p_ReplaceElement.SnapPointRuleSet)
                {
                    return false;
                }
            }
            return true;
        }

        public List<ABS_BuildingElement> MakePreBuilt()
        {
            return ChangePreBuiltState(true);
        }

        public List<ABS_BuildingElement> RemovePreBuilt()
        {
            return ChangePreBuiltState(false);
        }

        public void SetMaterialToDefault()
        {
            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                element.SetMaterialToDefault();
            }
        }

        public void SetMaterialBasedOnState()
        {
            foreach ((Vector3 pos, ABS_BuildingElement element) in m_Elements)
            {
                element.SetMaterialBasedOnState();
            }
        }

        public void ClearCache()
        {
            m_Cache.Clear();
        }

        public void EnableCahce()
        {
            EnableCache = true;
        }

        public void DisableCahce()
        {
            EnableCache = false;
        }

        public void Destroy(ABS_BuildingManagerTracker p_Tracker, in bool p_TriggeredByHistory)
        {
            if (p_Tracker != null)
            {
                if (p_TriggeredByHistory)
                {
                    p_Tracker.BuildingWillBeDestroyed(this);
                }
                else
                {
                    p_Tracker.BuildingWillBeHistoryDestroyed(this);
                }
            }

            Destroy(gameObject);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Implementation ABS_IBuildingExternalInterface
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency : Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [System.Serializable]
        public abstract class ABS_BuildingPersistedData : ABS_PersistedData
        {
            public int PositionSearchAlgorithmType = 0;

            public Vector3 WorldPosition = Vector3.zero;
            public Vector3 WorldEulerAngles = Vector3.zero;

            public uint MaximumElementCount = 1000;
            public bool EnableCache = true;

            public bool UpperRangeLimitEnabled = false;
            public bool UnderRangeLimitEnabled = false;
            public bool SideRangetLimitEnabled = false;
            public float UpperRangeLimit = 1000f;
            public float UnderRangeLimit = -1000f;
            public float SideRangetLimit = 1000f;

            public List<string> RemainingDefaultElements = new List<string>();
            public List<ABS_BuildingElement.ABS_BuildingElementPersistedData> Elements = 
                new List<ABS_BuildingElement.ABS_BuildingElementPersistedData>();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency : Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency : Abstract Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl(ABS_BuildingPersistedData p_Data);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency : Abstract Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency : Public Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static ABS_PersistencyLoadErrorCode CreateFromPersistedJSON(
            in string p_JsonString, 
            in ABS_BuildingParent p_Parent, 
            in ABS_BuildingElementList p_ElementList,
            out ABS_Building p_NewBuilding,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            if (string.IsNullOrEmpty(p_JsonString))
            {
                p_NewBuilding = null;
                return ABS_PersistencyLoadErrorCode.JSON_NullOrEmptyString;
            }

            ABS_BuildingPersistedData data = ABS_PersistencyManager.FromJson<ABS_BuildingPersistedData>(p_JsonString);
            if (data == null)
            {
                p_NewBuilding = null;
                return ABS_PersistencyLoadErrorCode.JSON_ErrorDuringLoad;
            }

            return CreateFromPersistedData(data, p_Parent, p_ElementList, out p_NewBuilding, p_ElementConnections);
        }

        public static ABS_PersistencyLoadErrorCode CreateFromPersistedData(
            in ABS_BuildingPersistedData p_PersistedData, 
            in ABS_BuildingParent p_Parent, 
            in ABS_BuildingElementList p_ElementList,
            out ABS_Building p_NewBuilding,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            if (p_PersistedData == null)
            {
                p_NewBuilding = null;
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            if (p_PersistedData.Type != ABS_SaveableMonobehaviour.DataType.Building)
            {
                p_NewBuilding = null;
                return ABS_PersistencyLoadErrorCode.PersistedData_WrongDataType;
            }

            if (p_ElementList == null)
            {
                p_NewBuilding = null;
                return ABS_PersistencyLoadErrorCode.BuildingElementList_NullInput;
            }

            if (p_Parent != null)
            {
                ABS_Building buildingForCheck = p_Parent.GetBuilding(p_PersistedData.WorldPosition);
                if (buildingForCheck != null)
                {
                    p_NewBuilding = null;
                    return ABS_PersistencyLoadErrorCode.Building_AlreadyUsedPosition;
                }
            }

            Transform transform = p_Parent == null ? null : p_Parent.transform;
            ABS_PersistencyLoadErrorCode result = ABS_PersistencyLoadErrorCode.Unkown;
            ABS_Building createdBuilding = null;
            switch ((ABS_PositionSearchAlgorithm)p_PersistedData.PositionSearchAlgorithmType)
            {
                case ABS_PositionSearchAlgorithm.SnapPointBased:
                    result = CreateFromPersistedDataImpl
                        <ABS_SnapPointBasedBuilding, ABS_SnapPointBasedBuilding.ABS_SnapPointBasedBuildingPersistedData>
                        (p_PersistedData, transform, p_ElementList, out createdBuilding, p_ElementConnections);
                    break;
                case ABS_PositionSearchAlgorithm.AdvancedGrid:
                    result = CreateFromPersistedDataImpl
                        <ABS_AdvancedGridBuilding, ABS_AdvancedGridBuilding.ABS_AdvancedGridBuildingPersistedData>
                        (p_PersistedData, transform, p_ElementList, out createdBuilding, p_ElementConnections);
                    break;
                case ABS_PositionSearchAlgorithm.BasicGrid:
                    result = CreateFromPersistedDataImpl
                        <ABS_BasicGridBuilding, ABS_BasicGridBuilding.ABS_BasicGridBuildingPersistedData>
                        (p_PersistedData, transform, p_ElementList, out createdBuilding, p_ElementConnections);
                    break;
                case ABS_PositionSearchAlgorithm.Free:
                    result = CreateFromPersistedDataImpl
                        <ABS_FreeBuilding, ABS_FreeBuilding.ABS_FreeBuildingPersistedData>
                        (p_PersistedData, transform, p_ElementList, out createdBuilding, p_ElementConnections);
                    break;
            }

            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                p_NewBuilding = null;
                return result;
            }

            p_NewBuilding = createdBuilding;
            p_NewBuilding.Parent = p_Parent;
            return ABS_PersistencyLoadErrorCode.Successful;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency : Public Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency : Private Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl<BuildingType, PersistedDataType>(
            in ABS_BuildingPersistedData p_PersistedData, 
            in Transform p_Parent, 
            in ABS_BuildingElementList p_ElementList,
            out ABS_Building p_ResultBuilding,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
            where BuildingType : ABS_Building
            where PersistedDataType : ABS_BuildingPersistedData
        {
            GameObject gameObject = new GameObject(p_PersistedData.Name);
            gameObject.transform.parent = p_Parent;

            p_ResultBuilding = gameObject.AddComponent<BuildingType>();
            if (p_ResultBuilding == null)
            {
                return ABS_PersistencyLoadErrorCode.Building_UnkownInstantiateError;
            }

            p_ResultBuilding.SetBasicPersistedDataValues(p_PersistedData);

            return p_ResultBuilding.CreateFromPersistedDataImpl(p_PersistedData, p_ElementList, p_ElementConnections);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency : Private Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency : Public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected void GetBasePersistedData(ABS_BuildingPersistedData p_Data)
        {
            SaveOwnData(p_Data);
            SaveElementsData(p_Data);
        }

        public ABS_PersistencyLoadErrorCode CreateFromPersistedData(
            in ABS_BuildingPersistedData p_PersistedData, 
            in ABS_BuildingElementList p_ElementList,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            return CreateFromPersistedDataImpl(p_PersistedData, p_ElementList, p_ElementConnections);
        }

        public ABS_PersistencyLoadErrorCode UpdateFromPersistedData(in ABS_BuildingPersistedData p_PersistedData, 
                                                                    in ABS_BuildingElementList p_ElementList, 
                                                                    in bool p_AlreadyChecked,
                                                                    in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            if (!p_AlreadyChecked)
            {
                ABS_PersistencyLoadErrorCode res = CheckUpdatePossibility(p_PersistedData, p_ElementList);
                if (res != ABS_PersistencyLoadErrorCode.Successful)
                {
                    return res;
                }
            }

            Dictionary<ABS_BuildingElement.ABS_BuildingElementPersistedData, ABS_BuildingElement> elementsForUpdate =
                new Dictionary<ABS_BuildingElement.ABS_BuildingElementPersistedData, ABS_BuildingElement>();

            //First try to create the new BuildingElements
            List<ABS_BuildingElement> newlyCreatedElements= new List<ABS_BuildingElement>();
            Dictionary<Vector3, ABS_BuildingElement> elements = GetElementsList();
            ABS_PersistencyLoadErrorCode result = ABS_PersistencyLoadErrorCode.Successful;
            bool problem = false;
            foreach (ABS_BuildingElement.ABS_BuildingElementPersistedData data in p_PersistedData.Elements)
            {
                ABS_BuildingElement element = null;
                if (elements.TryGetValue(data.LocalPosition, out element) && element != null)
                {
                    result = element.CheckUpdatePossibility(data);
                    if (result != ABS_PersistencyLoadErrorCode.Successful)
                    {
                        problem = true;
                        break;
                    }
                    elementsForUpdate[data] = element;
                }
                else
                {

                    ABS_BuildingElement newElement = null;
                    result = CreateElementFromPerssitedData(data, p_ElementList, out newElement, p_ElementConnections);
                    if (result != ABS_PersistencyLoadErrorCode.Successful)
                    {
                        return result;
                    }

                    newlyCreatedElements.Add(newElement);
                }
            }

            //Check the new BuildingElements
            if (problem)
            {
                foreach (ABS_BuildingElement e in newlyCreatedElements)
                {
                    Destroy(e.gameObject);
                }
                return result;
            }

            //At this point all of the BuildingElements should be checked by update possibility point of view
            List<ABS_BuildingElement> updatedElements = new List<ABS_BuildingElement>();
            foreach ((ABS_BuildingElement.ABS_BuildingElementPersistedData data, ABS_BuildingElement element) in elementsForUpdate)
            {
                result = element.UpdateDataFromPersistedData(data, true, p_ElementConnections);
                if (result != ABS_PersistencyLoadErrorCode.Successful)
                {
                    foreach (ABS_BuildingElement e in newlyCreatedElements)
                    {
                        Destroy(e.gameObject);
                    }

                    string errorMsg = $"The Update failed on the Building : {this.name}\n" +
                        $"New BuildingElements has been removed!\n";

                    if (updatedElements.Count > 0)
                    {
                        errorMsg += $"The Following BuildingElements has been already updated:";
                        foreach (ABS_BuildingElement elementForError in updatedElements)
                        {
                            errorMsg += $"\n    {elementForError.name}";
                        }
                    }

                    REST_Logging.Warrning("ABS_Building", errorMsg);

                    return result;
                }
                else
                {
                    updatedElements.Add(element);
                }
            }

            RemoveDestroyedDefaultElements(p_PersistedData);

            return ABS_PersistencyLoadErrorCode.Successful;
        }

        public ABS_PersistencyLoadErrorCode CheckUpdatePossibility(in ABS_BuildingPersistedData p_PersistedData, in ABS_BuildingElementList p_ElementList)
        {
            if (p_PersistedData == null)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            if (p_PersistedData.Type != ABS_SaveableMonobehaviour.DataType.Building)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_WrongDataType;
            }

            if (p_ElementList == null)
            {
                return ABS_PersistencyLoadErrorCode.BuildingElementList_NullInput;
            }

            if (!CompareBasicPersistedDataValues(p_PersistedData))
            {
                return ABS_PersistencyLoadErrorCode.Update_WrongBasicObjectValues_Building;
            }

            Dictionary<Vector3, ABS_BuildingElement> elements = GetElementsList();
            foreach (ABS_BuildingElement.ABS_BuildingElementPersistedData data in p_PersistedData.Elements)
            {
                ABS_BuildingElement element = null;
                if (elements.TryGetValue(data.LocalPosition, out element) && element != null)
                {
                    ABS_PersistencyLoadErrorCode res = element.CheckUpdatePossibility(data);
                    if (res != ABS_PersistencyLoadErrorCode.Successful)
                    {
                        REST_Logging.Warrning("ABS_Building", $"Checking the update possibility of the BuildingElement has been failed. Name: {element.name}");
                        return res;
                    }
                }
            }

            return ABS_PersistencyLoadErrorCode.Successful;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency : Public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency : Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void SaveOwnData (ABS_BuildingPersistedData p_PersistedData)
        {
            GetBasicPersistedDataValues(p_PersistedData as ABS_PersistedData);

            p_PersistedData.PositionSearchAlgorithmType = (int)m_PositionSearchAlgorithmType;

            p_PersistedData.WorldPosition = transform.position;
            p_PersistedData.WorldEulerAngles = transform.eulerAngles;

            p_PersistedData.MaximumElementCount = m_MaximumElementCount;
            p_PersistedData.EnableCache = m_EnableCache;

            p_PersistedData.UpperRangeLimitEnabled = m_UpperRangeLimitEnabled;
            p_PersistedData.UnderRangeLimitEnabled = m_UnderRangeLimitEnabled;
            p_PersistedData.SideRangetLimitEnabled = m_SideRangetLimitEnabled;
            p_PersistedData.UpperRangeLimit = m_UpperRangeLimit;
            p_PersistedData.UnderRangeLimit = m_UnderRangeLimit;
            p_PersistedData.SideRangetLimit = m_SideRangetLimit;
        }

        private void SaveElementsData (ABS_BuildingPersistedData p_PersisteData)
        {
            Dictionary<Vector3, ABS_BuildingElement> elements = m_Elements;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                elements = new Dictionary<Vector3, ABS_BuildingElement>(m_Vector3Comparer);
                foreach (Transform child in transform)
                {
                    ABS_BuildingElement be = child.GetComponent<ABS_BuildingElement>();
                    if (be != null)
                    {
                        elements[be.transform.localPosition] = be;
                        ElementIsPlaced(be);
                    }
                }
            }
#endif

            foreach ((Vector3 pos, ABS_BuildingElement element) in elements)
            {
                ABS_BuildingElement.ABS_BuildingElementPersistedData elementData =
                    element.GetPersistedData() as ABS_BuildingElement.ABS_BuildingElementPersistedData;
                p_PersisteData.Elements.Add(elementData);
            }

            p_PersisteData.RemainingDefaultElements = m_DefaultElements;
        }

        private void LoadOwnData(ABS_BuildingPersistedData p_PersistedData)
        {
            gameObject.transform.position = p_PersistedData.WorldPosition;
            gameObject.transform.eulerAngles = p_PersistedData.WorldEulerAngles;

            m_MaximumElementCount = p_PersistedData.MaximumElementCount;
            m_EnableCache = p_PersistedData.EnableCache;

            m_UpperRangeLimitEnabled = p_PersistedData.UpperRangeLimitEnabled;
            m_UnderRangeLimitEnabled = p_PersistedData.UnderRangeLimitEnabled;
            m_SideRangetLimitEnabled = p_PersistedData.SideRangetLimitEnabled;
            m_UpperRangeLimit = p_PersistedData.UpperRangeLimit;
            m_UnderRangeLimit = p_PersistedData.UnderRangeLimit;
            m_SideRangetLimit = p_PersistedData.SideRangetLimit;
        }

        private ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl(
            ABS_BuildingPersistedData p_PersistedData, 
            in ABS_BuildingElementList p_ElementList,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            if (p_PersistedData == null)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            if (p_PersistedData.Type != ABS_SaveableMonobehaviour.DataType.Building)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_WrongDataType;
            }

            if (p_ElementList == null)
            {
                return ABS_PersistencyLoadErrorCode.BuildingElementList_NullInput;
            }

            Dictionary<string, ABS_BuildingElement> elementDict = p_ElementList.BuildingElementsDict;
            ABS_PersistencyLoadErrorCode err = ABS_PersistencyLoadErrorCode.Successful;
            foreach (ABS_BuildingElement.ABS_BuildingElementPersistedData element in p_PersistedData.Elements)
            {
                ABS_BuildingElement newElement = null;
                err = CreateElementFromPerssitedData(element, p_ElementList, out newElement, p_ElementConnections);
                if (err != ABS_PersistencyLoadErrorCode.Successful)
                {
                    return err;
                }
            }

            LoadOwnData(p_PersistedData);
            err = CreateFromPersistedDataImpl(p_PersistedData);
            if (err != ABS_PersistencyLoadErrorCode.Successful)
            {
                return err;
            }

            RemoveDestroyedDefaultElements(p_PersistedData);

            return ABS_PersistencyLoadErrorCode.Successful;
        }

        private void RemoveDestroyedDefaultElements(ABS_BuildingPersistedData p_PersistedData)
        {
            List<string> elementsForRemove = new List<string>();
            foreach (string instanceID in m_DefaultElements)
            {
                if (p_PersistedData.RemainingDefaultElements.Find(item => string.Compare(item, instanceID) == 0) == null)
                {
                    elementsForRemove.Add(instanceID);
                }
            }

            foreach (string instanceID in elementsForRemove)
            {
                ABS_BuildingElement foundElement = m_Elements.Values.FirstOrDefault(element => element.InstanceGuid == instanceID);
                if (foundElement != null)
                {
                    Destroy(foundElement.gameObject);
                }
            }
        }

        private ABS_PersistencyLoadErrorCode CreateElementFromPerssitedData (
            ABS_BuildingElement.ABS_BuildingElementPersistedData p_ElementData,
            in ABS_BuildingElementList p_ElementList,
            out ABS_BuildingElement p_NewElement,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            ABS_BuildingElement buildingElementPrefab = p_ElementList.GetElementPrefab(p_ElementData.PrefabGuid);
            if (buildingElementPrefab == null)
            {
                REST_Logging.Warrning("ABS_Building",
                    $"Can't find element in the provided BuildingElementList. Element : {p_ElementData.Name} PrefabGUID : {p_ElementData.PrefabGuid}");
                p_NewElement = null;
                return ABS_PersistencyLoadErrorCode.BuildingElementList_MissingElement;
            }

            return ABS_BuildingElement.CreateFromPersistedData(p_ElementData, this, buildingElementPrefab, out p_NewElement, p_ElementConnections);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency : Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}
//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_BuildingParent : ABS_SaveableMonobehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<string> m_DefaultBuildings = null;
        private Dictionary<string, ABS_Building> m_Buildings = null;

        private bool m_Initalized = false;

        [SerializeField] private ABS_BasicGridBuilding m_GlobalBasicGridParent = null;
        [SerializeField] private ABS_FreeBuilding m_GlobalFreeParent = null;

        [SerializeField] private string m_DefaultBuildingName = "ABS_Building";
        [SerializeField] private bool m_EnableCache = true;

        [SerializeField] private bool m_AdvancedGridStabilityEnabled = false;
        [SerializeField][Range(3, 10)] private short m_AdvancedGridStabilityLevel = 3;

        [SerializeField] private uint m_MaximumElementFreeBuilding = 1000;
        [SerializeField] private uint m_MaximumElementBasicGridBuilding = 1000;
        [SerializeField] private uint m_MaximumElementAdvancedGridBuilding = 1000;
        [SerializeField] private uint m_MaximumElementSnapPointBasedBuidling = 1000;

        [SerializeField] private bool m_UpperRangeLimitEnabled = false;
        [SerializeField] private bool m_UnderRangeLimitEnabled = false;
        [SerializeField] private bool m_SideRangetLimitEnabled = false;
        [SerializeField] private float m_UpperRangeLimit = 1000f;
        [SerializeField] private float m_UnderRangeLimit = -1000f;
        [SerializeField] private float m_SideRangetLimit = 1000f;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BasicGridBuilding GlobalBasicGridParent => m_GlobalBasicGridParent;
        public ABS_FreeBuilding GlobalFreeParent => m_GlobalFreeParent;
        public bool AdvancedGridStabilityEnabled => m_AdvancedGridStabilityEnabled;
        public short AdvancedGridStabilityLevel => m_AdvancedGridStabilityLevel;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingParent() : base()
        {
            m_ISaveableType = ABS_SaveableMonobehaviour.DataType.BuildingParent;
        }

        public void BuildingWillBeDestroyed(in ABS_Building p_Building)
        {
            if (m_Buildings != null)
            {
                m_Buildings.Remove(p_Building.InstanceGuid);
            }
            if (m_DefaultBuildings != null)
            {
                m_DefaultBuildings.Remove(p_Building.InstanceGuid);
            }
        }

        public ABS_Building GetParentForNewBuildingElementHistory(
            ABS_BuildingElement p_TargetElement,
            Vector3 p_Position,
            Vector3 p_EulerAngles,
            ABS_IBuildingManagerInternalInterface p_Manager,
            ABS_BuildingManagerTracker p_Tracker,
            string p_BuildingInstanceGuid)
        {
            return GetParentForNewBuildingElementImpl(
                p_TargetElement,
                p_Position,
                p_EulerAngles,
                p_Manager,
                p_Tracker,
                true,
                p_BuildingInstanceGuid);
        }

        public ABS_Building GetParentForNewBuildingElement(
            ABS_BuildingElement p_TargetElement,
            Vector3 p_Position,
            Vector3 p_EulerAngles,
            ABS_IBuildingManagerInternalInterface p_Manager,
            ABS_BuildingManagerTracker p_Tracker)
        {
            return GetParentForNewBuildingElementImpl(
                p_TargetElement,
                p_Position,
                p_EulerAngles,
                p_Manager,
                p_Tracker,
                false,
                string.Empty);
        }

        private ABS_Building GetParentForNewBuildingElementImpl(
            ABS_BuildingElement p_TargetElement,
            Vector3 p_Position,
            Vector3 p_EulerAngles,
            ABS_IBuildingManagerInternalInterface p_Manager,
            ABS_BuildingManagerTracker p_Tracker,
            bool p_History,
            string p_BuildingInstanceGuid)
        {
            Init();
            ABS_Building building = null;
            switch (p_TargetElement.PositionSearchAlgorithm)
            {
                case ABS_PositionSearchAlgorithm.Free:
                    {
                        building = GetFreeBuildingParent();
                    }
                    break;
                case ABS_PositionSearchAlgorithm.BasicGrid:
                    {
                        building = GetBasicGridParent();
                    }
                    break;
                case ABS_PositionSearchAlgorithm.AdvancedGrid:
                    {
                        ABS_AdvancedGridBuilding buildingTMP = CreateBuildingBaseObject<ABS_AdvancedGridBuilding>(m_DefaultBuildingName, p_Position, p_EulerAngles);
                        buildingTMP.BuildingPositionModifier = p_Manager.GetParentPositionAlignment(p_TargetElement);
                        buildingTMP.StabilityLevel = m_AdvancedGridStabilityLevel;
                        building = buildingTMP;

                        if (p_History)
                        {
                            buildingTMP.InstanceGuid = p_BuildingInstanceGuid;
                        }
                        m_Buildings[building.InstanceGuid] = building;

                        buildingTMP.MaximumElementCount = m_MaximumElementAdvancedGridBuilding;
                        buildingTMP.EnableStability = m_AdvancedGridStabilityEnabled;

                    }
                    break;
                case ABS_PositionSearchAlgorithm.SnapPointBased:
                    {
                        ABS_SnapPointBasedBuilding buildingTMP = CreateBuildingBaseObject<ABS_SnapPointBasedBuilding>(m_DefaultBuildingName, p_Position, p_EulerAngles);
                        building = buildingTMP;

                        if (p_History)
                        {
                            buildingTMP.InstanceGuid = p_BuildingInstanceGuid;
                        }
                        m_Buildings[building.InstanceGuid] = building;

                        buildingTMP.MaximumElementCount = m_MaximumElementSnapPointBasedBuidling;
                    }
                    break;
            }

            if (p_History)
            {
                p_Tracker.BuildingHistoryPlaced(building);
            }
            else
            {
                p_Tracker.BuildingPlaced(building);
            }

            return building;
        }

        public ABS_BasicGridBuilding GetBasicGridParent()
        {
            Init();
            if (m_GlobalBasicGridParent == null)
            {
                m_GlobalBasicGridParent = CreateBuildingBaseObject<ABS_BasicGridBuilding>("BasicGridGlobalParent", Vector3.zero, Vector3.zero);
                m_GlobalBasicGridParent.MaximumElementCount = m_MaximumElementBasicGridBuilding;
                m_GlobalBasicGridParent.DontDestroyWhenEmpty = true;
            }

            return m_GlobalBasicGridParent;
        }

        public ABS_FreeBuilding GetFreeBuildingParent()
        {
            Init();
            if (m_GlobalFreeParent == null)
            {
                m_GlobalFreeParent = CreateBuildingBaseObject<ABS_FreeBuilding>("FreeBuildingGlobalParent", Vector3.zero, Vector3.zero);
                m_GlobalFreeParent.MaximumElementCount = m_MaximumElementFreeBuilding ;
                m_GlobalFreeParent.DontDestroyWhenEmpty = true;
            }

            return m_GlobalFreeParent;
        }
        public ABS_Building GetBuilding(in string p_InstanceGuid)
        {
            Init();
            ABS_Building b = null;
            m_Buildings.TryGetValue(p_InstanceGuid, out b);
            return b;
        }

        public ABS_Building GetBuilding(in Vector3 p_GlobalPosition)
        {
            Init();
            foreach (var item in m_Buildings)
            {
                if (REST_Vector3EqualityComparer.Static_Equals(item.Value.transform.position, p_GlobalPosition))
                {
                    return item.Value;
                }
            }
            return null;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region private / protected  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void AwakeImpl()
        {
        }

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            if(m_Initalized)
            {
                return;
            }

            m_Initalized = true;

            if (m_Buildings == null)
            {
                m_Buildings = new Dictionary<string, ABS_Building>();
            }
            if (m_DefaultBuildings == null)
            {
                m_DefaultBuildings = new List<string>();
            }

            GetBasicGridParent();
            GetFreeBuildingParent();
            GetBuildingChilds(m_Buildings, m_DefaultBuildings);
            m_GlobalFreeParent.DontDestroyWhenEmpty = true;
            m_GlobalBasicGridParent.DontDestroyWhenEmpty = true;
        }

        private void GetBuildingChilds(Dictionary<string, ABS_Building> p_Buildings, List<string> p_DefaultBuildings)
        {
            foreach (Transform child in transform)
            {
                ABS_Building building = child.GetComponent<ABS_Building>();
                if (building != null)
                {
                    building.Parent = this;
                    p_Buildings[building.InstanceGuid] = building;

                    p_DefaultBuildings.Add(building.InstanceGuid);
                }
            }
        }

        private T CreateBuildingBaseObject<T> (string p_Name, Vector3 p_Position, Vector3 p_EulerAngles)
            where T : ABS_Building
        {
            GameObject gameObj = new GameObject(p_Name);
            Transform tr = gameObj.transform;
            tr.parent = transform;
            tr.eulerAngles = p_EulerAngles;
            tr.position = p_Position;

            T building = gameObj.AddComponent<T>();
            building.EnableCache = m_EnableCache;
            building.Parent = this;

            SetLimits(building);

            return building;

        }

        private void SetLimits (ABS_Building p_Building)
        {
            p_Building.UpperRangeLimitEnabled = m_UpperRangeLimitEnabled;
            p_Building.UnderRangeLimitEnabled = m_UnderRangeLimitEnabled;
            p_Building.SideRangetLimitEnabled = m_SideRangetLimitEnabled;
            p_Building.UpperRangeLimit = m_UpperRangeLimit;
            p_Building.UnderRangeLimit = m_UnderRangeLimit;
            p_Building.SideRangetLimit = m_SideRangetLimit;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // private / protected  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Persistency : PersistedData
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [System.Serializable]
        public class ABS_BuildingParentPersistedData : ABS_PersistedData
        {
            public List<string> RemainingDefaultBuildings = new List<string>();

            public string GlobalBasicGridParentGuid;
            public string GlobalFreeParentGuid;

            //Important!!
            //For the proper FreeBuilding linkage the FreeBuildings should be the firstone!!!!
            //Also they should be loaded back first
            public List<ABS_FreeBuilding.ABS_FreeBuildingPersistedData> FreeBuildings
                = new List<ABS_FreeBuilding.ABS_FreeBuildingPersistedData>();
            public List<ABS_BasicGridBuilding.ABS_BasicGridBuildingPersistedData> BasicGridBuildings 
                = new List<ABS_BasicGridBuilding.ABS_BasicGridBuildingPersistedData>();
            public List<ABS_AdvancedGridBuilding.ABS_AdvancedGridBuildingPersistedData> AdvancedGridBuildings
                = new List<ABS_AdvancedGridBuilding.ABS_AdvancedGridBuildingPersistedData>();
            public List<ABS_SnapPointBasedBuilding.ABS_SnapPointBasedBuildingPersistedData> SnapPointBasedBuildings 
                = new List<ABS_SnapPointBasedBuilding.ABS_SnapPointBasedBuildingPersistedData>();

            public override string ToJSON(in bool p_PrettyPrint)
            {
                return ABS_PersistencyManager.ToJson(this, p_PrettyPrint);
            }
        }

        public override string ToJSON(in bool p_PrettyPrint)
        {
            return GetPersistedData().ToJSON(p_PrettyPrint);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Persistency : PersistedData
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Persistency :  Public Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static ABS_PersistencyLoadErrorCode UpdateFromPersistedJSON(in string p_JsonString, 
                                                                           in ABS_BuildingParent p_Target, 
                                                                           in ABS_BuildingElementList p_ElementList)
        {
            if (string.IsNullOrEmpty(p_JsonString))
            {
                return ABS_PersistencyLoadErrorCode.JSON_NullOrEmptyString;
            }

            ABS_BuildingParentPersistedData data = ABS_PersistencyManager.FromJson<ABS_BuildingParentPersistedData>(p_JsonString);
            if (data == null)
            {
                return ABS_PersistencyLoadErrorCode.JSON_ErrorDuringLoad;
            }

            return UpdateFromPersistedData(data, p_Target, p_ElementList);
        }

        public static ABS_PersistencyLoadErrorCode UpdateFromPersistedData(in ABS_BuildingParentPersistedData p_PersistedData,
                                                                           in ABS_BuildingParent p_Target,
                                                                           in ABS_BuildingElementList p_ElementList)
        {
            return p_Target.UpdateFromPersistedDataImpl(p_PersistedData, p_ElementList);
        }

        public static ABS_PersistencyLoadErrorCode CreateFromJSON(
            in string p_JsonString, 
            in GameObject p_Parent, 
            in ABS_BuildingElementList p_ElementList,
            out ABS_BuildingParent p_NewBuildingParent)
        {
            if (string.IsNullOrEmpty(p_JsonString))
            {
                p_NewBuildingParent = null;
                return ABS_PersistencyLoadErrorCode.JSON_NullOrEmptyString;
            }

            ABS_BuildingParentPersistedData data = ABS_PersistencyManager.FromJson<ABS_BuildingParentPersistedData>(p_JsonString);
            if (data == null)
            {
                p_NewBuildingParent = null;
                return ABS_PersistencyLoadErrorCode.JSON_ErrorDuringLoad;
            }

            return CreateFromPersistedData(
                p_PersistedData : data,
                p_Parent : p_Parent,
                p_ElementList : p_ElementList,
                p_NewBuildingParent: out p_NewBuildingParent);
        }

        public static ABS_PersistencyLoadErrorCode CreateFromPersistedData(
            in ABS_BuildingParentPersistedData p_PersistedData, 
            in GameObject p_Parent, 
            in ABS_BuildingElementList p_ElementList,
            out ABS_BuildingParent p_NewBuildingParent)
        {
            GameObject gameObject = new GameObject(p_PersistedData.Name);
            gameObject.transform.parent = p_Parent == null ? null : p_Parent.transform;
            ABS_BuildingParent buildingParent = gameObject.AddComponent<ABS_BuildingParent>();

            ABS_PersistencyLoadErrorCode res = buildingParent.CreateFromPersistedDataImpl(p_PersistedData, p_ElementList);
            if (res != ABS_PersistencyLoadErrorCode.Successful)
            {
                Destroy(gameObject);
            }

            p_NewBuildingParent = buildingParent;
            return res;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Persistency : Public Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Persistency :  Public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingParentPersistedData GetPersistedData()
        {
            Init();
            ABS_BuildingParentPersistedData data = new ABS_BuildingParentPersistedData();
            GetBasicPersistedDataValues(data);

            data.GlobalBasicGridParentGuid = GetBasicGridParent().InstanceGuid;
            data.GlobalFreeParentGuid = GetFreeBuildingParent().InstanceGuid;

            SaveElementsData(data);

            return data;
        }

        public ABS_PersistencyLoadErrorCode CreateFromPersistedData(
            in ABS_BuildingParentPersistedData p_PersistedData, 
            in ABS_BuildingElementList p_ElementList,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            Init();
            if (p_PersistedData == null)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            if (p_PersistedData.Type != ABS_SaveableMonobehaviour.DataType.BuildingParent)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_WrongDataType;
            }

            if (p_ElementList == null)
            {
                return ABS_PersistencyLoadErrorCode.BuildingElementList_NullInput;
            }

            return CreateFromPersistedDataImpl(p_PersistedData, p_ElementList);
        }
        

        public ABS_PersistencyLoadErrorCode UpdateFromPersistedData(ABS_BuildingParentPersistedData p_PersistedData, in ABS_BuildingElementList p_ElementList)
        {
            Init();
            if (p_PersistedData == null)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            if (p_PersistedData.Type != ABS_SaveableMonobehaviour.DataType.BuildingParent)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_WrongDataType;
            }

            if (p_ElementList == null)
            {
                return ABS_PersistencyLoadErrorCode.BuildingElementList_NullInput;
            }

            if (!FixedInstanceGuid)
            {
                REST_Logging.Warrning("ABS_BuildingParent", "The Target BuildingParent's instanceGuid is not fixed! Potentially can cause problems during the load process.");
            }

            return UpdateFromPersistedDataImpl(p_PersistedData, p_ElementList);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Persistency : Public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Persistency :  Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void SaveElementsData(ABS_BuildingParentPersistedData p_PersisteData)
        {
            Dictionary<string, ABS_Building> buildings = GetBuildingsForPersistency();

            foreach ((string instanceID, ABS_Building building) in buildings)
            {
                if (building == null)
                {
                    REST_Logging.Error("ABS_BuildingParent", $"The Building is already destroyed for instanceID: {instanceID}");
                    continue;
                }

                switch (building.PositionSearchAlgorithmType)
                {
                    case ABS_PositionSearchAlgorithm.Free:
                        p_PersisteData.FreeBuildings.Add((building as ABS_FreeBuilding).GetPersistedData());
                        break;
                    case ABS_PositionSearchAlgorithm.BasicGrid:
                        p_PersisteData.BasicGridBuildings.Add((building as ABS_BasicGridBuilding).GetPersistedData());
                        break;
                    case ABS_PositionSearchAlgorithm.AdvancedGrid:
                        p_PersisteData.AdvancedGridBuildings.Add((building as ABS_AdvancedGridBuilding).GetPersistedData());
                        break;
                    case ABS_PositionSearchAlgorithm.SnapPointBased:
                        p_PersisteData.SnapPointBasedBuildings.Add((building as ABS_SnapPointBasedBuilding).GetPersistedData());
                        break;
                }
            }

            p_PersisteData.RemainingDefaultBuildings = m_DefaultBuildings;
        }

        private ABS_PersistencyLoadErrorCode CreateFromPersistedDataImpl(
            in ABS_BuildingParentPersistedData p_PersistedData, 
            in ABS_BuildingElementList p_ElementList)
        {
            Init();
            SetBasicPersistedDataValues(p_PersistedData);

            List<ABS_BuildingElement.ABS_BuildingElementConnectionData> elementConnections = new List<ABS_BuildingElement.ABS_BuildingElementConnectionData>();
            //FreeBuildings should be laoded back first for the proper linkage for the FreeBuildingElements
            ABS_PersistencyLoadErrorCode result = 
                CreateBuildingFromPersistedDataImpl<ABS_FreeBuilding.ABS_FreeBuildingPersistedData>(
                    p_TargetList : p_PersistedData.FreeBuildings,
                    p_ElementList : p_ElementList,
                    p_ElementConnections : elementConnections);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            result = CreateBuildingFromPersistedDataImpl<ABS_BasicGridBuilding.ABS_BasicGridBuildingPersistedData>(
                    p_TargetList: p_PersistedData.BasicGridBuildings,
                    p_ElementList: p_ElementList,
                    p_ElementConnections: elementConnections);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            result = CreateBuildingFromPersistedDataImpl<ABS_AdvancedGridBuilding.ABS_AdvancedGridBuildingPersistedData>(
                    p_TargetList: p_PersistedData.AdvancedGridBuildings,
                    p_ElementList: p_ElementList,
                    p_ElementConnections: elementConnections);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            result = CreateBuildingFromPersistedDataImpl<ABS_SnapPointBasedBuilding.ABS_SnapPointBasedBuildingPersistedData>(
                    p_TargetList: p_PersistedData.SnapPointBasedBuildings,
                    p_ElementList: p_ElementList,
                    p_ElementConnections: elementConnections);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            foreach ((string instanceID, ABS_Building building) in GetBuildingsForPersistency())
            {
                if (string.Compare(instanceID, p_PersistedData.GlobalBasicGridParentGuid) == 0)
                {
                    m_GlobalBasicGridParent = building as ABS_BasicGridBuilding;
                }
                else if (string.Compare(instanceID, p_PersistedData.GlobalFreeParentGuid) == 0)
                {
                    m_GlobalFreeParent = building as ABS_FreeBuilding;
                }
            }

            RemoveDestroyedDefaultBuildings(p_PersistedData);
            return ConnectElement(elementConnections);
        }

        private ABS_PersistencyLoadErrorCode CreateBuildingFromPersistedDataImpl<BuildingPersistedDataType>(
            List<BuildingPersistedDataType> p_TargetList,
            in ABS_BuildingElementList p_ElementList,
            in List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
            where BuildingPersistedDataType : ABS_Building.ABS_BuildingPersistedData
        {
            foreach (BuildingPersistedDataType buildingData in p_TargetList)
            {
                ABS_Building newBuilding = null;
                ABS_PersistencyLoadErrorCode result = ABS_Building.CreateFromPersistedData(
                    p_PersistedData : buildingData,
                    p_Parent : this,
                    p_ElementList : p_ElementList,
                    p_NewBuilding : out newBuilding,
                    p_ElementConnections : p_ElementConnections);
                if (result != ABS_PersistencyLoadErrorCode.Successful)
                {
                    return result;
                }

                m_Buildings.Add(newBuilding.InstanceGuid, newBuilding);
            }
            return ABS_PersistencyLoadErrorCode.Successful;
        }

        private ABS_PersistencyLoadErrorCode UpdateFromPersistedDataImpl(
            in ABS_BuildingParentPersistedData p_PersistedData, 
            in ABS_BuildingElementList p_ElementList)
        {
            Init();
            Dictionary<string, ABS_Building> buildings = GetBuildingsForPersistency();
            Dictionary<ABS_Building.ABS_BuildingPersistedData, ABS_Building> buildingsForUpdate = new Dictionary<ABS_Building.ABS_BuildingPersistedData, ABS_Building>();
            List<ABS_Building.ABS_BuildingPersistedData> buildingsForCreate = new List<ABS_Building.ABS_BuildingPersistedData>();
            ABS_PersistencyLoadErrorCode res = CompareBasicPersistedDataValuesForUpdate(
                p_PersistedData, 
                p_ElementList, 
                buildings, 
                ref buildingsForUpdate,
                ref buildingsForCreate);
            if (res != ABS_PersistencyLoadErrorCode.Successful)
            {
                return res;
            }

            List<ABS_BuildingElement.ABS_BuildingElementConnectionData> elementConnections = new List<ABS_BuildingElement.ABS_BuildingElementConnectionData>();
            //First try to create the new Buildings
            List<ABS_Building> newBuildings = new List<ABS_Building>();
            ABS_PersistencyLoadErrorCode result = ABS_PersistencyLoadErrorCode.Successful;
            bool problem = false;
            foreach (ABS_Building.ABS_BuildingPersistedData data in buildingsForCreate)
            {
                ABS_Building newBuilding = null;
                result = ABS_Building.CreateFromPersistedData(data, this, p_ElementList, out newBuilding, elementConnections);
                if (result != ABS_PersistencyLoadErrorCode.Successful)
                {
                    problem = true;
                    break;
                }
                else if (newBuilding == null)
                {
                    result = ABS_PersistencyLoadErrorCode.Unkown;
                    problem = true;
                    break;
                }
                else
                {
                    newBuildings.Add(newBuilding);
                }
            }

            //Check the new Buildings
            if (problem)
            {
                foreach (ABS_Building building in newBuildings)
                {
                    Destroy(building.gameObject);
                }
                return result;
            }
            else
            {
                foreach (ABS_Building building in newBuildings)
                {
                    m_Buildings[building.InstanceGuid] = building;
                }
            }
            
            List<ABS_Building> updatedBuildings = new List<ABS_Building>();
            foreach ((ABS_Building.ABS_BuildingPersistedData data, ABS_Building building) in buildingsForUpdate)
            {
                result = building.UpdateFromPersistedData(data, p_ElementList, true, elementConnections);
                if (result != ABS_PersistencyLoadErrorCode.Successful)
                {
                    foreach (ABS_Building buildingForDestroy in newBuildings)
                    {
                        Destroy(buildingForDestroy.gameObject);
                    }

                    string errorMsg = $"The Update failed on the Building : {building.name}\n" +
                        $"New Buildings has been removed!\n";

                    if (updatedBuildings.Count > 0)
                    {
                        errorMsg += $"The Following Building has been already updated:";
                        foreach (ABS_Building buildingForError in updatedBuildings)
                        {
                            errorMsg += $"\n    {buildingForError.name}";
                        }
                    }

                    REST_Logging.Warrning("ABS_BuildingParent", errorMsg);
                    return result;
                }
                else
                {
                    updatedBuildings.Add(building);

                    /*foreach (ABS_BuildingElement.ABS_BuildingElementPersistedData elementData in data.Elements)
                    {
                        if (elementData.Connections != null && elementData.Connections.Count > 0)
                        {
                            foreach (ABS_BuildingElement.ABS_BuildingElementPersistedConnectionData connectionData in elementData.Connections)
                            {
                                elementConnections.Add(connectionData);
                            }
                        }
                    }*/
                }
            }

            RemoveDestroyedDefaultBuildings(p_PersistedData);

            return ConnectElement(elementConnections);
        }

        private void RemoveDestroyedDefaultBuildings (ABS_BuildingParentPersistedData p_PersistedData)
        {
            List<string> buildingsForRemove = new List<string>();
            foreach (string instanceID in m_DefaultBuildings)
            {
                if (p_PersistedData.RemainingDefaultBuildings.Find(item => string.Compare(item, instanceID) == 0) == null)
                {
                    buildingsForRemove.Add(instanceID);
                }
            }

            foreach (string instanceID in buildingsForRemove)
            {
                ABS_Building buildingForDestroy = null;
                if (m_Buildings.TryGetValue(instanceID, out buildingForDestroy) && buildingForDestroy != null)
                {
                    Destroy(buildingForDestroy.gameObject);
                }
            }
        }

        private Dictionary<string, ABS_Building> GetBuildingsForPersistency()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || m_Buildings == null)
            {
                if (m_Buildings == null)
                {
                    m_Buildings = new Dictionary<string, ABS_Building>();
                }
                if (m_DefaultBuildings == null)
                {
                    m_DefaultBuildings = new List<string>();
                }
                GetBuildingChilds(m_Buildings, m_DefaultBuildings);
            }
#endif
            return m_Buildings;

        }

        private ABS_PersistencyLoadErrorCode CompareBasicPersistedDataValuesForUpdate (
            in ABS_BuildingParentPersistedData p_PersistedData,
            in ABS_BuildingElementList p_ElementList,
            in Dictionary<string, ABS_Building> p_Buildings,
            ref Dictionary<ABS_Building.ABS_BuildingPersistedData, ABS_Building> p_BuildingsForUpdate,
            ref List<ABS_Building.ABS_BuildingPersistedData> p_BuildingsForCreate)
        {
            if (!CompareBasicPersistedDataValues(p_PersistedData))
            {
                REST_Logging.Warrning("ABS_BuildingParent", $"Comparing the Basic Persisted Data has been failed for the BuildingParent. Name: {this.name}");
                return ABS_PersistencyLoadErrorCode.Update_WrongBasicObjectValues_BuildingParent;
            }

            ABS_PersistencyLoadErrorCode result = CompareBasicPersistedDataValuesForUpdateImpl<ABS_FreeBuilding.ABS_FreeBuildingPersistedData>(
                p_ElementList,
                p_Buildings, 
                ref p_BuildingsForUpdate, 
                ref p_BuildingsForCreate, 
                p_PersistedData.FreeBuildings);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            result = CompareBasicPersistedDataValuesForUpdateImpl<ABS_BasicGridBuilding.ABS_BasicGridBuildingPersistedData>(
            p_ElementList, 
            p_Buildings, 
            ref p_BuildingsForUpdate, 
            ref p_BuildingsForCreate, 
            p_PersistedData.BasicGridBuildings);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            result = CompareBasicPersistedDataValuesForUpdateImpl<ABS_AdvancedGridBuilding.ABS_AdvancedGridBuildingPersistedData>(
            p_ElementList,
            p_Buildings, 
            ref p_BuildingsForUpdate,
            ref p_BuildingsForCreate, 
            p_PersistedData.AdvancedGridBuildings);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            result = CompareBasicPersistedDataValuesForUpdateImpl<ABS_SnapPointBasedBuilding.ABS_SnapPointBasedBuildingPersistedData>(
            p_ElementList, 
            p_Buildings, 
            ref p_BuildingsForUpdate, 
            ref p_BuildingsForCreate, 
            p_PersistedData.SnapPointBasedBuildings);
            if (result != ABS_PersistencyLoadErrorCode.Successful)
            {
                return result;
            }

            return ABS_PersistencyLoadErrorCode.Successful;
        }

        private ABS_PersistencyLoadErrorCode CompareBasicPersistedDataValuesForUpdateImpl<BuildingPersistedDataType>(
            in ABS_BuildingElementList p_ElementList,
            in Dictionary<string, ABS_Building> p_Buildings,
            ref Dictionary<ABS_Building.ABS_BuildingPersistedData, ABS_Building> p_BuildingsForUpdate,
            ref List<ABS_Building.ABS_BuildingPersistedData> p_BuildingsForCreate,
            List<BuildingPersistedDataType> p_TargetList)
            where BuildingPersistedDataType : ABS_Building.ABS_BuildingPersistedData
        {
            foreach (BuildingPersistedDataType buildingData in p_TargetList)
            {
                ABS_Building b = null;
                if (p_Buildings.TryGetValue(buildingData.InstanceGuid, out b) && b != null)
                {
                    ABS_PersistencyLoadErrorCode res = b.CheckUpdatePossibility(buildingData, p_ElementList);
                    if (res != ABS_PersistencyLoadErrorCode.Successful)
                    {
                        REST_Logging.Warrning("ABS_BuildingParent", $"Checking the update possibility of the Building has been failed. Name: {b.name}");
                        return res;
                    }
                    p_BuildingsForUpdate[buildingData] = b;
                }
                else
                {
                    p_BuildingsForCreate.Add(buildingData);
                }
            }
            return ABS_PersistencyLoadErrorCode.Successful;
        }


        private ABS_PersistencyLoadErrorCode ConnectElement(List<ABS_BuildingElement.ABS_BuildingElementConnectionData> p_ElementConnections)
        {
            foreach (ABS_BuildingElement.ABS_BuildingElementConnectionData connectionData in p_ElementConnections)
            {
                ABS_Building targetBuilding = null;
                if (!m_Buildings.TryGetValue(connectionData.ConnectionTargetBuildingGuid, out targetBuilding)
                    || targetBuilding == null)
                {
                    return ABS_PersistencyLoadErrorCode.BuildingElementConnection_MissingBuilding;
                }

                ABS_BuildingElement targetElement = targetBuilding.FindBuildingElementBasedInstanceGuid(connectionData.ConnectionTargetElementGuid);
                if (targetElement == null)
                {
                    return ABS_PersistencyLoadErrorCode.BuildingElementConnection_MissingBuildingElement;
                }

                targetElement.ConnectElement(connectionData.ConnectionType, connectionData.ConnectedElement);
            }

            return ABS_PersistencyLoadErrorCode.Successful;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Persistency : Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}

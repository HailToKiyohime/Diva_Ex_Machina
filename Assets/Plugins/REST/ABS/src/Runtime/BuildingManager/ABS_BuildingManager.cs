//*********************************************************************
//  Dependencies: System
using System;

//  Dependencies: Unity
using UnityEngine;
using UnityEngine.SocialPlatforms;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_BuildingManagerMode
    {
        Unkown,
        SimpleBuild,
        DragBuild,
        SimpleDestroy,
        DragDestory,
        SimpleDestroyWithoutElement,
        DragDestoryWithoutElement
    }

    public enum ABS_BuildingManagerBuildMode
    {
        Single,
        Continues
    }

    public enum ABS_BuildingManagerActiveStatus
    {
        Inactive,
        Active,
        ActiveOnlyDestroy
    }

    public enum ABS_PositionSearchAlgorithm
    { 
        Free,
        BasicGrid,
        AdvancedGrid,
        SnapPointBased
    }

    public class ABS_BuildingManager : MonoBehaviour,
        ABS_IBuildingManagerExternalInterface,
        ABS_IBuildingManagerInternalInterface,
        ABS_IInputTracker
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_DragBuildingManager m_DragBuildingManager = null;
        private ABS_TemporaryBuildingElementManager m_TemporaryBuildingElementManager = null;
        private GameObject m_TemporaryBuildingElementManagerObject = null;
        private Transform m_TemporaryBuildingElementManagerTransform = null;

        private ABS_SimpleBuildingManager m_SimpleBuildingManager = null;

        private ABS_InputManager m_InputManager = null;
        private ABS_ActionHistory m_ActionHistory = null;

        private ABS_BuildingManagerMode m_Mode = ABS_BuildingManagerMode.SimpleBuild;
        private ABS_BuildingManagerBuildMode m_BuildMode = ABS_BuildingManagerBuildMode.Continues;

        private ABS_BuildingManagerActiveStatus m_ActiveStatus = ABS_BuildingManagerActiveStatus.Inactive;
        private bool m_BlockedInput = false;
        private bool m_Initialized = false;

        private ABS_SimpleBuildingProcessErrorCode m_BuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.Unkown;

        //----------------------------------------------------------------------------------------------------------------------
        //MetaData
        [SerializeField] private ABS_BuildingElementList m_ElementList = null;
        private ABS_BuildingElement m_ActiveBuildingElement = null;

        [SerializeField] private ABS_BuildingParent m_BuildingParent = null;

        [SerializeField] private GameObject[] m_TrackerObject = null;
        private ABS_TrackerInternalHelper m_Tracker = null;

        [SerializeField] private ABS_ObjectPoolBase m_ObjectPool = null;

        //----------------------------------------------------------------------------------------------------------------------
        //ABS_Building Behaviour Settings
        [SerializeField] private bool m_Sandbox = false;
        [SerializeField] private bool m_EnableCache = true;

        [SerializeField] private bool m_SearchPositionOnReset = false;

        [SerializeField] private bool m_EnableForcedFallback = false;
        private bool m_ForcedFallbackIsOn = false;
        [SerializeField] private bool m_ForcedFallbackResetOnElementPlace = true;

        //----------------------------------------------------------------------------------------------------------------------
        //Raycast
        [SerializeField] private ABS_RaycastMode m_RaycastMode = ABS_RaycastMode.Camera;
        [SerializeField] private ABS_LayerCollection m_LayerCollection;
        [SerializeField][Range(5f, 100f)] private float m_RaycastDistance = 10f;
        [SerializeField][Range(-50f, 50f)] private float m_RaycastOffset = 5f;
        [SerializeField] private Camera m_Camera = null;
        private ABS_Raycaster m_Raycaster = null;

        //----------------------------------------------------------------------------------------------------------------------
        //Search Base Properties
        private ABS_PositionerManager m_PositionerManager = null;
        private ABS_BuilderBaseSettings m_AlgorithmSettings = null;

        //----------------------------------------------------------------------------------------------------------------------
        //Input Settings
        [SerializeField] private ABS_InputType m_InputType = ABS_InputType.Key;
        [SerializeField] private ABS_RotationInputType m_RotationInputType = ABS_RotationInputType.MouseWheel;

        //Key Input settings
        [SerializeField] private KeyCode m_KeyForRotationRight = KeyCode.O;
        [SerializeField] private KeyCode m_KeyForRotationLeft = KeyCode.I;

        [SerializeField] private KeyCode m_KeyForBuild = KeyCode.Mouse0;
        [SerializeField] private KeyCode m_KeyForDestroy = KeyCode.Mouse0;
        [SerializeField] private KeyCode m_KeyForDragBuild = KeyCode.Mouse1;
        [SerializeField] private KeyCode m_KeyForDragDestroy = KeyCode.Mouse1;

        [SerializeField] private KeyCode m_KeyForModeChange = KeyCode.Q;
        [SerializeField] private KeyCode m_KeyForForcedFallback = KeyCode.F;

        [SerializeField] private KeyCode m_KeyForUndo = KeyCode.Z;
        [SerializeField] private KeyCode m_KeyForRedo = KeyCode.Y;

        [SerializeField] private KeyCode m_KeyForAlignRotationToGround = KeyCode.R;
        [SerializeField] private KeyCode m_KeyForAlignRotationToBuildingElements = KeyCode.R;

        private bool m_KeyBasedRotationIsOn = false;
        private bool m_KeyBasedRotationRight = false;
        private bool m_KeyBasedRotationLeft = false;
        private float m_KeyBasedRotationTimer = 0f;
        [SerializeField] private float m_KeyBasedRotationCycleTime = 0.2f;

        private bool m_AlignRotationToGround = false;
        private bool m_AlignRotationToGroundHold = false;
        private bool m_AlignRotationToBuidlingElements = false;

        //----------------------------------------------------------------------------------------------------------------------
        //Destroy
        private ABS_BuildingElementDestroyer m_Destroyer = null;
        [SerializeField] private DestroyType m_DestroyType = DestroyType.Instant;
        [SerializeField][Range(0.1f, 10f)] private float m_DestroyTimerDuration = 0.3f; //Seconds
        [SerializeField] private bool m_CutTimerOnLookAway = false;
        [SerializeField][Range(1u, 1000u)] private uint m_MaximumDestoryCount = 100u;

        //----------------------------------------------------------------------------------------------------------------------
        //Action History
        [SerializeField] private bool m_IsHistoryEnabled = false;
        [SerializeField] private uint m_HistoryActionCount = 50;
        [SerializeField] private bool m_PartialProcessing = false;
        [SerializeField] private bool m_ClearHistoryInCaseOfError = false;

#if UNITY_EDITOR
        //----------------------------------------------------------------------------------------------------------------------
        //Statistics
        [SerializeField] private bool m_StatisticsEnableCounter = false;
        [SerializeField] private bool m_StatisticsBuilderCounter = false;

        private REST_Stopwatch m_Stopwatch = new REST_Stopwatch ();
        private long m_StatisticsSummary = 0;
        private uint m_StatisticsFrameCounter = 0;

        ABS_IBuildingManagerStatisticsTracker.StatisticsDataCallbackDelegate m_StatisticsTrackerDataCallback;

        //----------------------------------------------------------------------------------------------------------------------
        //ProjectSettings
        private ABS_ProjectSettings m_ProjectSettings = null;
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Initalization  
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Awake()
        {
            if (!m_Initialized)
            {
                if (m_BuildingParent == null)
                {
                    REST_Logging.Warrning($"{this}", "The ABS_BuildingManager has a missing must has property : ABS_BuildingParent");
                    return;
                }

                Init();
                m_Initialized = true;
            }
        }

        private void Init()
        {
            //Tracker ----------------------------------------------------------------------------------------
            ABS_TrackerInternalHelper internalTracker = gameObject.AddComponent<ABS_TrackerInternalHelper>();
            internalTracker.Init(m_TrackerObject);
            m_Tracker = internalTracker;
            m_Tracker.SetExternalInterface(this);

            //Histroy ----------------------------------------------------------------------------------------
            m_ActionHistory = new ABS_ActionHistory(m_IsHistoryEnabled, 
                                                    m_HistoryActionCount,
                                                    m_PartialProcessing,
                                                    m_ClearHistoryInCaseOfError,
                                                    this, 
                                                    m_Tracker, 
                                                    m_ElementList);

            //TemporaryBuildingElementManager -----------------------------------------------------------
            m_TemporaryBuildingElementManagerObject = new GameObject("ABS_TemporaryBuildingElementManager");
            m_TemporaryBuildingElementManagerObject.transform.parent = this.transform;
            m_TemporaryBuildingElementManagerTransform = m_TemporaryBuildingElementManagerObject.transform;
            m_TemporaryBuildingElementManagerObject.layer = gameObject.layer;
            m_TemporaryBuildingElementManager = m_TemporaryBuildingElementManagerObject.AddComponent<ABS_TemporaryBuildingElementManager>();
            m_TemporaryBuildingElementManager.Init(this, m_Tracker, m_ObjectPool);

            //DragBuildingManager ----------------------------------------------------------------------
            GameObject tmpDragBuildingManager = new GameObject("ABS_DragBuildingManager");
            tmpDragBuildingManager.transform.parent = this.transform;
            tmpDragBuildingManager.layer = gameObject.layer;
            m_DragBuildingManager = tmpDragBuildingManager.AddComponent<ABS_DragBuildingManager>();
            m_DragBuildingManager.Init(this, m_Tracker, m_ActionHistory, m_BuildingParent, m_TemporaryBuildingElementManager);

            //PositionerManager ----------------------------------------------------------------------
            m_PositionerManager = new ABS_PositionerManager(this, m_Tracker);

            //SimpleBuildingManager -----------------------------------------------------------------------------------
            m_SimpleBuildingManager = new ABS_SimpleBuildingManager(this, 
                                                                    m_Tracker,
                                                                    m_PositionerManager,
                                                                    m_TemporaryBuildingElementManager);

            //Raycaster -----------------------------------------------------------------------------------
            m_Raycaster = new ABS_Raycaster(this, m_Tracker, m_Camera, m_RaycastMode);

            //InputManager -------------------------------------------------------------------------------
            m_InputManager = gameObject.AddComponent<ABS_InputManager>();
            m_InputManager.Init(this, m_Tracker);

            //Destroy ------------------------------------------------------------------------------------
            m_Destroyer = new ABS_BuildingElementDestroyer(this, m_Tracker, m_ActionHistory);
        }

        private void OnDisable()
        {
            Deactivate();
        }

        private void OnDestroy()
        {
            Deactivate();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Initalization  
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Activation / Mode / Status
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool ChangeMode(in ABS_BuildingManagerMode p_Mode)
        {
            if (m_Mode == ABS_BuildingManagerMode.SimpleBuild || m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                if(p_Mode != ABS_BuildingManagerMode.SimpleDestroy && p_Mode != ABS_BuildingManagerMode.SimpleDestroy)
                {
                    return false;
                }
            }
            else if (m_Mode == ABS_BuildingManagerMode.SimpleDestroy || m_Mode == ABS_BuildingManagerMode.DragDestory)
            {
                if (p_Mode != ABS_BuildingManagerMode.SimpleBuild && p_Mode != ABS_BuildingManagerMode.DragBuild)
                {
                    return false;
                }
            }
            else if (m_Mode == ABS_BuildingManagerMode.SimpleDestroyWithoutElement 
                || m_Mode == ABS_BuildingManagerMode.DragDestoryWithoutElement
                || p_Mode == ABS_BuildingManagerMode.SimpleDestroyWithoutElement
                || p_Mode == ABS_BuildingManagerMode.DragDestoryWithoutElement)
            {
                return false;
            }

            ChangeModePressed();
            return true;
        }

        public void ChangeMode()
        {
            ChangeModePressed();
        }

        private void ChangeModeImpl(ABS_BuildingManagerMode p_Mode)
        {
            if (m_Mode == p_Mode)
            {
                return;
            }

            m_Mode = p_Mode;
            m_Tracker.ModeChanged(m_Mode);
        }

        public ABS_BuildingManagerMode GetMode()
        {
            return m_Mode;
        }

        public bool ActivateAsDestroy()
        {
            if (!m_Initialized)
            {
                REST_Logging.Warrning($"{this}", "The Manager is not initalized successfully.");
                return false;
            }

            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.ActiveOnlyDestroy)
            {
            }
            else if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Active)
            {
                ChangeMode(ABS_BuildingManagerMode.SimpleDestroy);
            }
            else
            {
                ChangeModeImpl(ABS_BuildingManagerMode.SimpleDestroyWithoutElement);
                m_ActiveStatus = ABS_BuildingManagerActiveStatus.ActiveOnlyDestroy;
                m_Tracker.ActivatedOnlyDestroy();
            }
            return true;
        }

        public bool Activate(in uint p_Index, in ABS_BuildingManagerBuildMode p_BuildMode)
        {
            if (m_LayerCollection == null)
            {
                REST_Logging.Warrning($"{this}", "The LayerCollection is null");
                return false;
            }

            if (m_BuildingParent == null)
            {
                REST_Logging.Warrning($"{this}", "The BuildingParent is null");
                return false;
            }

            if (m_ElementList == null)
            {
                REST_Logging.Warrning($"{this}", "The BuildingElementList is null");
                return false;
            }

            if (m_ElementList.BuildingElements == null)
            {
                REST_Logging.Warrning($"{this}", "The BuildingElements of the List is null");
                return false;
            }

            if (m_ElementList.BuildingElements.Length == 0)
            {
                REST_Logging.Warrning($"{this}", "The BuildingElementList is empty");
                return false;
            }

            if (p_Index >= m_ElementList.BuildingElements.Length)
            {
                REST_Logging.Warrning($"{this}", "Too high index is given as argument");
                return false;
            }

            return Activate(m_ElementList.BuildingElements[p_Index], p_BuildMode);
        }

        public bool Activate(in ABS_BuildingElement p_BuildingElementPrefab, in ABS_BuildingManagerBuildMode p_BuildMode)
        {
            if (!m_Initialized)
            {
                REST_Logging.Warrning($"{this}", "The Manager is not initalized successfully.");
                return false;
            }

            if (p_BuildingElementPrefab == null)
            {
                REST_Logging.Warrning($"{this}", "The given ABS_BuildingElement is null");
                return false;
            }

            if (p_BuildingElementPrefab.PositionAlgorithmSettings == null)
            {
                REST_Logging.Warrning($"{this}", $"The given ABS_BuildingElement's ({p_BuildingElementPrefab.gameObject.name}) MetaData is null.");
                return false;
            }

            return ActivateImpl(p_BuildingElementPrefab, p_BuildMode);
        }

        private bool ActivateImpl(in ABS_BuildingElement p_BuildingElementPrefab, in ABS_BuildingManagerBuildMode p_BuildMode)
        {
            ResetManager();
            m_Destroyer.ResetDestroyer();

            m_BuildMode = p_BuildMode;

            m_ActiveBuildingElement = p_BuildingElementPrefab;

            m_AlgorithmSettings = m_ActiveBuildingElement.PositionAlgorithmSettings;
            m_TemporaryBuildingElementManager.Settings = m_AlgorithmSettings;

            //Reset element on components
            m_PositionerManager.ResetBuildingElement(m_ActiveBuildingElement);
            m_SimpleBuildingManager.ResetBuildingElement(m_ActiveBuildingElement);
            m_DragBuildingManager.ResetPendingBuildingElement(m_ActiveBuildingElement);

            m_TemporaryBuildingElementManagerObject.SetActive(true);

            if (m_SearchPositionOnReset)
            {
                m_Raycaster.PerformRaycast();
            }

            m_Tracker.ResetBuildingElement(m_ActiveBuildingElement);

            ChangeModeImpl(ABS_BuildingManagerMode.SimpleBuild);

            m_ActiveStatus = ABS_BuildingManagerActiveStatus.Active;
            m_Tracker.Activated(p_BuildingElementPrefab, p_BuildMode);
#if UNITY_EDITOR
            m_Stopwatch.Start();
#endif
            return true;
        }

        public void Deactivate()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive)
            {
                return;
            }

            m_ActiveStatus = ABS_BuildingManagerActiveStatus.Inactive;

            ResetManager();

            m_Destroyer.ResetDestroyer();
            m_ActiveBuildingElement = null;
            m_DragBuildingManager.ResetPendingBuildingElement(null);
            m_SimpleBuildingManager.Reset();
            m_TemporaryBuildingElementManagerObject.SetActive(false);
            m_InputManager.ClearData();
            m_PositionerManager.ResetManager();

            m_Tracker.Deactivated();
        }

        //Reset the Manager's internal state
        private void ResetManager()
        {
            if (m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                m_Tracker.DragBuildingStoped();
            }

            m_AlignRotationToGround = false;
            m_AlignRotationToBuidlingElements = false;
            m_ForcedFallbackIsOn = false;

            ClearCache();

#if UNITY_EDITOR
            if (m_StatisticsEnableCounter)
            {
                m_Stopwatch.Stop();
                m_StatisticsSummary = 0;
                m_StatisticsFrameCounter = 0;
            }
#endif
        }

        public ABS_BuildingManagerActiveStatus ActiveStatus()
        {
            return m_ActiveStatus;
        }
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Activation / Mode / Status
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Update
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Update()
        {
#if UNITY_EDITOR
            if (m_StatisticsTrackerDataCallback != null || m_StatisticsEnableCounter)
            {
                m_Stopwatch.TakeSnapshot();
            }
#endif
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive)
            {
                return;
            }

            m_Tracker.StartOfBuildingCycle();

            m_Raycaster.PerformRaycast();

            m_BuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.Unkown;

            if (m_Mode == ABS_BuildingManagerMode.SimpleBuild)
            {
                UpdateBuildLogic();

                m_Tracker.SimpleBuildResult(m_BuildingProcessErrorCode);
            }
            else if (m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                UpdateBuildLogic();
            }
            else if (m_Mode == ABS_BuildingManagerMode.SimpleDestroy 
                || m_Mode == ABS_BuildingManagerMode.SimpleDestroyWithoutElement
                || m_Mode == ABS_BuildingManagerMode.DragDestory
                || m_Mode == ABS_BuildingManagerMode.DragDestoryWithoutElement)
            {
                m_Destroyer.UpdateDestroyLogic();
            }
           
            m_Tracker.EndOfBuildingCycle();

#if UNITY_EDITOR
            if ((m_StatisticsTrackerDataCallback != null || m_StatisticsEnableCounter))
            {
                PrintStatisticsLogs();
            }
#endif
        }


#if UNITY_EDITOR
        private void PrintStatisticsLogs()
        {
            long nowNS = m_Stopwatch.GetElapsedNanoseconds();

            long difference = nowNS - m_Stopwatch.Snapshot;
            m_StatisticsSummary += difference;
            ++m_StatisticsFrameCounter;

            if (nowNS / 1000_000_000 >= 1)
            {
                ABS_BuildingManagerStatisticsData statisticsData = new ABS_BuildingManagerStatisticsData();

                statisticsData.StatisticsTimer = nowNS;
                statisticsData.StatisticsSummary = m_StatisticsSummary;
                statisticsData.StatisticsFrameCounter = m_StatisticsFrameCounter;

                ulong raycastCount = m_Raycaster.StatisticsRaycastCounter;
                raycastCount += m_DragBuildingManager.StatisticsCounterRaycast;
                raycastCount += m_PositionerManager.StatisticsCounterRaycast;
                statisticsData.StatisticsRaycastCounter = raycastCount;

                ulong overlapCheckCount = m_PositionerManager.StatisticsCounterRaycast;
                raycastCount += m_DragBuildingManager.StatisticsCounterOverlapCheck;
                statisticsData.StatisticsOverlapCheckCount = overlapCheckCount;

                statisticsData.StatisticsInstantiatedObject = m_TemporaryBuildingElementManager.StatisticsInstantiatedObject;
                statisticsData.StatisticsDestroyedObject = m_TemporaryBuildingElementManager.StatisticsDestroyedObject;

                if (m_StatisticsEnableCounter)
                {
                    statisticsData.Print();

                    if (m_StatisticsBuilderCounter)
                    {
                        m_PositionerManager.TriggerStatisticsPrint();
                    }
                }

                if (m_StatisticsTrackerDataCallback != null)
                {
                    m_StatisticsTrackerDataCallback(statisticsData);
                }

                m_Stopwatch.Restart();
                m_StatisticsSummary = 0;
                m_StatisticsFrameCounter = 0;
                m_Raycaster.StatisticsReset();
                m_PositionerManager.StatisticsReset();
                m_DragBuildingManager.StatisticsReset();
                m_TemporaryBuildingElementManager.StatisticsReset();
            }
        }
#endif

        private void UpdateBuildLogic()
        {
            //Handle visibilty cases
            if (m_Raycaster.HitTransform == null
                && m_AlgorithmSettings.ElementVisiblity == ABS_ElementVisibilityStrategy.VisibleOnHit)
            {
                //Hidden element
                m_TemporaryBuildingElementManagerObject.SetActive(false);
                m_BuildingProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.HiddenElement;
                return;
            }
            else
            {
                //Visible element
                m_TemporaryBuildingElementManagerObject.SetActive(true);
            }

            if (m_Mode == ABS_BuildingManagerMode.SimpleBuild)
            {
                ABS_SimpleBuildingProcessErrorCode buildProcessErrorCode = ABS_SimpleBuildingProcessErrorCode.Unkown;
                m_SimpleBuildingManager.SearchAndMovePosition(ref buildProcessErrorCode);
                m_DragBuildingManager.PositionSearchResult = m_SimpleBuildingManager.PositionSearchResult;

                ABS_SimpleBuildingProcessErrorCode tmpErrorCode = m_TemporaryBuildingElementManager.GetBlockErrorCode();
                if (tmpErrorCode != ABS_SimpleBuildingProcessErrorCode.Successful)
                {
                    m_BuildingProcessErrorCode = tmpErrorCode;
                }
                else
                {
                    m_BuildingProcessErrorCode = buildProcessErrorCode;
                }
            }
            else if (m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                m_DragBuildingManager.Build();
            }
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Update
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive)
            {
                return;
            }

            if (m_ProjectSettings == null)
            {
                m_ProjectSettings = ABS_ProjectSettingsGetter.GetSettings();
            }

            if ((m_Mode == ABS_BuildingManagerMode.SimpleBuild || m_Mode == ABS_BuildingManagerMode.DragBuild) 
                 && m_SimpleBuildingManager.PositionSearchResult != null)
            {
                m_PositionerManager.OnDrawGizmos(m_ProjectSettings, m_SimpleBuildingManager.PositionSearchResult);
            }

            m_TemporaryBuildingElementManager.DrawGizmos(m_ProjectSettings);

            if (m_ProjectSettings.Raycast_Hitpoint && m_Raycaster != null && m_Raycaster.HitTransform != null)
            {
                REST_GizmosUtils.DrawSphere(m_Raycaster.HitPoint, 0.1f, m_ProjectSettings.Raycast_HitpointColor);
            }

            if (m_ProjectSettings.Raycast_Line)
            {
                if (m_RaycastMode == ABS_RaycastMode.Camera)
                {
                    Vector3 forward = m_Camera.transform.TransformDirection(Vector3.forward) * m_RaycastDistance;
                    Vector3 startingPosition = m_Camera.transform.position;
                    startingPosition += forward.normalized * m_RaycastOffset;
                    UnityEngine.Debug.DrawRay(startingPosition, forward, m_ProjectSettings.Raycast_LineColor);
                }
                else
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Gizmos.color = m_ProjectSettings.Raycast_LineColor;
                    Vector3 startPoint = ray.origin + ray.direction * m_RaycastOffset;
                    Gizmos.DrawLine(startPoint, startPoint + ray.direction * m_RaycastDistance);
                }
            }

            if (m_SimpleBuildingManager.PositionSearchResult != null)
            {
                REST_GizmosUtils.DrawWireCube(m_ActiveBuildingElement.Dimension, 
                    m_SimpleBuildingManager.PositionSearchResult.WorldPosition, 
                    m_SimpleBuildingManager.PositionSearchResult.Rotation.eulerAngles, 
                    UnityEngine.Color.white);
            }
        }
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Input Logic logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_InputType InputType { get { return m_InputType; } }
        public ABS_RotationInputType RotationInputType { get { return m_RotationInputType; } }

        public KeyCode KeyForRotationRight { get { return m_KeyForRotationRight; } }
        public KeyCode KeyForRotationLeft { get { return m_KeyForRotationLeft; } }
        public KeyCode KeyForBuild { get { return m_KeyForBuild; } }
        public KeyCode KeyForDestroy { get { return m_KeyForDestroy; } }
        public KeyCode KeyForDragBuild { get { return m_KeyForDragBuild; } }
        public KeyCode KeyForDragDestroy { get { return m_KeyForDragDestroy; } }
        public KeyCode KeyForUndo { get { return m_KeyForUndo; } }
        public KeyCode KeyForRedo { get { return m_KeyForRedo; } }
        public KeyCode KeyForModeChange { get { return m_KeyForModeChange; } }
        public KeyCode KeyForForcedFallback { get { return m_KeyForForcedFallback; } }
        public KeyCode KeyForAlignRotationToGround { get { return m_KeyForAlignRotationToGround; } }
        public KeyCode KeyForAlignRotationToBuildingElements { get { return m_KeyForAlignRotationToBuildingElements; } }

        public ABS_IBuildingManagerInputActionInterface GetInputInterface()
        {
            return m_InputManager;
        }

        public void BlockInput(in bool p_BlockState)
        {
            m_BlockedInput = p_BlockState;
        }

        public void ChangeModePressed()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive 
                || !m_Initialized 
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleDestroyWithoutElement
                || m_Mode == ABS_BuildingManagerMode.DragDestoryWithoutElement)
            {
                return;
            }

            switch (m_Mode)
            {
                case ABS_BuildingManagerMode.SimpleBuild:
                case ABS_BuildingManagerMode.DragBuild:
                    {
                        m_DragBuildingManager.ResetPendingBuildingElement(null);
                        m_SimpleBuildingManager.Reset();
                        m_TemporaryBuildingElementManagerObject.SetActive(false);
                        ResetManager();
                        m_Destroyer.ResetDestroyer();
                        ChangeModeImpl(ABS_BuildingManagerMode.SimpleDestroy);
                    }
                    break;
                case ABS_BuildingManagerMode.SimpleDestroy:
                case ABS_BuildingManagerMode.DragDestory:
                    {
                        m_DragBuildingManager.ResetPendingBuildingElement(m_ActiveBuildingElement);
                        m_SimpleBuildingManager.ResetBuildingElement(m_ActiveBuildingElement);
                        m_TemporaryBuildingElementManagerObject.SetActive(true);
                        m_Destroyer.ResetDestroyer();
                        ChangeModeImpl(ABS_BuildingManagerMode.SimpleBuild);
                    }
                    break;
                default:
                    REST_Logging.Error($"{this}", "Unkown Mode as ABS_BuildingManager Mode");
                    break;
            }
        }

        public void ForcedFallbackPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleDestroy
                || m_Mode == ABS_BuildingManagerMode.DragDestory)
            {
                return;
            }

            //The forcefallback feature can not be used for not foundation elements if the settings using foundation logic
            //Because the Foundation and a ForcedFallback features doing the oposit things in case of not foundation elements
            if (m_ActiveBuildingElement.PositionAlgorithmSettings.UseFoundationLogic
                && !m_ActiveBuildingElement.Foundation)
            {
                return;
            }

            m_ForcedFallbackIsOn = !m_ForcedFallbackIsOn;
        }

        public void SimpleBuildingPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            if (m_SimpleBuildingManager.RepositionState == ABS_RepositionState.Moving)
            {
                if (m_AlgorithmSettings.AllowPlacementDuringMovement)
                {
                    //Move the elements to the target position immedietly
                    m_TemporaryBuildingElementManagerTransform.position = m_SimpleBuildingManager.PositionSearchResult.WorldPosition;
                    m_TemporaryBuildingElementManagerTransform.rotation = m_SimpleBuildingManager.PositionSearchResult.Rotation;
                }
                else
                {
                    return;
                }
            }

            //PlaceItem
            if (m_TemporaryBuildingElementManagerObject.activeSelf && m_TemporaryBuildingElementManager.Avaliable())
            {
                m_DragBuildingManager.Place();
                if (m_ForcedFallbackResetOnElementPlace)
                {
                    m_ForcedFallbackIsOn = false;
                }
                if (m_BuildMode == ABS_BuildingManagerBuildMode.Single)
                {
                    Deactivate();
                }
                else
                {
                    ClearCache();
                }
            }
        }

        public void DragBuildingPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleDestroy
                || m_Mode == ABS_BuildingManagerMode.DragDestory)
            {
                return;
            }

            //If the drag building is already inprogress
            if (m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            //Check if the DragBuidling Is disabled by settings
            if (!m_AlgorithmSettings.EnableDragBuilding
               || !m_ActiveBuildingElement.DragBuildingEnabled
               || !(m_ActiveBuildingElement.EnabledDragBuildingX || m_ActiveBuildingElement.EnabledDragBuildingZ))
            {
                return;
            }

            //Check if the DragBuidling is not available without valid position
            if (m_Raycaster.HitTransform == null
                && m_SimpleBuildingManager.PositionSearchResult.TargetBuilding != null
                && !m_AlgorithmSettings.AllowDragBuildingWithoutHit
                && m_AlgorithmSettings.AllowPositionSearchAtRaycastEndPosition)
            {
                return;
            }

            //Check if the DragBuildingIs not available based on an other options
            if (!m_TemporaryBuildingElementManager.Avaliable())
            {
                return;
            }

            if (m_SimpleBuildingManager.RepositionState == ABS_RepositionState.Moving)
            {
                if (m_AlgorithmSettings.AllowPlacementDuringMovement)
                {
                    //Move the elements to the target position immedietly
                    m_TemporaryBuildingElementManagerTransform.position = m_SimpleBuildingManager.PositionSearchResult.WorldPosition;
                    m_TemporaryBuildingElementManagerTransform.rotation = m_SimpleBuildingManager.PositionSearchResult.Rotation;
                }
                else
                {
                    return;
                }
            }

            ChangeModeImpl(ABS_BuildingManagerMode.DragBuild);
            m_Tracker.DragBuildingStarted();
        }

        public void DragBuildingReleased()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            //PlaceItem
            if (m_TemporaryBuildingElementManagerObject.activeSelf && m_TemporaryBuildingElementManager.Avaliable())
            {
                m_DragBuildingManager.Place();
                m_Tracker.DragBuildingStoped();
                m_TemporaryBuildingElementManagerTransform.position = GetRaycastHitOrEndPosition();
                ChangeModeImpl(ABS_BuildingManagerMode.SimpleBuild);

                if (m_BuildMode == ABS_BuildingManagerBuildMode.Single)
                {
                    Deactivate();
                }
                else
                {
                    ClearCache();
                }
            }
            else
            {
                m_TemporaryBuildingElementManager.ResetTemoraryElementsList(true, false);
                m_Tracker.DragBuildingStoped();
                m_TemporaryBuildingElementManagerTransform.position = GetRaycastHitOrEndPosition();
                ChangeModeImpl(ABS_BuildingManagerMode.SimpleBuild);
            }
        }

        public void MouseWheelChanged(in float p_Value)
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            m_PositionerManager.MouseWheelChanged(p_Value);
        }

        public void RotationButtonRightPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }
            m_KeyBasedRotationIsOn = true;
            m_KeyBasedRotationRight = true;
            m_KeyBasedRotationTimer = 0f;
            if (!m_KeyBasedRotationLeft)
            {
                m_PositionerManager.MouseWheelChanged(-0.1f);
            }
        }

        public void RotationButtonRightHold()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            if (m_KeyBasedRotationIsOn 
                && m_KeyBasedRotationRight
                && !m_KeyBasedRotationLeft)
            {
                m_KeyBasedRotationTimer += Time.deltaTime;
                if (m_KeyBasedRotationTimer > m_KeyBasedRotationCycleTime)
                {
                    m_KeyBasedRotationTimer -= m_KeyBasedRotationCycleTime;
                    m_PositionerManager.MouseWheelChanged(-0.1f);
                }
            }
        }

        public void RotationButtonRightReleased()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            m_KeyBasedRotationRight = false;
            if (!m_KeyBasedRotationLeft)
            {
                m_KeyBasedRotationIsOn = false;
            }
            else
            {
                m_KeyBasedRotationTimer = 0f;
                m_PositionerManager.MouseWheelChanged(0.1f);
            }
        }

        public void RotationButtonLeftPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            m_KeyBasedRotationIsOn = true;
            m_KeyBasedRotationLeft = true;
            if (!m_KeyBasedRotationRight)
            {
                m_PositionerManager.MouseWheelChanged(0.1f);
            }
        }

        public void RotationButtonLeftHold()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            if (m_KeyBasedRotationIsOn
                && !m_KeyBasedRotationRight
                && m_KeyBasedRotationLeft)
            {
                m_KeyBasedRotationTimer += Time.deltaTime;
                if (m_KeyBasedRotationTimer > m_KeyBasedRotationCycleTime)
                {
                    m_KeyBasedRotationTimer -= m_KeyBasedRotationCycleTime;
                    m_PositionerManager.MouseWheelChanged(0.1f);
                }
            }
        }

        public void RotationButtonLeftReleased()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }
            m_KeyBasedRotationLeft = false;
            if (!m_KeyBasedRotationRight)
            {
                m_KeyBasedRotationIsOn = false;
            }
            else
            {
                m_KeyBasedRotationTimer = 0f;
                m_PositionerManager.MouseWheelChanged(-0.1f);
            }
        }

        public void AlignRotationToGroundPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild
                || !m_AlgorithmSettings.IsAlignRotationToGroundStrategySupported())
            {
                return;
            }

            m_AlignRotationToGround = !m_AlignRotationToGround;
            m_AlignRotationToGroundHold = true;
        }
        public void AlignRotationToGroundReleased()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild
                || !m_AlgorithmSettings.IsAlignRotationToGroundStrategySupported())
            {
                return;
            }

            m_AlignRotationToGroundHold = false;
        }

        public void AlignRotationToBuildingElementsPressed()
        {
            if (m_ActiveStatus != ABS_BuildingManagerActiveStatus.Active
                || !m_Initialized
                || m_BlockedInput
                || m_Mode != ABS_BuildingManagerMode.SimpleBuild)
            {
                return;
            }

            m_AlignRotationToBuidlingElements = true;
        }

        public void UndoPressed()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput)
            {
                return;
            }

            ABS_ActionHistoryErrorCodes res = m_ActionHistory.Undo();
            m_Tracker.ActionHistroryUndoResult(res);
        }

        public void RedoPressed()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput)
            {
                return;
            }

            ABS_ActionHistoryErrorCodes res = m_ActionHistory.Redo();
            m_Tracker.ActionHistroryRedoResult(res);
        }

        public void SimpleDestroyPressed()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleBuild
                || m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            m_Destroyer.DestroyKeyIsPressed();
        }

        public void SimpleDestroyHold()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleBuild
                || m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            m_Destroyer.DestroyKeyIsHeld();
        }

        public void SimpleDestroyReleased()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleBuild
                || m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            m_Destroyer.DestroyKeyIsReleased();
        }

        public void DragDestroyPressed()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleBuild
                || m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            m_Destroyer.DragKeyIsPressed();
        }

        public void DragDestroyReleased()
        {
            if (m_ActiveStatus == ABS_BuildingManagerActiveStatus.Inactive
                || !m_Initialized
                || m_BlockedInput
                || m_Mode == ABS_BuildingManagerMode.SimpleBuild
                || m_Mode == ABS_BuildingManagerMode.DragBuild)
            {
                return;
            }

            m_Destroyer.DragKeyIsReleased();
        }
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Input Logic logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void ClearCache()
        {
            m_PositionerManager.ClearCache();
        }

        public void ClearHistory()
        {
            m_ActionHistory.Clear();
        }

        public void AddTracker(in ABS_BuildingManagerTracker p_Tracker)
        {
            m_Tracker.AddTracker(p_Tracker);
        }

        public void RemoveTracker(in ABS_BuildingManagerTracker p_Tracker)
        {
            m_Tracker.RemoveTracker(p_Tracker);
        }

        public void SetMaximumBuildingElementCount(in uint p_MaxElementCount)
        {
            m_DragBuildingManager.RefreshMaxBuildingElementCount(p_MaxElementCount);
        }

        public ABS_BuildingParent BuildingParent
        {
            get { return m_BuildingParent; }
            set { m_BuildingParent = value; }
        }

        public bool ResetCamera(in Camera p_Camera)
        {
            if (m_RaycastMode == ABS_RaycastMode.Camera)
            {
                m_Camera = p_Camera;
                m_Raycaster.Camera = p_Camera;
                return true;
            }
            return false;
        }

        public ABS_BuildingElement GetActiveBuildingElement()
        {
            return m_ActiveBuildingElement;
        }

        public void EnableSandbox()
        {
            m_Sandbox = true;
        }

        public void DisableSandbox()
        {
            m_Sandbox = false;
        }

        public void SetBuildingElementList(in ABS_BuildingElementList p_ElementList)
        {
            m_ElementList = p_ElementList;
        }

        public ABS_BuildingElementList ElementList => m_ElementList;

        public void SetBuildingElementList(in ABS_BuildingElementList p_ElementList, uint p_NewIndex)
        {
            m_ElementList = p_ElementList;
            Activate(p_NewIndex, m_BuildMode);
        }

        //----------------------------------------------------------------------------------------------------------------------
        // MetaData
        public bool IsSandbox { get { return m_Sandbox; } }
        public bool IsCachingEnabled { get { return m_EnableCache; } }
        public ABS_BuildingManagerBuildMode BuildMode { get { return m_BuildMode; } }


        //----------------------------------------------------------------------------------------------------------------------
        //Raycast
        public ABS_Raycaster Raycaster { get { return m_Raycaster; } }

        public ABS_LayerCollection LayerCollection { get { return m_LayerCollection; } }
        public float RaycastDistance
        {
            get { return m_RaycastDistance; }
        }
        public float RaycastOffset
        {
            get { return m_RaycastOffset; }
        }
        public Transform HitTransform { get { return m_Raycaster.HitTransform; } }
        public Camera Camera { get { return m_Camera; } }

        public Vector3 GetRaycastHitOrEndPosition()
        {
            if (m_RaycastMode == ABS_RaycastMode.Camera)
            {
                return m_Raycaster.GetRaycastHitOrEndPosition(m_RaycastDistance, m_RaycastOffset);
            }
            else
            {
                if (m_Raycaster.HitTransform == null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Vector3 startPoint = ray.origin + ray.direction * m_RaycastOffset;
                    return startPoint + ray.direction * m_RaycastDistance;
                }
                else
                {
                    return m_Raycaster.HitPoint;
                }
            }
        }

        public Vector3 GetRaycastEndPosition()
        {
            if (m_RaycastMode == ABS_RaycastMode.Camera)
            {
                return Raycaster.CalcualteRaycastEndPosition(m_RaycastDistance, m_RaycastOffset);
            }
            else
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 startPoint = ray.origin + ray.direction * m_RaycastOffset;
                return startPoint + ray.direction * m_RaycastDistance;
            }
        }


        //----------------------------------------------------------------------------------------------------------------------
        //Temporay
        public Vector3 GetTMPLocalPositionFromGlobal(in Vector3 p_GlobalPosition)
        {
            return m_TemporaryBuildingElementManagerTransform.InverseTransformPoint(p_GlobalPosition);
        }

        public Vector3 GetGlobalPositionFromTMPLocal(in Vector3 p_LocalPosition)
        {
            return m_TemporaryBuildingElementManagerTransform.TransformPoint(p_LocalPosition);
        }

        //----------------------------------------------------------------------------------------------------------------------
        //Destroy
        public DestroyType DestroyType { get { return m_DestroyType; } }
        public float DestroyTimerDuration { get { return m_DestroyTimerDuration; } }
        public bool CutTimerOnLookAway { get { return m_CutTimerOnLookAway; } }
        public uint MaximumDestoryCount { get { return m_MaximumDestoryCount; } }

        public ABS_PositionValidationData ValidatePosition(in Vector3 p_Position, in UnityEngine.Quaternion p_Rotation)
        {
            return m_SimpleBuildingManager.ValidatePosition(p_Position, p_Rotation);
        }

        public bool IsRotationAlignmentToGroundOn()
        {
            return m_AlignRotationToGround;
        }
        public bool IsRotationAlignmentToGroundHold()
        {
            return m_AlignRotationToGroundHold;
        }

        public bool IsAlignRotationToBuidlingElementsTriggered()
        {
            bool res = m_AlignRotationToBuidlingElements;
            m_AlignRotationToBuidlingElements = false;
            return res;
        }

        public bool IsForcedFallbackIsOn()
        {
            return m_EnableForcedFallback && m_ForcedFallbackIsOn;
        }

        public ABS_BasicGridBuilding GlobalBasicGridParent
        {
            get { return m_BuildingParent.GetBasicGridParent(); }
        }

        public ABS_FreeBuilding GlobalFreeParent
        {
            get { return m_BuildingParent.GetFreeBuildingParent(); }
        }

        public Vector3 GetParentPositionAlignment(ABS_BuildingElement p_TargetElement)
        {
            return m_PositionerManager.GetParentPositionAlignment(p_TargetElement);
        }

        public ABS_Building GetParentForNewBuildingElement()
        {
            if (m_SimpleBuildingManager.PositionSearchResult != null && m_SimpleBuildingManager.PositionSearchResult.TargetBuilding != null)
            {
                return m_SimpleBuildingManager.PositionSearchResult.TargetBuilding;
            }
            else
            {
                return m_BuildingParent.GetParentForNewBuildingElement(
                    m_ActiveBuildingElement, 
                    m_TemporaryBuildingElementManagerTransform.position,
                    m_TemporaryBuildingElementManagerTransform.eulerAngles,
                    this,
                    m_Tracker);
            }
        }
        public ABS_BuildingParent GetBuildingParent()
        {
            return m_BuildingParent;
        }
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public string ToJSON(in bool p_PrettyPrint)
        {
            if (!m_Initialized || m_BuildingParent == null)
            {
                return null;
            }

            return m_BuildingParent.ToJSON(p_PrettyPrint);
        }

        public ABS_PersistedData GetPersistedData()
        {
            if (!m_Initialized || m_BuildingParent == null)
            {
                return null;
            }

            return m_BuildingParent.GetPersistedData();
        }

        public ABS_PersistencyLoadErrorCode UpdateFromPersistedJSON(in string p_PersistedDataJSON)
        {
            if (m_BuildingParent == null)
            {
                return ABS_PersistencyLoadErrorCode.Unkown;
            }

            return ABS_BuildingParent.UpdateFromPersistedJSON(p_PersistedDataJSON, m_BuildingParent, m_ElementList);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region ABS_IBuildingManagerStatisticsTracker
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        public void RegistrateStatTracker(in ABS_IBuildingManagerStatisticsTracker p_StatTracker)
        {
            m_StatisticsTrackerDataCallback += p_StatTracker.StatisticsDataCallback;
        }

        public void UnRegistrateStatTracker(in ABS_IBuildingManagerStatisticsTracker p_StatTracker)
        {
            m_StatisticsTrackerDataCallback -= p_StatTracker.StatisticsDataCallback;
        }
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // ABS_IBuildingManagerStatisticsTracker
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}
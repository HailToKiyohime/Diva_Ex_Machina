//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_BuildingElementState
    {
        NORMAL,
        PENDING,
        BLOCKED,
        SIGNEDFORDELETE,
        PREBUILT,
    }

    public enum ABS_HighlightStrategy
    {
        AutomaticCollection,
        Custom,
        None
    }

    public enum ABS_DragBuildingBehaviour
    {
        FilledUp,
        Frame,
        HalfFrame,
        OneLine
    }

    public enum ABS_BuildingElementDimensionType
    {
        Fixed,
        ColliderBased
    }

    public enum ABS_BuildingElementConnectionType
    {
        Unkown,
        Attachment,
        Structure
    }

    public class ABS_BuildingElement : ABS_SaveableMonobehaviour, ABS_IBuildingElementExternalInterface
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //MetaData
        [SerializeField] private ABS_BuildingElement m_FinalElement = null;

        //Properties Basics
        [SerializeField] private bool m_PreBuilt = false;
        [SerializeField] private bool m_Foundation = false;
        [SerializeField] private ABS_BuildingElementAreaType m_AreaType = ABS_BuildingElementAreaType.Unkown;
        [SerializeField] private bool m_Indestructible = false;
        [SerializeField] private bool m_CanNotBeAttachTarget = false;

        //Properties Behaviour
        [SerializeField] private bool m_ShouldOverride = false;
        [SerializeField] private bool m_ShouldAllowedByArea = false;
        [SerializeField] private bool m_ShouldSnapToFoundation = false;
        [SerializeField] private bool m_SnapToPreBuiltFinalElement = false;

        //Properties DragBuilding
        [SerializeField] private bool m_DragBuildingEnabled = true;
        [SerializeField] private ABS_DragBuildingBehaviour m_DragBuildingBehaviour = ABS_DragBuildingBehaviour.FilledUp;
        [SerializeField] private bool m_EnabledDragBuildingX = true;
        [SerializeField] private bool m_EnabledDragBuildingZ = true;

        //Build Colider
        [SerializeField] private ABS_BuildingElementDimensionType m_BuildingElementDimensionType = ABS_BuildingElementDimensionType.Fixed;
        [SerializeField] private Vector3 m_Dimension = Vector3.one;
        [SerializeField] private BoxCollider m_DimensionCollider = null;

        //Highlight
        [SerializeField] private ABS_BuildingElementHighlightCollection m_HighlightCollection = null;
        [SerializeField] private ABS_HighlightStrategy m_HighlightStrategy = ABS_HighlightStrategy.AutomaticCollection;
        [SerializeField] private List<Renderer> m_Renderers = new List<Renderer>();
        private List<Material[]> m_DefaultMaterialLists = new List<Material[]>();
        private List<Material[][]> m_OutlineMaterialLists = new List<Material[][]>();
        private bool m_DefaultMaterialCollected = false;

        private List<MeshRenderer> m_MeshRenderers = new List<MeshRenderer>();
        private bool m_MeshRenderersCollected = false;

        //Algorithm properties
        [SerializeField] private ABS_PositionSearchAlgorithm m_PositionSearchAlgorithm = ABS_PositionSearchAlgorithm.Free;
        [SerializeField] private ABS_BuilderBaseSettings m_PositionAlgorithmSettings = null;

        //AdvacnedGridBuilding
        [SerializeField] private ABS_AdvancedGridSnapPointRuleSet m_SnapPointRuleSet = null;
        [SerializeField] private ABS_AdvancedGridType m_AdvancedGridType = ABS_AdvancedGridType.Floor;
        [SerializeField] private ABS_AdvancedGridAxisType m_AdvancedGridAxisType = ABS_AdvancedGridAxisType.Both;
        [SerializeField] private bool m_AllowMixedAxisDragBuilding = false;
        [SerializeField] private bool m_StableElement = false;
        private short m_StabilityLevel = -1;
        private bool m_Stable = false;

        //SnapPointBased
        [SerializeField] private Mesh m_Mesh = null;
        [SerializeField] private ABS_BuildingElementSnapPointType m_SnapPointType = ABS_BuildingElementSnapPointType.Unkown;

        //FreeBuilding
        [SerializeField] private bool m_ShouldAttached = false;

        //Link elements
        private Dictionary<ABS_BuildingElement, ABS_BuildingElementConnectionType> m_ConnectionTargetElements = null;
        private List<ABS_BuildingElement> m_ConnectedElements = null;

        //Others
        private ABS_Building m_ParentBuilding = null;
        private ABS_BuildingElementState m_State = ABS_BuildingElementState.NORMAL;

#if ABS_ENABLE_NAVMESH
        private UnityEngine.AI.NavMeshObstacle m_NavMeshObstacle = null;
#endif
        private List<Collider> m_Colliders = new List<Collider>();
        private bool m_ColliderCollected = false;

#if UNITY_EDITOR
        private ABS_ProjectSettings m_ProjectSettings = null;

        public static bool s_IsApplicationQuitting = false;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElement() : base()
        {
            m_ConnectionTargetElements = new Dictionary<ABS_BuildingElement, ABS_BuildingElementConnectionType>();
            m_ConnectedElements = new List<ABS_BuildingElement>();

            m_ISaveableType = ABS_SaveableMonobehaviour.DataType.BuildingElement;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  MonoBehaviour implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void AwakeImpl()
        {
            CollectColliders();
            SaveDefaultMaterial();

            if (m_PreBuilt)
            {
                MakePrebuiltState();
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // MonoBehaviour implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  getters / setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElement FinalElement
        {
            get { return m_FinalElement; }
            set { m_FinalElement = value; }
        }

        public bool PreBuilt
        {
            get { return m_PreBuilt; }
            set
            {
                if (m_PreBuilt == value)
                {
                    return;
                }
                m_PreBuilt = value;
                MakePrebuiltState();
            }
        }

        public bool Foundation
        {
            get { return m_Foundation; }
            set { m_Foundation = value; }
        }

        public ABS_BuildingElementAreaType AreaType
        {
            get { return m_AreaType; }
            set { m_AreaType = value; }
        }

        public bool Indestructible
        {
            get { return m_Indestructible; }
            set { m_Indestructible = value; }
        }
        
        public bool CanNotBeAttachTarget
        {
            get { return m_CanNotBeAttachTarget; }
            set { m_CanNotBeAttachTarget = value; }
        }

        public bool ShouldOverride
        {
            get { return m_ShouldOverride; }
            set { m_ShouldOverride = value; }
        }

        public bool ShouldAllowedByArea
        {
            get { return m_ShouldAllowedByArea; }
            set { m_ShouldAllowedByArea = value; }
        }

        public bool SnapToPreBuiltFinalElement
        {
            get { return m_SnapToPreBuiltFinalElement; }
            set { m_SnapToPreBuiltFinalElement = value; }
        }

        public bool ShouldSnapToFoundation
        {
            get { return m_ShouldSnapToFoundation; }
            set { m_ShouldSnapToFoundation = value; }
        }

        public bool DragBuildingEnabled
        {
            get { return m_DragBuildingEnabled; }
            set { m_DragBuildingEnabled = value; }
        }

        public ABS_DragBuildingBehaviour DragBuildingBehaviour
        {
            get { return m_DragBuildingBehaviour; }
            set { m_DragBuildingBehaviour = value; }
        }

        public bool EnabledDragBuildingX
        {
            get { return m_EnabledDragBuildingX; }
            set { m_EnabledDragBuildingX = value; }
        }

        public bool EnabledDragBuildingZ
        {
            get { return m_EnabledDragBuildingZ; }
            set { m_EnabledDragBuildingZ = value; }
        }

        public ABS_BuildingElementDimensionType BuildingElementDimensionType
        {
            get { return m_BuildingElementDimensionType; }
        }

        public BoxCollider DimensionCollider
        {
            get { return m_DimensionCollider; }
            set { m_DimensionCollider = value; }
        }

        public ABS_BuildingElementHighlightCollection HighlightCollection
        {
            get { return m_HighlightCollection; }
            set { m_HighlightCollection = value; }
        }

        public ABS_HighlightStrategy HighlightStrategy
        {
            get { return m_HighlightStrategy; }
        }

        public List<Renderer> Renderers
        {
            get { return m_Renderers; }
            set { m_Renderers = value; }
        }

        public ABS_PositionSearchAlgorithm PositionSearchAlgorithm
        {
            get { return m_PositionSearchAlgorithm; }
            set { m_PositionSearchAlgorithm = value; }
        }

        public ABS_BuilderBaseSettings PositionAlgorithmSettings
        {
            get { return m_PositionAlgorithmSettings; }
            set { m_PositionAlgorithmSettings = value; }
        }

        public ABS_AdvancedGridSnapPointRuleSet SnapPointRuleSet
        {
            get { return m_SnapPointRuleSet; }
            set { m_SnapPointRuleSet = value; }
        }

        public ABS_AdvancedGridType AdvancedGridType
        {
            get { return m_AdvancedGridType; }
            set { m_AdvancedGridType = value; }
        }

        public ABS_AdvancedGridAxisType AdvancedGridAxisType
        {
            get { return m_AdvancedGridAxisType; }
            set { m_AdvancedGridAxisType = value; }
        }

        public bool AllowMixedAxisDragBuilding
        {
            get { return m_AllowMixedAxisDragBuilding; }
            set { m_AllowMixedAxisDragBuilding = value; }
        }
        
        public bool StableElement
        {
            get { return m_StableElement; }
            set { m_StableElement = value; }
        }
        
        public short StabilityLevel
        {
            get { return m_StabilityLevel; }
            set { m_StabilityLevel = value; }
        }
        
        public bool Stable
        {
            get { return m_Stable; }
            set { m_Stable = value; }
        }

        public Mesh Mesh
        {
            get { return m_Mesh; }
            set { m_Mesh = value; }
        }

        public ABS_BuildingElementSnapPointType SnapPointType
        {
            get { return m_SnapPointType; }
            set { m_SnapPointType = value; }
        }

        public List<Collider> Collider
        {
            get
            {
                CollectColliders();
                return m_Colliders;
            }
        }

        public Vector3 Dimension
        {
            get
            {
                if (m_BuildingElementDimensionType == ABS_BuildingElementDimensionType.ColliderBased)
                {
                    if (m_DimensionCollider == null)
                    {
                        REST_Logging.Warrning($"{this}",
                            $"The BuildingElementDimensionType is set to ColliderBased but the DimensionCollider is null for element: {this.name}");
                    }
                    else
                    {
                        return m_DimensionCollider.size;
                    }
                }
                return m_Dimension; 
            }
            set { m_Dimension = value; }
        }

        public Vector3 VerticalShifting
        {
            get { return Dimension.y / 2f * Vector3.up; }
        }

        public Vector3 Shifting
        {
            get { return Dimension / 2f; }
        }

        public ABS_Building ParentBuilding
        {
            get { return m_ParentBuilding; }
            set { m_ParentBuilding = value; }
        }

        public ABS_LayerCollection LayerCollection { get { return m_PositionAlgorithmSettings.LayerCollection; } }

        public ABS_BuildingElementState State
        {
            get { return m_State; }
            set
            {
                if (m_State == value)
                {
                    return;
                }

                SaveDefaultMaterial();

                if (m_State == ABS_BuildingElementState.PREBUILT && value != ABS_BuildingElementState.PREBUILT)
                {
                    EnablePlayerCollision();
                }

                m_State = value;
                if (PreBuilt && m_State == ABS_BuildingElementState.NORMAL)
                {
                    m_State = ABS_BuildingElementState.PREBUILT;
                }

                SetMaterialBasedOnStateImpl();
            }
        }

        public Dictionary<ABS_BuildingElement, ABS_BuildingElementConnectionType> ConnectionTargetElements
        {
            get { return m_ConnectionTargetElements; }
            set { m_ConnectionTargetElements = value; }
        }

        public List<ABS_BuildingElement> ConnectedElements
        {
            get { return m_ConnectedElements; }
        }

        public bool ShouldAttached
        {
            get { return m_ShouldAttached; }
            set { m_ShouldAttached = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Basic getters / setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //#region  public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool IsStable ()
        {
            return m_StableElement 
                || m_Stable
                || (m_PositionAlgorithmSettings.UseFoundationLogic && m_Foundation);
        }

        public void EnableMeshRenderers(bool p_Enable)
        {
            CollectMeshRenderers();
            foreach (MeshRenderer meshRenderer in m_MeshRenderers)
            {
                meshRenderer.enabled = p_Enable;
            }
        }

        public void EnableCollider(bool p_Enable)
        {
            CollectColliders();
            foreach (Collider collider in m_Colliders)
            {
                collider.enabled = p_Enable;
            }

#if ABS_ENABLE_NAVMESH
            if (p_Enable)
            {
                EnableNavmesh();
            }
            else
            {
                DisableNavmesh();
            }
#endif
        }

        private void MakePrebuiltState()
        {
            if (m_PreBuilt)
            {
                m_State = ABS_BuildingElementState.PREBUILT;
                DisablePlayerCollision();
            }
            else
            {
                m_State = ABS_BuildingElementState.NORMAL;
                EnablePlayerCollision();
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            SetMaterialBasedOnStateImpl();
        }

        public bool IsPositionValidBasedOnAxis(in bool p_XAxisUsed)
        {
            if (AdvancedGridType == ABS_AdvancedGridType.Wall || AdvancedGridType == ABS_AdvancedGridType.EdgeHorizontal)
            {
                return AdvancedGridAxisType == ABS_AdvancedGridAxisType.Both
                    || (p_XAxisUsed && AdvancedGridAxisType != ABS_AdvancedGridAxisType.X)
                    || (!p_XAxisUsed && AdvancedGridAxisType != ABS_AdvancedGridAxisType.Z);
            }
            else
            {
                return true;
            }
        }

        public void SetMaterialBasedOnState()
        {
            if (m_State == ABS_BuildingElementState.PREBUILT)
            {
                DisablePlayerCollision();
            }

            SetMaterialBasedOnStateImpl();
        }

        private void SetMaterialBasedOnStateImpl()
        {
            if (m_HighlightStrategy == ABS_HighlightStrategy.None)
            {
                return;
            }
            else if (m_State != ABS_BuildingElementState.NORMAL)
            {
                Material mat = null;
                switch (m_State)
                {
                    case ABS_BuildingElementState.SIGNEDFORDELETE:
                        mat = m_HighlightCollection.DestroyHighlightMaterial;
                        break;
                    case ABS_BuildingElementState.BLOCKED:
                        mat = m_HighlightCollection.BlockedHighlightMaterial;
                        break;
                    case ABS_BuildingElementState.PENDING:
                        mat = m_HighlightCollection.PendingHighlightMaterial;
                        break;
                    case ABS_BuildingElementState.PREBUILT:
                        mat = m_HighlightCollection.PreBuiltHighlightMaterial;
                        break;
                }
                for (int i = 0; i < m_Renderers.Count; ++i)
                {
                    //m_Renderers[i].material = mat;

                    int count = (m_Renderers[i].materials).Length;
                    Material[] materials = new Material[count];

                    for (int j = 0; j < count; ++j)
                    {
                        materials[j] = mat;
                    }
                    m_Renderers[i].materials = materials;
                }
            }
            else
            {
                SetMaterialToDefault();
            }

        }

        public void SetHighlightMaterial(int p_Index)
        {
            if (m_HighlightStrategy == ABS_HighlightStrategy.None)
            {
                return;
            }

            Debug.Assert(p_Index < m_HighlightCollection.CustomHighlightMaterials.Count, "Index Out of Bounds Exception");

            for (int i = 0; i < m_Renderers.Count; ++i)
            {
                m_Renderers[i].materials = m_OutlineMaterialLists[i][p_Index];
            }

        }

        public void SetMaterialToDefault()
        {
            if (m_HighlightStrategy == ABS_HighlightStrategy.None)
            {
                return;
            }

            for (int i = 0; i < m_Renderers.Count; ++i)
            {
                m_Renderers[i].materials = m_DefaultMaterialLists[i];
            }
        }

        private void CollectRenderers()
        {
            m_Renderers.AddRange(gameObject.GetComponentsInChildren<Renderer>(true));
        }

        private void CollectMeshRenderers()
        {
            if (!m_MeshRenderersCollected)
            {
                m_MeshRenderers.AddRange(gameObject.GetComponentsInChildren<MeshRenderer>(true));
            }
        }

        private void CollectColliders()
        {
            if (!m_ColliderCollected)
            {
                m_Colliders.AddRange(gameObject.GetComponentsInChildren<Collider>(true));
                m_ColliderCollected = true;
            }
        }

        private void SaveDefaultMaterial()
        {
            if (m_DefaultMaterialCollected)
            {
                return;
            }

            m_DefaultMaterialCollected = true;

            if (m_HighlightStrategy == ABS_HighlightStrategy.None)
            {
                return;
            }
            else if (m_HighlightStrategy == ABS_HighlightStrategy.AutomaticCollection)
            {
                CollectRenderers();
            }

            Debug.Assert(m_Renderers.Count != 0, "Missing Renderers");
            Debug.Assert(m_HighlightCollection != null, "Missing HighlightCollection");
            Debug.Assert(m_HighlightCollection.CustomHighlightMaterials != null, "Missing CustomHighlightMaterials");

            foreach (Renderer renderer in m_Renderers)
            {
                Material[] materials = renderer.materials;

                Material[] newMaterials = new Material[materials.Length];
                for (int i = 0; i < newMaterials.Length; ++i)
                {
                    Material newMaterial = new Material(materials[i].shader);
                    newMaterial.CopyPropertiesFromMaterial(materials[i]);
#if UNITY_EDITOR
                    string newName = $"{newMaterial.name}_Copy";
                    if (!string.IsNullOrEmpty(newName))
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(newMaterial), newName);
                    }
#endif
                    newMaterials[i] = newMaterial;
                }
                m_DefaultMaterialLists.Add(newMaterials);

                List<Material> customHighlightMaterials = m_HighlightCollection.CustomHighlightMaterials;
                int highkightCount = customHighlightMaterials.Count;
                if (highkightCount > 0)
                {
                    m_OutlineMaterialLists.Add(new Material[highkightCount][]);
                    for (int i = 0; i < highkightCount; ++i)
                    {
                        Material[] newMaterialsForOutline = new Material[newMaterials.Length + 1];
                        for (int j = 0; j < newMaterials.Length; ++j)
                        {
                            newMaterialsForOutline[j] = newMaterials[j];
                        }

                        Material newOutlineMaterial = new Material(customHighlightMaterials[i].shader);
                        newOutlineMaterial.CopyPropertiesFromMaterial(customHighlightMaterials[i]);
#if UNITY_EDITOR
                        string newName = $"{newOutlineMaterial.name}_Copy";
                        if (!string.IsNullOrEmpty(newName))
                        {
                            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(newOutlineMaterial), newName);
                        }
#endif
                        newMaterialsForOutline[newMaterials.Length] = newOutlineMaterial;

                        m_OutlineMaterialLists[m_OutlineMaterialLists.Count - 1][i] = newMaterialsForOutline;
                    }
                }
            }
        }

        private void DisablePlayerCollision()
        {
            Debug.Assert(m_PositionAlgorithmSettings != null, "Missing Settings");
            CollectColliders();
            foreach (Collider collider in m_Colliders)
            {
                collider.excludeLayers = collider.excludeLayers.value | m_PositionAlgorithmSettings.LayerCollection.LayerOfPlayer.value;
            }

#if ABS_ENABLE_NAVMESH
            EnableNavmesh();
#endif
        }

        private void EnablePlayerCollision()
        {
            Debug.Assert(m_PositionAlgorithmSettings != null, "Missing Settings");
            CollectColliders();
            foreach (Collider collider in m_Colliders)
            {
                collider.excludeLayers = collider.excludeLayers.value & ~m_PositionAlgorithmSettings.LayerCollection.LayerOfPlayer.value;
            }

#if ABS_ENABLE_NAVMESH
            DisableNavmesh();
#endif
        }

#if ABS_ENABLE_NAVMESH
        private void EnableNavmesh()
        {
            CreateNavmesh();
            m_NavMeshObstacle.enabled = true;
        }
        private void DisableNavmesh()
        {
            CreateNavmesh();
            m_NavMeshObstacle.enabled = false;
        }

        private void CreateNavmesh()
        {
            if (m_NavMeshObstacle == null)
            {
                m_NavMeshObstacle = transform.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (m_NavMeshObstacle == null)
                {
                    m_NavMeshObstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                }
                m_NavMeshObstacle.carving = true;

                if (m_BuildingElementDimensionType == ABS_BuildingElementDimensionType.Fixed || m_DimensionCollider == null)
                {
                    m_NavMeshObstacle.size = Dimension;
                }
                else if (m_DimensionCollider != null)
                {
                    m_NavMeshObstacle.size = m_DimensionCollider.size;
                }

            }
        }
#endif

        public static void TransferConnection(
            in ABS_BuildingElement p_OriginalConnectionTarget,
            in ABS_BuildingElement p_NewConnectionTarget)
        {
            List<ABS_ActionElementConnectionData> lostConnectionsConnected = new List<ABS_ActionElementConnectionData>();
            p_OriginalConnectionTarget.RemoveAndCollectConnectedElementsData(null, lostConnectionsConnected, true);

            List<ABS_ActionElementConnectionData> lostConnections_ConnectionTarget = new List<ABS_ActionElementConnectionData>();
            p_OriginalConnectionTarget.RemoveAndCollectConnectionTargetElementsDate(lostConnections_ConnectionTarget);

            foreach (ABS_ActionElementConnectionData connectionData in lostConnectionsConnected)
            {
                p_NewConnectionTarget.ConnectElement(connectionData.ConnectionType, connectionData.BuildingElementInstance);
            }

            foreach (ABS_ActionElementConnectionData connectionData in lostConnections_ConnectionTarget)
            {
                connectionData.BuildingElementInstance.ConnectElement(connectionData.ConnectionType, p_NewConnectionTarget);
            }
        }

        public ABS_DestroyActionElementData Destroy(
            in ABS_BuildingManagerTracker p_Tracker, 
            in bool p_TriggeredByHistory,
            in bool p_DontDestroyTheParent,
            in bool p_IgnoreStability)
        {
            ABS_DestroyActionElementData actionData = new ABS_DestroyActionElementData();
            actionData.AddBuildingElement(this);

            //Call tracker
            if (p_Tracker != null)
            {
                if (p_TriggeredByHistory)
                {
                    p_Tracker.BuildingElementHistoryDestroyed(this);
                }
                else
                {
                    p_Tracker.BuildingElementDestroyed(this);
                }
            }

            //Handle connections
            Dictionary<ABS_DestroyActionElementData, ABS_BuildingElementConnectionType> destroyedConnectedData =
                new Dictionary<ABS_DestroyActionElementData, ABS_BuildingElementConnectionType>();
            List<ABS_ActionElementConnectionData> lostConnections_Connected = new List<ABS_ActionElementConnectionData>();
            List<ABS_ActionElementConnectionData> lostConnections_ConnectionTarget = new List<ABS_ActionElementConnectionData>();
            RemoveAndCollectConnectedElementsData(destroyedConnectedData, lostConnections_Connected, false);
            RemoveAndCollectConnectionTargetElementsDate(lostConnections_ConnectionTarget);

            actionData.AddDestroyedConenctedElementData(destroyedConnectedData);
            actionData.AddConnectedData(lostConnections_Connected);
            actionData.AddConenctionTargetData(lostConnections_ConnectionTarget);

            //Remove element from parent building
            bool isParentDestroyed = false;
            if (m_ParentBuilding != null)
            {
                m_ParentBuilding.RemoveBuildingElement(
                    actionData,
                    p_Tracker, 
                    p_TriggeredByHistory, 
                    p_IgnoreStability, 
                    this,
                    p_DontDestroyTheParent,
                    out isParentDestroyed);
            }
            m_ParentBuilding = null;
            actionData.BuildingData.IsParentDestroyed = isParentDestroyed;

            //Destroy
            DestroyImmediate(gameObject, true);
            return actionData;
        }

        public void OnDestroy()
        {
#if UNITY_EDITOR
            if (s_IsApplicationQuitting)
            {
                return;
            }
#endif

            if (m_ParentBuilding != null)
            {
                bool isParentDestroyed = false;
                m_ParentBuilding.RemoveBuildingElement(null, null, false, false, this, false, out isParentDestroyed);
            }

            DisconnectAllConnectedElements();
            DisconnectThisFromAllTarget();
        }

#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            s_IsApplicationQuitting = true;
        }
#endif

        private void RemoveAndCollectConnectedElementsData(
            in Dictionary<ABS_DestroyActionElementData, ABS_BuildingElementConnectionType> p_DestroyedConnectedData,
            in List<ABS_ActionElementConnectionData> p_LostConnections,
            in bool p_DoNotDestroyConnections)
        {
            int connectionMaxCount = m_ConnectedElements.Count;
            int i = 0;
            while (m_ConnectedElements.Count > 0 && (i++) < connectionMaxCount)
            {
                ABS_BuildingElement connectedElement = m_ConnectedElements[0];

                //Don't use the disconnect function becasue that will modify the m_ConnectedElements list
                ABS_BuildingElementConnectionType connectiontype;
                if (!connectedElement.ConnectionTargetElements.TryGetValue(this, out connectiontype))
                {
                    REST_Logging.Error($"{this}", $"Missing element connection. Connection Target : {this.name}  Connected Element : {connectedElement}");
                }

                if (!p_DoNotDestroyConnections && IsConnectedElementDestroyNeeded(connectiontype))
                {
                    //This destroy should remove the element fromt he m_ConnectedElements
                    //Because the connected element's process included the HandleThisElementsConnections call
                    //What willl remove itsellf from this connetion target's connections list
                    ABS_DestroyActionElementData connectedData = connectedElement.Destroy(null, false, false, false);
                    p_DestroyedConnectedData[connectedData] = connectiontype;
                }
                else
                {
                    connectedElement.ConnectionTargetElements.Remove(this);
                    ABS_ActionElementConnectionData connectionData = new ABS_ActionElementConnectionData(connectiontype);
                    connectionData.AddBuildingElement(connectedElement);
                    p_LostConnections.Add(connectionData);
                    m_ConnectedElements.RemoveAt(0);
                }
            }

            if (m_ConnectedElements.Count > 0)
            {
                REST_Logging.Error($"{this}", $"Unkown logic error : m_ConnectedElements count more than zero");
            }
        }

        private bool IsConnectedElementDestroyNeeded (ABS_BuildingElementConnectionType p_ConnectionType)
        {
            return p_ConnectionType == ABS_BuildingElementConnectionType.Attachment;
        }

        private void RemoveAndCollectConnectionTargetElementsDate(List<ABS_ActionElementConnectionData> p_LostConnections)
        {
            foreach ((ABS_BuildingElement connectionTarget, ABS_BuildingElementConnectionType type) in m_ConnectionTargetElements)
            {
                ABS_ActionElementConnectionData connectionData = new ABS_ActionElementConnectionData(type);
                connectionData.AddBuildingElement(connectionTarget);
                p_LostConnections.Add(connectionData);
                connectionTarget.m_ConnectedElements.Remove(this);
            }
            m_ConnectionTargetElements.Clear();
        }

        public void ConnectElement(ABS_BuildingElementConnectionType p_ConnectionType, ABS_BuildingElement p_Element)
        {
            if (p_Element == null)
            {
                REST_Logging.Error($"{this}", "Connector element is null");
                return;
            }

            if (p_Element == this)
            {
                REST_Logging.Error($"{this}", "Connection the same element");
                return;
            }

            ABS_BuildingElement elementInList = m_ConnectedElements.Find(e => e == p_Element);
            if (elementInList == null)
            {
                m_ConnectedElements.Add(p_Element);
                p_Element.ConnectionTargetElements[this] = p_ConnectionType;
            }
            else
            {
                REST_Logging.Error($"{this}", "Element already linked.");
            }
        }

        public void DisconnectElement(ABS_BuildingElement p_Element)
        {
            if (m_ConnectedElements.Remove(p_Element))
            {
                p_Element.ConnectionTargetElements.Remove(this);
            }
            else
            {
                REST_Logging.Error($"{this}", "Could not unlink element.");
            }
        }

        public void DisconnectAllConnectedElements()
        {
            foreach (ABS_BuildingElement element in m_ConnectedElements)
            {
                element.ConnectionTargetElements.Remove(this);
            }

            m_ConnectedElements.Clear();
        }

        public void DisconnectThisFromAllTarget()
        {
            foreach ((ABS_BuildingElement connectionTarget, ABS_BuildingElementConnectionType type) in m_ConnectionTargetElements)
            {
                connectionTarget.m_ConnectedElements.Remove(this);
            }
            m_ConnectionTargetElements.Clear();
        }

        public static ABS_BuildingElement InstantiateClone(ABS_BuildingElement p_element)
        {
            return Instantiate(p_element);
        }

        public void RefreshLink()
        {
            RefreshLinkReq(gameObject, true);
        }

        private void RefreshLinkReq(in GameObject p_GameObject, in bool p_Base)
        {
            if (!p_Base)
            {
                Collider[] colliders = p_GameObject.GetComponents<Collider>();
                if (colliders.Length > 0)
                {
                    ABS_BuildingElementLink[] links = p_GameObject.GetComponents<ABS_BuildingElementLink>();
                    if (links.Length > 1)
                    {
                        REST_Logging.Warrning($"{this}", $"Multiple Link on element: {this.name} | child: {p_GameObject.name}");
                    }
                    else if (links.Length == 1)
                    {
                        REST_Logging.Info($"{this}", $"Link refreshed for element : {this.name} | child: {p_GameObject.name}");
                        links[0].m_Element = this;
                    }
                    else
                    {
                        REST_Logging.Info($"{this}", $"Link added for element : {this.name} | child: {p_GameObject.name}");
                        ABS_BuildingElementLink link = p_GameObject.AddComponent<ABS_BuildingElementLink>();
                        link.m_Element = this;
                    }
                }
            }

            foreach (Transform child in p_GameObject.transform)
            {
                RefreshLinkReq(child.gameObject, false);
            }
        }

#if ABS_ENABLE_NAVMESH
        public void RefreshNavMeshSize()
        {
            UnityEngine.AI.NavMeshObstacle[] obstaclels = gameObject.GetComponents<UnityEngine.AI.NavMeshObstacle>();
            if (obstaclels.Length > 1)
            {
                REST_Logging.Warrning($"{this}", $"The refresh failed becasue multiple NavMeshObstacle have been found. Count : {obstaclels.Length}");
            }
            else
            {
                CreateNavmesh();
                if (m_BuildingElementDimensionType == ABS_BuildingElementDimensionType.ColliderBased)
                {
                    if ( m_DimensionCollider == null)
                    {
                        REST_Logging.Warrning($"{this}", 
                            "The refresh failed becasue the BuildingElementDimensionType is set to ColliderBased but DimensionCollider is null");
                    }
                    else
                    {
                       m_NavMeshObstacle.size = m_DimensionCollider.size;
                    }
                }
                else
                {
                    m_DimensionCollider.size = m_Dimension;
                }
            }
        }
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            OnDrawGizmosImpl(true);
        }

        private void OnDrawGizmos()
        {
            OnDrawGizmosImpl(false);
        }
        private void OnDrawGizmosImpl(in bool p_Selected)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (m_ProjectSettings == null)
            {
                m_ProjectSettings = ABS_ProjectSettingsGetter.GetSettings();
            }
            /*
            if (m_PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.SnapPointBased)
            {
                ABS_SnapPointBasedBuilderSettings settings = m_PositionAlgorithmSettings as ABS_SnapPointBasedBuilderSettings;
                if (settings == null || settings.ABS_SnapRelationshipList == null)
                {
                    return;
                }
                else
                {
                    settings.ABS_SnapRelationshipList.OnDrawGizmos();
                }
            }*/

            Transform elementTrasform = this.transform;

            if (m_ProjectSettings.BuildingElement_Stability
                && (!m_ProjectSettings.BuildingElement_StabilityWhenSelected || p_Selected))
            {
                ABS_AdvancedGridBuilderSettings settings = m_PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings;
                ABS_AdvancedGridBuilding parent = ParentBuilding as ABS_AdvancedGridBuilding;
                if (settings != null && settings.IsStabilitySupported() && parent != null && parent.EnableStability)
                {
                    float normalizedValue = (1.0f / parent.StabilityLevel) * m_StabilityLevel;
                    UnityEngine.Color color = Color.Lerp(Color.red, Color.green, normalizedValue);
                    REST_GizmosUtils.DrawText(elementTrasform.position, $"{m_StabilityLevel}", color);
                }
            }
        }
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Persistency
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public class ABS_BuildingElementConnectionData
        {
            public ABS_BuildingElementConnectionType ConnectionType;
            public string ConnectionTargetElementGuid = string.Empty;
            public string ConnectionTargetBuildingGuid = string.Empty;
            public ABS_BuildingElement ConnectedElement = null;
        }


        [System.Serializable]
        public class ABS_BuildingElementPersistedConnectionData
        {
            public ABS_BuildingElementConnectionType ConnectionType;
            public string ConnectionTargetElementGuid = string.Empty;
            public string ConnectionTargetBuildingGuid = string.Empty;
        }

        [System.Serializable]
        public class ABS_BuildingElementPersistedData : ABS_PersistedData
        {
            private readonly static short s_PreBuilt_Idx = 0;
            private readonly static short s_Foundation_Idx = 1;
            private readonly static short s_Indestructible_Idx = 2;
            private readonly static short s_SnapToPreBuiltFinalElement_Idx = 3;
            private readonly static short s_ShouldOverride_Idx = 4;
            private readonly static short s_ShouldAllowedByArea_Idx = 5;
            private readonly static short s_Stable_Idx = 6;

            public Vector3 LocalPosition = Vector3.zero;
            public Vector3 LocalEulerAngles = Vector3.zero;

            public List<ABS_BuildingElementPersistedConnectionData> Connections = new List<ABS_BuildingElementPersistedConnectionData>();

            public ulong Properties = 0;
            public short StabilityLevel = 0;

            public bool PreBuilt
            {
                get { return Get(s_PreBuilt_Idx); }
                set { Set(value, s_PreBuilt_Idx); }
            }
            public bool Foundation
            {
                get { return Get(s_Foundation_Idx); }
                set { Set(value, s_Foundation_Idx); }
            }
            public bool Indestructible
            {
                get { return Get(s_Indestructible_Idx); }
                set { Set(value, s_Indestructible_Idx); }
            }
            public bool SnapToPreBuiltFinalElement
            {
                get { return Get(s_SnapToPreBuiltFinalElement_Idx); }
                set { Set(value, s_SnapToPreBuiltFinalElement_Idx); }
            }
            public bool ShouldOverride
            {
                get { return Get(s_ShouldOverride_Idx); }
                set { Set(value, s_ShouldOverride_Idx); }
            }
            public bool ShouldAllowedByArea
            {
                get { return Get(s_ShouldAllowedByArea_Idx); }
                set { Set(value, s_ShouldAllowedByArea_Idx); }
            }
            public bool Stable
            {
                get { return Get(s_Stable_Idx); }
                set { Set(value, s_Stable_Idx); }
            }
            
            private bool Get (short p_Idx)
            {
                return (Properties & (ulong)(1 << p_Idx)) > 0;
            }
            private void Set(bool p_Value, short p_Idx)
            {
                if (p_Value) Properties |= ((ulong)1 << p_Idx);
            }

            public override string ToJSON(in bool p_PrettyPrint)
            {
                return ABS_PersistencyManager.ToJson(this, p_PrettyPrint);
            }
        }

        public override string ToJSON(in bool p_PrettyPrint)
        {
            return GetPersistedData().ToJSON(p_PrettyPrint);
        }

        public ABS_BuildingElementPersistedData GetPersistedData()
        {
            ABS_BuildingElementPersistedData data = new ABS_BuildingElementPersistedData();
            GetBasicPersistedDataValues(data);
            SaveData(data);
            return data;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Persistency Static implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static ABS_PersistencyLoadErrorCode CreateFromPersistedJSON(
            in string p_JsonString,
            ABS_Building p_TargetParent,
            in ABS_BuildingElementList p_ElementList,
            out ABS_BuildingElement p_BuildingElement,
            List<ABS_BuildingElementConnectionData> p_Connections)
        {
            if (string.IsNullOrEmpty(p_JsonString))
            {
                p_BuildingElement = null;
                return ABS_PersistencyLoadErrorCode.JSON_NullOrEmptyString;
            }

            ABS_BuildingElementPersistedData data = ABS_PersistencyManager.FromJson<ABS_BuildingElementPersistedData>(p_JsonString);
            if (data == null)
            {
                p_BuildingElement = null;
                return ABS_PersistencyLoadErrorCode.JSON_ErrorDuringLoad;
            }

            ABS_BuildingElement buildingElementPrefab = p_ElementList.GetElementPrefab(data.PrefabGuid);
            if (buildingElementPrefab == null)
            {
                REST_Logging.Warrning("ABS_BuildingElement",
                    $"Can't find element in the provided BuildingElementList. Element : {data.Name} PrefabGUID : {data.PrefabGuid}");
                p_BuildingElement = null;
                return ABS_PersistencyLoadErrorCode.BuildingElementList_MissingElement;
            }

            return CreateFromPersistedData(data, p_TargetParent, buildingElementPrefab, out p_BuildingElement, p_Connections);
        }

        public static ABS_PersistencyLoadErrorCode CreateFromPersistedData (
            ABS_BuildingElementPersistedData p_PersistedData,
            ABS_Building p_TargetParent, 
            ABS_BuildingElement p_ElementPrefab,
            out ABS_BuildingElement p_BuildingElement,
            List<ABS_BuildingElementConnectionData> p_Connections)
        {
            if (p_ElementPrefab == null)
            {
                p_BuildingElement = null;
                return ABS_PersistencyLoadErrorCode.BuildingElement_NullPrefab;
            }

            GameObject go = Instantiate(p_ElementPrefab.gameObject);
            if (go == null)
            {
                p_BuildingElement = null;
                return ABS_PersistencyLoadErrorCode.BuildingElement_UnkownInstantiateError;
            }

            p_BuildingElement = go.GetComponent<ABS_BuildingElement>();
            if (p_BuildingElement != null)
            {
                p_BuildingElement.SetBasicPersistedDataValues(p_PersistedData);
                p_BuildingElement.LoadDataFromPersistedData(p_PersistedData);
            }
            else
            {
                return ABS_PersistencyLoadErrorCode.BuildingElement_UnkownComponentError;
            }

            if (p_TargetParent.AddBuildingElement(
                    p_Tracker : null,
                    p_TriggeredByHistory : false,
                    p_NewElement : p_BuildingElement,
                    p_LocalPosition: p_PersistedData.LocalPosition,
                    p_LocalEulerAngles: p_PersistedData.LocalEulerAngles,
                    p_Force : false,
                    p_DestroyOld : false) == null)
            {
                return ABS_PersistencyLoadErrorCode.BuildingElement_AddToBuildingFailed;
            }

            p_BuildingElement.CreateConnectionList(p_PersistedData, p_Connections);
            return ABS_PersistencyLoadErrorCode.Successful;
        }

        public ABS_PersistencyLoadErrorCode UpdateDataFromPersistedData(in ABS_BuildingElementPersistedData p_PersistedData, 
                                                                        in bool p_AlreadyChecked,
                                                                        in List<ABS_BuildingElementConnectionData> p_Connections)
        {
            if (!p_AlreadyChecked)
            {
                ABS_PersistencyLoadErrorCode res = CheckUpdatePossibility(p_PersistedData);
                if (res != ABS_PersistencyLoadErrorCode.Successful)
                {
                    return res;
                }
            }

            LoadDataFromPersistedData(p_PersistedData);
            CreateConnectionList(p_PersistedData, p_Connections);
            return ABS_PersistencyLoadErrorCode.Successful;
        }

        public ABS_PersistencyLoadErrorCode CheckUpdatePossibility(in ABS_BuildingElementPersistedData p_PersistedData)
        {
            if (p_PersistedData == null)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_NullInput;
            }

            if (p_PersistedData.Type != ABS_SaveableMonobehaviour.DataType.BuildingElement)
            {
                return ABS_PersistencyLoadErrorCode.PersistedData_WrongDataType;
            }

            if (!CompareBasicPersistedDataValues(p_PersistedData))
            {
                return ABS_PersistencyLoadErrorCode.Update_WrongBasicObjectValues_BuildingElement;
            }

            return ABS_PersistencyLoadErrorCode.Successful;
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Persistency private implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void SaveData(ABS_BuildingElementPersistedData p_PersistedData)
        {
            p_PersistedData.LocalPosition = transform.localPosition;
            p_PersistedData.LocalEulerAngles = transform.localEulerAngles;

            p_PersistedData.PreBuilt = m_PreBuilt;
            p_PersistedData.Foundation = m_Foundation;
            p_PersistedData.Indestructible = m_Indestructible;
            p_PersistedData.SnapToPreBuiltFinalElement = m_SnapToPreBuiltFinalElement;

            p_PersistedData.ShouldOverride = m_ShouldOverride;
            p_PersistedData.ShouldAllowedByArea = m_ShouldAllowedByArea;

            p_PersistedData.StabilityLevel = m_StabilityLevel;
            p_PersistedData.Stable = m_Stable;

            foreach ((ABS_BuildingElement element, ABS_BuildingElementConnectionType connectionType) in m_ConnectionTargetElements)
            {
                ABS_BuildingElementPersistedConnectionData connection = new ABS_BuildingElementPersistedConnectionData();
                connection.ConnectionTargetElementGuid = element.InstanceGuid;
                connection.ConnectionTargetBuildingGuid = element.ParentBuilding.InstanceGuid;
                connection.ConnectionType = connectionType;
                p_PersistedData.Connections.Add(connection);
            }
        }

        private void LoadDataFromPersistedData(ABS_BuildingElementPersistedData p_PersistedData)
        {
            PreBuilt = p_PersistedData.PreBuilt;
            Foundation = p_PersistedData.Foundation;
            Indestructible = p_PersistedData.Indestructible;
            SnapToPreBuiltFinalElement = p_PersistedData.SnapToPreBuiltFinalElement;

            ShouldOverride = p_PersistedData.ShouldOverride;
            ShouldAllowedByArea = p_PersistedData.ShouldAllowedByArea;

            StabilityLevel = p_PersistedData.StabilityLevel;
            Stable = p_PersistedData.Stable;
        }

        private void CreateConnectionList(ABS_BuildingElementPersistedData p_PersistedData, 
                                          List<ABS_BuildingElementConnectionData> p_Connections)
        {
            foreach (ABS_BuildingElementPersistedConnectionData persistedConnectionData in p_PersistedData.Connections)
            {
                ABS_BuildingElementConnectionData connectionData = new ABS_BuildingElementConnectionData();
                connectionData.ConnectionType = persistedConnectionData.ConnectionType;
                connectionData.ConnectionTargetElementGuid = persistedConnectionData.ConnectionTargetElementGuid;
                connectionData.ConnectionTargetBuildingGuid = persistedConnectionData.ConnectionTargetBuildingGuid;
                connectionData.ConnectedElement = this;
                p_Connections.Add(connectionData);
            }
        }
    }
}
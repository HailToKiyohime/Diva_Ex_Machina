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
    public class ABS_TemporaryBuildingElementHandler : ABS_BuildingManagerComponentBaseMonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BuilderBaseSettings m_Settings = null;
        private ABS_BuildingElement m_ActiveBuildingElement = null;
        private bool m_AllowMixedAxisDragBuilding = false;
        private bool m_HasFinalElement = false;

        private ABS_ObjectPoolBase m_ObjectPool = null;

        private List<List<ABS_TemporaryBuildingElement>> m_Elements = null;

        //First Element
        private ABS_TemporaryBuildingElement m_FirstElement = null;
        private int m_FirstElementXIndex = 0;

        //Position Result
        private ABS_PositionSearchResult m_PositionSearchResult = null;

#if UNITY_EDITOR
        protected ulong m_StatisticsInstantiatedObject = 0;
        protected ulong m_StatisticsDestroyedObject = 0;
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters / Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool AllowMixedAxisDragBuilding
        {
            set { m_AllowMixedAxisDragBuilding = value; }
            get { return m_AllowMixedAxisDragBuilding; }
        }

        public ABS_BuilderBaseSettings Settings
        {
            set { m_Settings = value; }
            get { return m_Settings; }
        }

        public List<List<ABS_TemporaryBuildingElement>> Elements { get { return m_Elements; } }

#if UNITY_EDITOR
        public ulong StatisticsInstantiatedObject
        {
            get { return m_StatisticsInstantiatedObject; }
        }
        public ulong StatisticsDestroyedObject
        {
            get { return m_StatisticsDestroyedObject; }
        }
#endif

        public ABS_PositionSearchResult PositionSearchResult
        {
            set
            {
                m_PositionSearchResult = value;
                m_Elements[m_FirstElementXIndex][0].ValidationData = value.ValidationResult;
            }
        }

        public Vector3 GetLocalRaycastPosition ()
        {
            return transform.InverseTransformPoint(m_Manager.GetRaycastHitOrEndPosition());
        }

        public int FirstElementXIndex
        {
            get { return m_FirstElementXIndex; }
        }

        public int GetDimensionX()
        {
            return m_Elements.Count;
        }

        public int GetDimensionZ(int p_X)
        {
            return m_Elements.Count > p_X ? m_Elements[p_X].Count : 0;
        }

        public ABS_TemporaryBuildingElement GetElement(int p_X, int p_Z)
        {
            return m_Elements[p_X][p_Z];
        }

        public ABS_PositionValidationData GetValidationResult(int p_X, int p_Z)
        {
            return m_Elements[p_X][p_Z].ValidationData;
        }

        public bool Avaliable()
        {
            return m_FirstElement != null && m_FirstElement.Avaliable();
        }

        public void Block()
        {
            m_FirstElement.SetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_LOGIC);
        }

        public void UnBlock()
        {
            m_FirstElement.UnSetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_LOGIC);
        }
        
        public ABS_TemporaryBuildingElement.ABS_BlockState GetBlockState()
        {
            return m_FirstElement.BlockState;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_TemporaryBuildingElementHandler() : base()
        {
            m_Elements = new List<List<ABS_TemporaryBuildingElement>>();
        }

        public void Init(
            ABS_IBuildingManagerInternalInterface p_Manager,
            ABS_BuildingManagerTracker p_Tracker,
            ABS_ObjectPoolBase p_ObjectPool)
        {
            base.Init(p_Manager, p_Tracker);
            m_ObjectPool = p_ObjectPool;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  public implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void ResetTemporaryBuildingElement(ABS_BuildingElement p_TargetBuidlignElement)
        {
            m_ActiveBuildingElement = p_TargetBuidlignElement;

            if (m_ActiveBuildingElement != null)
            {
                m_HasFinalElement = m_ActiveBuildingElement.FinalElement != null;
            }
        }

        public void ResetTemoraryElementsList(in bool p_LeaveFirstElement, in bool p_ElementsArePlaced)
        {
            for (int x = 0; x < m_Elements.Count; ++x)
            {
                for (int z = 0; z < m_Elements[x].Count; ++z)
                {
                    if (!(x == m_FirstElementXIndex && z == 0) || !p_LeaveFirstElement)
                    {
                        DestroyImpl(m_Elements[x][z], p_ElementsArePlaced);
                        m_Elements[x][z] = null;
                    }
                }
            }

            m_Elements.Clear();

            if (p_LeaveFirstElement)
            {
                AddFirstElement(m_FirstElement);
                m_Tracker.CurrentValidBuildingElements(1);
            }
            else
            {
                m_Tracker.CurrentValidBuildingElements(0);
                m_FirstElement = null;
            }
        }

        public void FillUpColumnWithNull (in int p_X, in int p_ZEnd)
        {
            List<ABS_TemporaryBuildingElement> listElementX = m_Elements[p_X];
            for (int i = listElementX.Count; i < p_ZEnd; ++i)
            {
                listElementX.Add(null);
            }
        }

        public void ClearColumn(in int p_X)
        {
            //First Destroy Every ABS_BuildingElement
            for (int z = 0; z < m_Elements[p_X].Count; ++z)
            {
                DestroyImpl(m_Elements[p_X][z], false);
                m_Elements[p_X][z] = null;
            }

            //Second remove the lists
            m_Elements[p_X].Clear();
        }

        public void RemoveZElements(in int p_X, in int p_ZStart, in int p_ZEnd)
        {
            List<ABS_TemporaryBuildingElement> zColumn = m_Elements[p_X];
            int countZ = zColumn.Count;
            countZ = countZ <= p_ZEnd ? countZ : countZ - 1;

            for (int z = p_ZStart; z < countZ; ++z)
            {
                DestroyImpl(zColumn[z], false);
                zColumn[z] = null;
            }
        }

        public void RemoveInnerElements(in int p_XByHitpoint_ABS, in int p_ZByHitpoint_ABS)
        {
            int countX = GetDimensionX();
            countX = countX < p_XByHitpoint_ABS ? countX : countX - 1;

            for (int x = 1; x < countX; ++x)
            {
                RemoveZElements(x, 1, p_ZByHitpoint_ABS);
            }
        }

        public void RemoveColumns(int p_RemoveCount)
        {
            int xDimension = GetDimensionX();
            int removeStartIndex = xDimension - p_RemoveCount;
            
            //First destroy every ABS_BuildingElement
            for (int i = removeStartIndex; i < xDimension; ++i)
            {
                for (int j = 0; j < m_Elements[i].Count; ++j)
                {
                    if (!(i == 0 && j == 0))
                    {
                        DestroyImpl(m_Elements[i][j], false);
                        m_Elements[i][j] = null;
                    }
                }
            }
            //Second remove the lists
            int index = removeStartIndex == 0 ? 1 + FirstElementXIndex : removeStartIndex;
            m_Elements.RemoveRange(index, p_RemoveCount);
        }

        public void RemoveRows(int p_RemoveCount)
        {
            //First Destroy Every ABS_BuildingElement
            for (int x = 0; x < GetDimensionX(); ++x)
            {
                //Checkif the column was already cleared
                if (m_AllowMixedAxisDragBuilding && x == 0 && m_Elements[0].Count == 0)
                {
                    continue;
                }

                int removeRange = m_Elements[x].Count - p_RemoveCount;
                for (int z = removeRange; z < m_Elements[x].Count; ++z)
                {
                    if (!(x == 0 && z == 0))
                    {
                        DestroyImpl(m_Elements[x][z], false);
                        m_Elements[x][z] = null;
                    }
                }
                //Second remove the lists
                int removeStartIndex = (x == 0 && removeRange == 0 ? 1 : removeRange);
                m_Elements[x].RemoveRange(removeStartIndex, p_RemoveCount);
            }
        }

        public void AddColumn()
        {
            m_Elements.Add(new List<ABS_TemporaryBuildingElement>());
        }

        public void AddColumnsUntil(in int p_TargetX)
        {
            for (int x = m_Elements.Count; x < p_TargetX; ++x)
            {
                AddColumn();
            }
        }

        public void AddRowElement(in int p_TargetX)
        {
            m_Elements[p_TargetX].Add(null);
        }

        public bool CreateElement(in int p_XByHitpoint, in int p_ZByHitpoint, in int p_CurrentXIndex, in int p_CurrentZIndex, in Vector3 p_DragGridSize)
        {
            int currentXIndexAligned = p_CurrentXIndex - FirstElementXIndex;
            //this can be true only in case of AdvancedGridBuilding
            if (m_AllowMixedAxisDragBuilding)
            {
                if (!CanBuildOnPositionDuringMixedAxis(currentXIndexAligned, p_CurrentZIndex))
                {
                    return false;
                }
            }

            float xIndex = p_DragGridSize.x * currentXIndexAligned;
            float signedXIndex = xIndex * (p_XByHitpoint < 0 ? -1 : 1);
            float zIndex = p_DragGridSize.z * p_CurrentZIndex;
            float signedZIndex = zIndex * (p_ZByHitpoint < 0 ? -1 : 1);
            Vector3 FinalPosition = new Vector3(signedXIndex, 0.0f, signedZIndex);

            ABS_TemporaryBuildingElement element = InstantiateElement(FinalPosition, (m_AllowMixedAxisDragBuilding && p_CurrentXIndex % 2 == 0));

            m_Elements[p_CurrentXIndex][p_CurrentZIndex] = element;

            if (p_CurrentXIndex == FirstElementXIndex && p_CurrentZIndex == 0)
            {
                m_Elements[p_CurrentXIndex][p_CurrentZIndex].ValidationData = GetFirstElementResult();
            }
            else
            {
                ABS_PositionValidationData positionValidationResult = m_Manager.ValidatePosition(element.transform.position, this.transform.rotation);
                m_Elements[p_CurrentXIndex][p_CurrentZIndex].ValidationData = positionValidationResult;
            }
            return true;
        }

        private bool CanBuildOnPositionDuringMixedAxis(in int p_X, in int p_Z)
        {
            return (p_X % 2 == 0 && p_Z % 2 == 0) || (p_X % 2 != 0 && p_Z % 2 != 0);
        }

        public void CreateFirstElement(uint p_MaxElementCount)
        {
            m_FirstElementXIndex = m_AllowMixedAxisDragBuilding ? 1 : 0;
            m_FirstElement = InstantiateElement(Vector3.zero, false).GetComponent<ABS_TemporaryBuildingElement>();
            m_FirstElement.FirstElement = true;
            m_FirstElement.Tracker = m_Tracker;
            AddFirstElement(m_FirstElement);
            if (p_MaxElementCount == 0)
            {
                m_FirstElement.SetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_NOT_ENOUGH_MATERIAL);
            }
            else
            {
                m_Tracker.FirstBuildingElementBlockedStateChanged(m_FirstElement.BlockState);
            }
        }


#if UNITY_EDITOR
        public void StatisticsReset()
        {
            m_StatisticsInstantiatedObject = 0;
            m_StatisticsDestroyedObject = 0;
        }
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  private implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void DestroyImpl(ABS_TemporaryBuildingElement p_Element, in bool p_ElementsArePlaced)
        {
            if (p_Element == null)
            {
                return;
            }

            if (m_ObjectPool == null)
            {
#if UNITY_EDITOR
                ++m_StatisticsDestroyedObject;
#endif
                Destroy(p_Element.gameObject);
            }
            else
            {
                if (p_ElementsArePlaced && !m_HasFinalElement)
                {
                    m_ObjectPool.Release(p_Element.TargetBuildingElement);
                }
                else
                {
                    m_ObjectPool.GiveBack(p_Element.TargetBuildingElement);
                }

                Destroy(p_Element.gameObject);
            }
        }

        private ABS_BuildingElement InstantiateImpl()
        {
            if (m_ObjectPool == null)
            {
#if UNITY_EDITOR
                ++m_StatisticsInstantiatedObject;
#endif
                return Instantiate(m_ActiveBuildingElement);
            }
            else
            {
                ABS_BuildingElement element = m_ObjectPool.Get(m_ActiveBuildingElement);
                if (element == null)
                {
                    REST_Logging.Warrning("ABS_TemporaryBuildingElementHandler", "Get null element returned!");
                    element = Instantiate(m_ActiveBuildingElement);
                }
                return element;
            }
        }

        private ABS_TemporaryBuildingElement InstantiateElement(Vector3 p_Position, bool p_RotationIsNeeded)
        {
            GameObject go = new GameObject("ABS_TemporaryBuildingElement");
            go.layer = this.gameObject.layer;
            go.transform.parent = this.transform;
            go.transform.localPosition = p_Position;

            if (p_RotationIsNeeded)
            {
                go.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + (Vector3.up * 90));
            }
            else
            {
                go.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles);
            }

            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = m_ActiveBuildingElement.Dimension + (Vector3.one * 0.2f);
            collider.isTrigger = true;
            collider.includeLayers = m_Settings.LayerCollection.LayerOfPlayer;

            ABS_TemporaryBuildingElement tmpE = go.AddComponent<ABS_TemporaryBuildingElement>();
            ABS_BuildingElement element = InstantiateImpl();
            tmpE.Init(element);
            return tmpE;
        }


        private void AddFirstElement(in ABS_TemporaryBuildingElement p_BE)
        {
            AddColumn();

            if (m_AllowMixedAxisDragBuilding)
            {
                AddColumn();
            }

            m_Elements[m_FirstElementXIndex].Add(p_BE);
            p_BE.ValidationData = GetFirstElementResult();
        }

        private ABS_PositionValidationData GetFirstElementResult()
        {
            if (m_PositionSearchResult != null)
            {
                return m_PositionSearchResult.ValidationResult;
            }
            else
            {
                return new ABS_PositionValidationData();
            }
        }
    }
}
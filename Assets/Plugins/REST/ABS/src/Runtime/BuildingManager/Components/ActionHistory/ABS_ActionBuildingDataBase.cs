//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_ActionBuildingDataBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_Building m_BuildingInstance = null;
        private ABS_BuildingParent m_BuildingParent = null;
        private string m_BuildingInstanceGuid = string.Empty;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        virtual public void Init(ABS_Building p_Building)
        {
            m_BuildingInstance = p_Building;
            m_BuildingParent = p_Building.Parent;
            m_BuildingInstanceGuid = m_BuildingInstance.InstanceGuid;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_Building BuildingInstance
        {
            get { return m_BuildingInstance; }
            set { m_BuildingInstance = value; }
        }

        public ABS_BuildingParent BuildingParent => m_BuildingParent;
        public string BuildingInstanceGuid => m_BuildingInstanceGuid;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }

    public class ABS_ActionBuildingDataWithPropertiesBase : ABS_ActionBuildingDataBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private Vector3 m_ParentPosition = Vector3.zero;
        private Vector3 m_ParentEulerAngles = Vector3.zero;
        private Vector3 m_PositionModifier = Vector3.zero;
        private uint m_MaximumElementCount = 1000;
        private bool m_UseCache = false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override void Init(ABS_Building p_Building)
        {
            base.Init(p_Building);
            m_ParentPosition = p_Building.gameObject.transform.position;
            m_ParentEulerAngles = p_Building.gameObject.transform.eulerAngles;
            m_MaximumElementCount = p_Building.MaximumElementCount;
            m_UseCache = p_Building.EnableCache;
            if (p_Building.PositionSearchAlgorithmType == ABS_PositionSearchAlgorithm.AdvancedGrid)
            {
                m_PositionModifier = (p_Building as ABS_AdvancedGridBuilding).BuildingPositionModifier;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public Vector3 ParentPosition => m_ParentPosition;
        public Vector3 ParentEulerAngles => m_ParentEulerAngles;
        public Vector3 PositionModifier => m_PositionModifier;
        public uint MaximumElementCount => m_MaximumElementCount;
        public bool UseCache => m_UseCache;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}
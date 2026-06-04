//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System.Xml.Serialization;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_ActionElementDataBase<BuildingDataType> 
        where BuildingDataType : ABS_ActionBuildingDataBase, new()
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected ABS_BuildingElement m_BuildingElementPrefab = null;
        protected ABS_BuildingElement m_BuildingElementInstance = null;

        protected string m_PrefabGuid = string.Empty;
        protected string m_InstanceGuid = string.Empty;

        protected Vector3 m_LocalPosition = Vector3.zero;
        protected Vector3 m_LocalEulerAngles = Vector3.zero;

        protected bool m_Prebuilt = false;

        protected short m_Stability = -2;
        protected bool m_Stable = false;

        BuildingDataType m_BuildingData = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElement BuildingElementPrefab
        {
            get => m_BuildingElementPrefab;
            set { m_BuildingElementPrefab = value; }
        }
        
        public ABS_BuildingElement BuildingElementInstance
        {
            get => m_BuildingElementInstance;
            set { m_BuildingElementInstance = value; }
        }

        public string PrefabGuid => m_PrefabGuid;
        public string InstanceGuid => m_InstanceGuid;
        public Vector3 LocalPosition => m_LocalPosition;
        public Vector3 LocalEulerAngles => m_LocalEulerAngles;

        public BuildingDataType BuildingData => m_BuildingData;

        public bool Prebuilt => m_Prebuilt;

        public short Stability => m_Stability;
        public bool Stable => m_Stable;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public virtual void AddBuildingElement(ABS_BuildingElement p_BuildingElement)
        {
            AddBuildingElementImpl(p_BuildingElement);

            m_BuildingData = new BuildingDataType();
            m_BuildingData.Init(p_BuildingElement.ParentBuilding);
        }

        public virtual void AddBuildingElement(ABS_BuildingElement p_BuildingElement, BuildingDataType p_BuildingData)
        {
            AddBuildingElementImpl(p_BuildingElement);

            m_BuildingData = p_BuildingData;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void AddBuildingElementImpl (ABS_BuildingElement p_BuildingElement)
        {
            m_BuildingElementInstance = p_BuildingElement;

            m_PrefabGuid = p_BuildingElement.PrefabGuid;
            m_InstanceGuid = p_BuildingElement.InstanceGuid;
            m_LocalPosition = p_BuildingElement.gameObject.transform.localPosition;
            m_LocalEulerAngles = p_BuildingElement.gameObject.transform.localEulerAngles;
            m_Prebuilt = p_BuildingElement.PreBuilt;
            m_Stability = p_BuildingElement.StabilityLevel;
            m_Stable = p_BuildingElement.Stable;
        }
    }
}
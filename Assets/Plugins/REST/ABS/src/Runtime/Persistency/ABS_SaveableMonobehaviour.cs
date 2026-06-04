//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_SaveableMonobehaviour : ABS_MonoBehaviourBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum DataType
        {
            Unkown,
            BuildingElement,
            Building,
            BuildingParent
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] protected DataType m_ISaveableType = DataType.Unkown;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void GetBasicPersistedDataValues (ABS_PersistedData p_Data)
        {
            p_Data.InstanceGuid = m_InstanceGuid;
            p_Data.PrefabGuid = m_PrefabGuid;
            p_Data.Type = m_ISaveableType;
            p_Data.Name = gameObject.name;
        }

        public void SetBasicPersistedDataValues(in ABS_PersistedData p_Data)
        {
            m_ISaveableType = p_Data.Type;
            m_InstanceGuid = p_Data.InstanceGuid;
            m_PrefabGuid = p_Data.PrefabGuid;
            gameObject.name = p_Data.Name;
        }

        public bool CompareBasicPersistedDataValues(in ABS_PersistedData p_Data)
        {
            return m_ISaveableType == p_Data.Type
                   && string.Compare(m_InstanceGuid, p_Data.InstanceGuid) == 0
                   && string.Compare(m_PrefabGuid, p_Data.PrefabGuid) == 0
                   && string.Compare(gameObject.name, p_Data.Name) == 0;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstarct functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public abstract string ToJSON(in bool p_PrettyPrint);
    }
}
//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [System.Serializable]
    public abstract class ABS_PersistedData
    {
        [SerializeField] private ABS_SaveableMonobehaviour.DataType m_Type = ABS_SaveableMonobehaviour.DataType.Unkown;
        [SerializeField] private string m_Name = string.Empty;
        [SerializeField] private string m_InstanceGuid = string.Empty;
        [SerializeField] private string m_PrefabGuid = string.Empty;

        public ABS_SaveableMonobehaviour.DataType Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public string InstanceGuid
        {
            get { return m_InstanceGuid; }
            set { m_InstanceGuid = value; }
        }
        
        public string PrefabGuid
        {
            get { return m_PrefabGuid; }
            set { m_PrefabGuid = value; }
        }

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public abstract string ToJSON (in bool p_PrettyPrint);
    }
}

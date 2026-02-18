//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_MonoBehaviourBase : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] protected string m_PrefabGuid = string.Empty;

        [SerializeField] protected bool m_FixedInstanceGuid = false;
        [SerializeField] protected string m_InstanceGuid = string.Empty;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Setters/Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

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

        public bool FixedInstanceGuid
        {
            get { return m_FixedInstanceGuid; }
            set { m_FixedInstanceGuid = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Setters/Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region MonoBehaviour Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Awake()
        {
            if (!m_FixedInstanceGuid || string.IsNullOrEmpty(m_InstanceGuid))
            {
                GenerateNewInstanceGuid();
            }

            AwakeImpl();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // MonoBehaviour Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Own Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void GenerateNewPrefabGuid()
        {
            m_PrefabGuid = REST_IDManager.CreateGuid();
        }

        public void GenerateNewInstanceGuid()
        {
            m_InstanceGuid = REST_IDManager.CreateGuid();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion //Own Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region  Abstract function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void AwakeImpl();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion //Abstract function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}
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
    public class ABS_ObjectPool : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool m_Initalized = false;

        private int m_MaximumElementCapacity = 10000;
        private int m_TargetElementCapacity = 500;

        private bool m_Buffering = false;
        private float m_BufferCreateTimerDefault = 0.5f;
        private float m_BufferCreateTimer = 0.0f;

        [SerializeField] private ABS_BuildingElement m_BuildingElement = null;
        private List<ABS_BuildingElement> m_ActiveElementPool = null;
        private List<ABS_BuildingElement> m_InactiveElementPool = null;

        private float m_ClearPoolTimer = 300f;
        private float m_Timer = 0.0f;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public int MaximumElementCapacity { get { return m_MaximumElementCapacity; } }
        public int TargetElementCapacity { get { return m_TargetElementCapacity; } }
        public int CurrentElementCount { get { return m_ActiveElementPool.Count + m_InactiveElementPool.Count; } }
        public int ActiveElementCount { get { return m_ActiveElementPool.Count; } }
        public int InactiveElementCount { get { return m_InactiveElementPool.Count; } }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Init(
            in ABS_BuildingElement p_BuildingElement,
            in int p_MaximumElementCapacity,
            in int p_TargetElementCapacity,
            in bool p_Buffering,
            in float p_BufferCreateTimerDefault,
            in float p_ClearPoolTimer)
        {
            if (m_Initalized)
            {
                return;
            }

            m_ActiveElementPool = new List<ABS_BuildingElement>();
            m_InactiveElementPool = new List<ABS_BuildingElement>();

            m_BuildingElement = p_BuildingElement;

            m_MaximumElementCapacity = p_MaximumElementCapacity;
            m_TargetElementCapacity = p_TargetElementCapacity;

            m_Buffering = p_Buffering;
            m_BufferCreateTimerDefault = p_BufferCreateTimerDefault;

            m_ClearPoolTimer = p_ClearPoolTimer;

            m_Initalized = true;
        }

        public ABS_BuildingElement Get ()
        {
            if (!m_Initalized)
            {
                REST_Logging.Warrning("ABS_ObjectPool", "The pool is not initalized!");
                return null;
            }

            ResetTimer();

            if (m_InactiveElementPool.Count > 0)
            {
                ABS_BuildingElement element = m_InactiveElementPool[0];
                m_InactiveElementPool.RemoveAt(0);
                m_ActiveElementPool.Add(element);
                element.gameObject.SetActive(true);
                return element;
            }
            else if (CurrentElementCount < m_MaximumElementCapacity)
            {
                ABS_BuildingElement element = CreatePooledObject();
                m_ActiveElementPool.Add(element);
                return element;
            }
            else
            {
                REST_Logging.Warrning("ABS_ObjectPool", "The pool has reached it's capacity!");
                return null; 
            }
        }

        public bool GiveBack(ABS_BuildingElement p_Element)
        {
            if (!m_Initalized)
            {
                REST_Logging.Warrning("ABS_ObjectPool", "The pool is not initalized!");
                return false;
            }

            ResetTimer();

            if (!m_ActiveElementPool.Remove(p_Element))
            {
                REST_Logging.Warrning("ABS_ObjectPool", "Not owned element has given as parameter!");
                return false;
            }
            else
            {
                m_InactiveElementPool.Add(p_Element);
                p_Element.transform.parent = null;
                p_Element.gameObject.SetActive(false);
                return true;
            }
        }

        public bool Release(ABS_BuildingElement p_Element)
        {
            if (!m_Initalized)
            {
                REST_Logging.Warrning("ABS_ObjectPool", "The pool is not initalized!");
                return false;
            }

            ResetTimer();

            if (!m_ActiveElementPool.Remove(p_Element))
            {
                REST_Logging.Error("ABS_ObjectPool", "Not owned element has given as parameter!");
                return false;
            }
            else
            {
                return true;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void FixedUpdate()
        {
            if (!m_Initalized)
            {
                return;
            }

            if (!CheckDestroyTimer())
            {
                //return if the object has been destroyed
                return;
            }

            if (m_Buffering)
            {
                CheckBuffering();
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void DestroyPoolObject(ABS_BuildingElement p_Element)
        {
            if (p_Element != null)
            {
                Destroy(p_Element.gameObject);
            }
        }

        private ABS_BuildingElement CreatePooledObject()
        {
            ABS_BuildingElement element = Instantiate(m_BuildingElement);
            return element;
        }

        private void Clear()
        {
            foreach (ABS_BuildingElement element in m_InactiveElementPool)
            {
                DestroyPoolObject(element);
            }
            m_ActiveElementPool.Clear();
            m_InactiveElementPool.Clear();
        }

        private void ResetTimer()
        {
            m_Timer = 0.0f;
        }

        private bool CheckDestroyTimer()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer >= m_ClearPoolTimer)
            {
                REST_Logging.Debug(
                    "ABS_ObjectPool",
                    "FixedUpdate",
                    $"Pool has been destroyed for {m_BuildingElement.name} | {m_BuildingElement.PrefabGuid}");
                Clear();
                Destroy(this);
                return false;
            }

            return true;
        }

        private void CheckBuffering()
        {
            m_BufferCreateTimer += Time.deltaTime;
            if (m_BufferCreateTimer > m_BufferCreateTimerDefault)
            {
                m_BufferCreateTimer -= m_BufferCreateTimerDefault;
                if (CurrentElementCount < m_MaximumElementCapacity
                    && InactiveElementCount < m_TargetElementCapacity)
                {
                    ABS_BuildingElement element = CreatePooledObject();
                    m_InactiveElementPool.Add(element);
                    element.gameObject.SetActive(false);
                }
            }
        }
    }
}
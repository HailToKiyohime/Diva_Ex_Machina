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
    public class ABS_ObjectPoolManager : ABS_ObjectPoolBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private int m_MaximumElementCapacity = 10000;
        [SerializeField] private int m_TargetElementCapacity = 100;

        [SerializeField] private bool m_Buffering = false;
        [SerializeField] private float m_BufferCreateTimerDefault = 0.5f;

        [SerializeField] private float m_ClearPoolTimer = 300f;

        private Dictionary<string, ABS_ObjectPool> m_Pools = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_ObjectPoolManager () : base()
        {
            m_Pools = new Dictionary<string, ABS_ObjectPool>();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override ABS_BuildingElement Get (ABS_BuildingElement p_Element)
        {
            ABS_ObjectPool pool = GetPool(p_Element);

            return pool.Get();
        }

        public override void GiveBack(ABS_BuildingElement p_Element)
        {
            if (p_Element == null)
            {
                REST_Logging.Error("ABS_ObjectPoolManager", "Null parameter");
                return;
            }

            ABS_ObjectPool pool = GetPool(p_Element);
            pool.GiveBack(p_Element);
        }

        public override void Release(ABS_BuildingElement p_Element)
        {
            if (p_Element == null)
            {
                REST_Logging.Error("ABS_ObjectPoolManager", "Null parameter");
                return;
            }

            ABS_ObjectPool pool = GetPool(p_Element);
            pool.Release(p_Element);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_ObjectPool GetPool (ABS_BuildingElement p_Element)
        {
            ABS_ObjectPool pool = null;
            if (m_Pools.TryGetValue(p_Element.PrefabGuid, out pool) && pool != null)
            {
                return pool;
            }
            else
            {
                REST_Logging.Debug("ABS_ObjectPoolManager", $"New pool created for {p_Element.name} | {p_Element.PrefabGuid}");
                ABS_ObjectPool newPool = this.gameObject.AddComponent<ABS_ObjectPool>();
                newPool.Init(
                    p_Element,
                    m_MaximumElementCapacity,
                    m_TargetElementCapacity,
                    m_Buffering,
                    m_BufferCreateTimerDefault,
                    m_ClearPoolTimer);
                m_Pools[p_Element.PrefabGuid] = newPool;
                return newPool;
            }
        }
    }
}


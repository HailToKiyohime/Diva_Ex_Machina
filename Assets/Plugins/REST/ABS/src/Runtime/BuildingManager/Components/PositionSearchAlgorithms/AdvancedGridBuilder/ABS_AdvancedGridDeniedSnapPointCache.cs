
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

    public class ABS_AdvancedGridDeniedSnapPointCache
    {
        private Dictionary<Vector3, HashSet<ABS_BuildingElement>> m_ValidationCache = null;
        protected REST_Vector3EqualityComparer m_Vector3Comparer = null;

        public ABS_AdvancedGridDeniedSnapPointCache(REST_Vector3EqualityComparer p_Vector3Comparer)
        {
            m_ValidationCache = new Dictionary<Vector3, HashSet<ABS_BuildingElement>>(p_Vector3Comparer);
            m_Vector3Comparer = p_Vector3Comparer;
        }

        public bool IsPositionDenied(in Vector3 p_LocalPosition)
        {
            HashSet<ABS_BuildingElement> blockers = null;
            if (m_ValidationCache.TryGetValue(p_LocalPosition, out blockers) 
                && blockers != null
                && blockers.Count != 0)
            {
                return true;
            }

            return false;
        }


        public void RemoveCacheData(in ABS_BuildingElement p_Blocker, in Vector3 p_LocalPosition)
        {
            HashSet<ABS_BuildingElement> blockers = null;
            if (m_ValidationCache.TryGetValue(p_LocalPosition, out blockers) && blockers != null)
            {
                blockers.Remove(p_Blocker);
                if (blockers.Count == 0)
                {
                    m_ValidationCache.Remove(p_LocalPosition);
                }
            }
        }

        public void SaveResultToCache(in ABS_BuildingElement p_Blocker, in Vector3 p_LocalPosition)
        {
            HashSet<ABS_BuildingElement> blockers = null;
            if (!m_ValidationCache.TryGetValue(p_LocalPosition, out blockers) || blockers != null)
            {
                blockers = new HashSet<ABS_BuildingElement>();
                m_ValidationCache[p_LocalPosition] = blockers;
            }

            blockers.Add(p_Blocker);
        }
    }
}
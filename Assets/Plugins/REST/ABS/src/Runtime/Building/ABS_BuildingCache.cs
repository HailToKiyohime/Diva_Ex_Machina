
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
    public class ABS_BuildingCache
    {
        private REST_Vector3EqualityComparer m_Vector3Comparer = null;
        private Dictionary<string, Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>>> m_ValidationCache = null;

        public ABS_BuildingCache(REST_Vector3EqualityComparer p_Vector3Comparer)
        {
            m_Vector3Comparer = p_Vector3Comparer;
            m_ValidationCache = new Dictionary<string, Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>>>();
        }

        public ABS_PositionValidationData GetCacheData(in Vector3 p_LocalPosition, 
                                                         in Vector3 p_LocalRotation,         
                                                         string p_PrefabGuid)
        {
            Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>> cachePrefabData = null;
            if (!m_ValidationCache.TryGetValue(p_PrefabGuid, out cachePrefabData) || cachePrefabData == null)
            {
                return null;
            }

            Dictionary<Vector3, ABS_PositionValidationData> cachePositionBasedData = null;
            if (!cachePrefabData.TryGetValue(p_LocalPosition, out cachePositionBasedData) || cachePositionBasedData == null)
            {
                return null;
            }


            ABS_PositionValidationData cacheRotationBasedData = null;
            if (!cachePositionBasedData.TryGetValue(p_LocalRotation, out cacheRotationBasedData) || cacheRotationBasedData == null)
            {
                return null;
            }

            return cacheRotationBasedData;
        }


        public void RemoveCacheData(in Vector3 p_LocalPosition,
                                    in Vector3 p_LocalRotation,
                                    string p_PrefabGuid)
        {
            Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>> cachePrefabData = null;
            if (!m_ValidationCache.TryGetValue(p_PrefabGuid, out cachePrefabData) || cachePrefabData == null)
            {
                return;
            }

            Dictionary<Vector3, ABS_PositionValidationData> cachePositionBasedData = null;
            if (!cachePrefabData.TryGetValue(p_LocalPosition, out cachePositionBasedData) || cachePositionBasedData == null)
            {
                return;
            }

            cachePositionBasedData.Remove(p_LocalRotation);
            if (cachePositionBasedData.Count == 0)
            {
                cachePrefabData.Remove(p_LocalPosition);
                if (cachePrefabData.Count == 0)
                {
                    m_ValidationCache.Remove(p_PrefabGuid);
                }
            }
        }

        public void SaveResultToCache(in Vector3 p_LocalPosition,
                                      in Vector3 p_LocalRotation,
                                      in string p_PrefabGuid, 
                                      in ABS_PositionValidationData p_ResultDataForSave)
        {
            p_ResultDataForSave.m_Result.ParentCachedResult = true;

            Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>> cachePrefabData = null;
            if (!m_ValidationCache.TryGetValue(p_PrefabGuid, out cachePrefabData) || cachePrefabData == null)
            {
                Dictionary<Vector3, ABS_PositionValidationData> rotationBasedDict = new Dictionary<Vector3, ABS_PositionValidationData>(m_Vector3Comparer);
                cachePrefabData = new Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>>(m_Vector3Comparer);

                m_ValidationCache[p_PrefabGuid] = cachePrefabData;
                cachePrefabData[p_LocalPosition] = rotationBasedDict;
                rotationBasedDict[p_LocalRotation] = p_ResultDataForSave;
                return;
            }

            Dictionary<Vector3, ABS_PositionValidationData> cachePositionBasedData = null;
            if (!cachePrefabData.TryGetValue(p_LocalPosition, out cachePositionBasedData) || cachePositionBasedData == null)
            {
                cachePositionBasedData = new Dictionary<Vector3, ABS_PositionValidationData>(m_Vector3Comparer);

                cachePrefabData[p_LocalPosition] = cachePositionBasedData;
                cachePositionBasedData[p_LocalRotation] = p_ResultDataForSave;
                return;
            }
        }

        public int CheckCacheSize()
        {
            int count = 0;
            foreach ((string guid, Dictionary<Vector3, Dictionary<Vector3, ABS_PositionValidationData>> item) in m_ValidationCache)
            foreach ((Vector3 pos, Dictionary<Vector3, ABS_PositionValidationData> item2) in item)
            {
                count += item2.Count;
            }
            REST_Logging.Debug("ABS_Building", $"Cache size : {count}");
            return count;
        }

        public void Clear()
        {
            m_ValidationCache.Clear();
        }
    }
}
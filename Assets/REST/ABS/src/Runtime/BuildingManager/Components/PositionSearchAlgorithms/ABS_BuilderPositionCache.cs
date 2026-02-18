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
    public class ABS_BuilderCacheResultData
    {
        public ABS_BuilderCacheResultData(float p_Distance, ABS_PositionSearchResult p_Result)
        {
            m_Distance = p_Distance;
            m_Result = p_Result;
            p_Result.ValidationResult.m_Result.BuilderCachedResult = true;
        }

        public float m_Distance = 0f;
        public ABS_PositionSearchResult m_Result = null;
    }

    internal class ABS_BuilderCacheRotationBasedData
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private REST_Vector3EqualityComparer m_VectorCompareator = new REST_Vector3EqualityComparer();

        //NearestSnapPointPosition : ABS_BuilderCacheResultData
        private Dictionary<Vector3, ABS_BuilderCacheResultData> m_SuccessCache = null;
        //FinalPosition : ABS_BuilderCacheResultData
        private Dictionary<Vector3, ABS_BuilderCacheResultData> m_PositionCache = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuilderCacheRotationBasedData(REST_Vector3EqualityComparer p_VectorCompareator)
        {
            m_VectorCompareator = p_VectorCompareator;
            m_SuccessCache = new Dictionary<Vector3, ABS_BuilderCacheResultData>(m_VectorCompareator);
            m_PositionCache = new Dictionary<Vector3, ABS_BuilderCacheResultData>(m_VectorCompareator);
        }

        public void AddData(in Vector3 p_NearestSnapPointPosition,
                             in float p_Distance,
                             in ABS_PositionSearchResult p_Result)
        {
            ABS_BuilderCacheResultData data = new ABS_BuilderCacheResultData(p_Distance, p_Result);
            if (p_Result.ValidationResult.IsSuccessFull())
            {
                m_SuccessCache[p_NearestSnapPointPosition] = data;
            }

            m_PositionCache[p_Result.WorldPosition] = data;
        }

        public ABS_BuilderCacheResultData FindSuccessResultCache(in Vector3 p_NearestSnapPoint)
        {
            ABS_BuilderCacheResultData data = null;
            m_SuccessCache.TryGetValue(p_NearestSnapPoint, out data);
            return data;
        }

        public ABS_BuilderCacheResultData FindInCache(in Vector3 p_Position)
        {
            ABS_BuilderCacheResultData data = null;
            m_PositionCache.TryGetValue(p_Position, out data);
            return data;
        }
    }

    internal class ABS_BuilderCacheBuildingData
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private REST_Vector3EqualityComparer m_VectorCompareator = new REST_Vector3EqualityComparer();

        private Dictionary<Vector3, ABS_BuilderCacheRotationBasedData> m_Cache = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuilderCacheBuildingData(REST_Vector3EqualityComparer p_VectorCompareator)
        {
            m_VectorCompareator = p_VectorCompareator;
            m_Cache = new Dictionary<Vector3, ABS_BuilderCacheRotationBasedData>();
        }

        public void AddData(in Vector3 p_NearestSnapPointPosition,
                            in Vector3 p_PlayerRotation,
                            in float p_Distance,
                            in ABS_PositionSearchResult p_Result)
        {
            ABS_BuilderCacheRotationBasedData data = null;
            if (!m_Cache.TryGetValue(p_PlayerRotation, out data))
            {
                data = new ABS_BuilderCacheRotationBasedData(m_VectorCompareator);
                m_Cache[p_PlayerRotation] = data;
            }

            data.AddData(p_NearestSnapPointPosition, p_Distance, p_Result);
        }

        public ABS_BuilderCacheResultData FindSuccessResultCache(in Vector3 p_NearestSnapPoint, in Vector3 p_PlayerRotation)
        {
            ABS_BuilderCacheRotationBasedData data = null;
            if (m_Cache.TryGetValue(p_PlayerRotation, out data))
            {
                return data.FindSuccessResultCache(p_NearestSnapPoint);
            }
            return null;
        }

        public ABS_BuilderCacheResultData FindInCache(in Vector3 p_Position, in Vector3 p_PlayerRotation)
        {
            ABS_BuilderCacheRotationBasedData data = null;
            if (m_Cache.TryGetValue(p_PlayerRotation, out data))
            {
                return data.FindInCache(p_Position);
            }
            return null;
        }
    }

    public class ABS_BuilderPositionCache
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private Dictionary<ABS_Building, ABS_BuilderCacheBuildingData> m_Cache = null;
        protected REST_Vector3EqualityComparer m_VectorCompareator = new REST_Vector3EqualityComparer();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuilderPositionCache ()
        {
            m_Cache = new Dictionary<ABS_Building, ABS_BuilderCacheBuildingData>();
        }


        public void AddCache(in Vector3 p_NearestSnapPoint,
                             in Vector3 p_PlayerRotation,
                             float p_Distance,
                             in ABS_PositionSearchResult p_Result)
        {
            if (p_Result.TargetBuilding == null)
            {
                return;
            }

            ABS_BuilderCacheBuildingData data = null;
            if (!m_Cache.TryGetValue(p_Result.TargetBuilding, out data))
            {
                data = new ABS_BuilderCacheBuildingData(m_VectorCompareator);
                m_Cache[p_Result.TargetBuilding] = data;
            }

            data.AddData(p_NearestSnapPoint, p_PlayerRotation, p_Distance, p_Result);
        }

        public ABS_BuilderCacheResultData FindSuccessResultCache(in ABS_Building p_Building, in Vector3 p_NearestSnapPoint, in Vector3 p_PlayerRotation)
        {
            if (p_Building == null)
            {
                return null;
            }

            ABS_BuilderCacheBuildingData data = null;
            if (m_Cache.TryGetValue(p_Building, out data))
            {
                return data.FindSuccessResultCache(p_NearestSnapPoint, p_PlayerRotation);
            }

            return null;
        }

        public ABS_BuilderCacheResultData FindInCache(in ABS_Building p_Building, in Vector3 p_Position, in Vector3 p_PlayerRotation)
        {
            if (p_Building == null)
            {
                return null;
            }

            ABS_BuilderCacheBuildingData data = null;
            if (m_Cache.TryGetValue(p_Building, out data))
            {
                return data.FindInCache(p_Position, p_PlayerRotation);
            }

            return null;
        }

        public void ClearCache()
        {
            m_Cache.Clear();
        }

    }
}
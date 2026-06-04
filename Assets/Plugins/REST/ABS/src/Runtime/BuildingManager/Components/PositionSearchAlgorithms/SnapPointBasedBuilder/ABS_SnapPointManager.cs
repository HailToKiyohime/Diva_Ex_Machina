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
    public class ABS_SnapPointManager
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private class ValidationResultData
        {
            public List<(Vector3, Vector3)> m_NotValidatedSnapPoints = new List<(Vector3, Vector3)>();
            public List<(Vector3, Vector3)> m_UnderValidationSnapPoints = new List<(Vector3, Vector3)>();
            public List<(Vector3, Vector3)> m_NotValidSnapPoints = new List<(Vector3, Vector3)>();
            public List<(Vector3, Vector3)> m_ValidSnapPoints = new List<(Vector3, Vector3)>();
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        private REST_Vector3EqualityComparer m_Vector3EqualityComparer = new REST_Vector3EqualityComparer();

        private ABS_SnapRelationshipList m_SnapPointList = null;

        private Dictionary<(string, string), List<(Vector3, Vector3)>> m_SnapRelationshipsCache =
            new Dictionary<(string, string), List<(Vector3, Vector3)>>();

        private Dictionary<(ABS_SnapPointBasedBuilding, ABS_BuildingElement), ValidationResultData> m_SnapRelationshipsBuildingCache =
            new Dictionary<(ABS_SnapPointBasedBuilding, ABS_BuildingElement), ValidationResultData>();

        private int m_MaxValidator = 4;
        private int m_CurrentValidator = 0;

        private float m_RigidbodyActivationArea = 1.5f;
        [Range(0.0f, 1.0f)]private float m_ValidationTolerance = 0.9f;
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_SnapPointManager(ABS_SnapRelationshipList p_SnapPointList)
        {
            m_SnapPointList = p_SnapPointList;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  public functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public List<(Vector3, Vector3)> GetSnapPositions(ABS_BuildingElement p_Active, ABS_BuildingElement p_Target)
        {
            List<(Vector3, Vector3)> result = null;
            bool contains = m_SnapRelationshipsCache.TryGetValue((p_Active.PrefabGuid, p_Target.PrefabGuid), out result);
            if (contains && result != null)
            {
                return result;
            }

            return FillCache(p_Active, p_Target);
        }

        public List<(Vector3, Vector3)> ValidateSnapPoints(
            ABS_BuildingElement p_Active,
            ABS_BuildingElement p_Target,
            List<(Vector3, Vector3)> p_SnapPoints,
            ABS_SnapPointBasedBuilding p_Building)
        {
            List<(Vector3, Vector3)> validationSnapPoints = new List<(Vector3, Vector3)>();

            ValidationResultData cachedLocalSnapPoints = GetCachedBuildingSnapPositions(p_Building, p_Active);
            Transform targetTransform = p_Target.transform;
            Transform targetParentTransform = p_Target.ParentBuilding.transform;
            foreach ((Vector3 snapPointPosition, Vector3 snapPointRotation) in p_SnapPoints)
            {
                Vector3 snapPointLocalPosition = targetParentTransform.InverseTransformPoint(targetTransform.TransformPoint(snapPointPosition));
                Vector3 snapPointLocalRotation = targetTransform.localEulerAngles + snapPointRotation;
                if (FindSnapPoint(cachedLocalSnapPoints.m_ValidSnapPoints, snapPointLocalPosition, snapPointLocalRotation))
                {
                    validationSnapPoints.Add((snapPointLocalPosition, snapPointLocalRotation));
                }
                else if (!FindSnapPoint(cachedLocalSnapPoints.m_NotValidSnapPoints, snapPointLocalPosition, snapPointLocalRotation)
                    && !FindSnapPoint(cachedLocalSnapPoints.m_NotValidatedSnapPoints, snapPointLocalPosition, snapPointLocalRotation)
                    && !FindSnapPoint(cachedLocalSnapPoints.m_UnderValidationSnapPoints, snapPointLocalPosition, snapPointLocalRotation))
                {
                    cachedLocalSnapPoints.m_NotValidatedSnapPoints.Add((snapPointLocalPosition, snapPointLocalRotation));
                }
            }

            CheckSnapPointsForValidation();
            return validationSnapPoints;
        }

        public void Report(
            ABS_SnapPointBasedBuilding p_Building, 
            ABS_BuildingElement p_Element, 
            Vector3 p_LocalPosition, 
            Vector3 p_LocalRotation,
            bool p_Result)
        {
            --m_CurrentValidator;
            ValidationResultData data = GetCachedBuildingSnapPositions(p_Building, p_Element);
            data.m_NotValidatedSnapPoints.Remove((p_LocalPosition, p_LocalRotation));
            if (p_Result)
            {
                data.m_NotValidSnapPoints.Add((p_LocalPosition, p_LocalRotation));
            }
            else
            {
                data.m_ValidSnapPoints.Add((p_LocalPosition, p_LocalRotation));
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  private functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool FindSnapPoint (List<(Vector3, Vector3)> p_SnapPointList, Vector3 p_Position, Vector3 p_Rotation)
        {
            foreach ((Vector3 pos, Vector3 rot) in p_SnapPointList)
            {
                if (m_Vector3EqualityComparer.Equals(p_Position, pos)
                    && m_Vector3EqualityComparer.Equals(p_Rotation, rot))
                {
                    return true;
                }
            }
            return false;
        }

        private ValidationResultData GetCachedBuildingSnapPositions(ABS_SnapPointBasedBuilding p_Building, ABS_BuildingElement p_Element)
        {
            (ABS_SnapPointBasedBuilding, ABS_BuildingElement) key = (p_Building, p_Element);
            ValidationResultData result = null;
            bool contains = m_SnapRelationshipsBuildingCache.TryGetValue(key, out result);
            if (contains && result != null)
            {
                return result;
            }

            result = new ValidationResultData();
            m_SnapRelationshipsBuildingCache[key] = result;
            return result;
        }

        private List<(Vector3, Vector3)> FillCache(ABS_BuildingElement p_Active, ABS_BuildingElement p_Target)
        {
            List<ABS_SnapRelationship.SnapPosition> snapPositionsActive = new List<ABS_SnapRelationship.SnapPosition>();
            foreach (ABS_SnapRelationship relation in m_SnapPointList.SnapRelationships)
            {
                if ((relation.ElementA.PrefabGuid == p_Active.PrefabGuid && relation.ElementB.PrefabGuid == p_Target.PrefabGuid)
                    || (relation.ElementA.PrefabGuid == p_Target.PrefabGuid && relation.ElementB.PrefabGuid == p_Active.PrefabGuid))
                {
                    foreach (ABS_SnapRelationship.SnapPosition snapPosition in relation.Positions)
                    {
                        if ((relation.ElementA.PrefabGuid == p_Active.PrefabGuid && snapPosition.m_RelationType == ABS_SnapRelationship.RelationType.AToB)
                             || (relation.ElementB.PrefabGuid == p_Active.PrefabGuid && snapPosition.m_RelationType == ABS_SnapRelationship.RelationType.BToA))
                        {
                            snapPositionsActive.Add(snapPosition);
                        }
                    }
                    break;
                }
            }

            List<(Vector3, Vector3)> result = new List<(Vector3, Vector3)>();
            foreach (ABS_SnapRelationship.SnapPosition snapPosition in snapPositionsActive)
            {
                result.Add((snapPosition.m_Position, snapPosition.m_Rotation));
            }

            m_SnapRelationshipsCache[(p_Active.PrefabGuid, p_Target.PrefabGuid)] = result;
            return result;
        }

        private void CheckSnapPointsForValidation()
        {
            foreach (((ABS_SnapPointBasedBuilding building, ABS_BuildingElement element), ValidationResultData data) in m_SnapRelationshipsBuildingCache)
            {
                List<(Vector3 pos, Vector3 rot)> tmpList = new List<(Vector3 pos, Vector3 rot)> ();
                foreach ((Vector3 pos, Vector3 rot) in data.m_NotValidatedSnapPoints)
                {
                    if (m_CurrentValidator < m_MaxValidator)
                    {
                        CreateValidator(building, element, pos, rot);
                        tmpList.Add((pos, rot));
                    }
                }

                foreach ((Vector3 pos, Vector3 rot) in tmpList)
                {
                    data.m_NotValidatedSnapPoints.Remove((pos, rot));
                    data.m_UnderValidationSnapPoints.Add((pos, rot));
                }
            }
        }

        private void CreateValidator(ABS_SnapPointBasedBuilding p_Building, ABS_BuildingElement p_Element, Vector3 p_LocalPosition, Vector3 p_LocalEulerAngles)
        {
            ++m_CurrentValidator;
            LayerMask layer = p_Element.LayerCollection.LayerOfBuildingElement;

            GameObject validatorObj = new GameObject();
            validatorObj.layer = layer;

            Transform validatorTransf = validatorObj.transform;
            validatorTransf.parent = p_Building.transform;
            validatorTransf.localPosition = p_LocalPosition;
            validatorTransf.localEulerAngles = p_LocalEulerAngles;
            validatorTransf.localScale = Vector3.one * m_ValidationTolerance;

            MeshCollider collider = validatorObj.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.isTrigger = true;
            collider.sharedMesh = p_Element.Mesh;
            collider.includeLayers = layer;

            ABS_SnapPointValidator validator = validatorObj.AddComponent<ABS_SnapPointValidator>();
            validator.Init(this, m_RigidbodyActivationArea, layer, p_Building, p_Element, p_LocalPosition, p_LocalEulerAngles);
        }


    }
}

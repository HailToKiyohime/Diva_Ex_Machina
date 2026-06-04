//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewSnapRelationshipList", menuName = "AdvancedBuildingSystem/BuildingElement/New SnapRelationship List")]
    public class ABS_SnapRelationshipList : ScriptableObject
    {
        [SerializeField] private List<ABS_SnapRelationship> m_SnapRelationships = new List<ABS_SnapRelationship>();

        public List<ABS_SnapRelationship> SnapRelationships { get { return m_SnapRelationships; } }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            foreach (ABS_SnapRelationship snapRelation in m_SnapRelationships)
            {
                if (snapRelation != null)
                {
                    snapRelation.OnDrawGizmos();
                }
            }
        }
#endif
    }
}
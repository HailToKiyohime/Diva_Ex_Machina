//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    [CreateAssetMenu(fileName = "NewBuildingAreaRuleset", menuName = "AdvancedBuildingSystem/BuildingArea/New Ruleset")]
    public class ABS_BuildingAreaRuleset : ScriptableObject, ABS_IEntityListHolder
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private EntityListBase<ABS_BuildingAreaRule> m_Rules = new EntityListBase<ABS_BuildingAreaRule>();

        public ABS_BuildingAreaRule.PermissionType m_BasePermissionType = ABS_BuildingAreaRule.PermissionType.Allow;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public List<ABS_BuildingAreaRule> Rules { get { return m_Rules.EntityList; } }

        public bool Allow (ABS_BuildingElement p_ElementToCheck)
        {
            foreach (ABS_BuildingAreaRule rule in m_Rules.EntityList)
            {
                ABS_BuildingAreaRule.RuleCheckResult result = rule.Allow(p_ElementToCheck);
                switch (result)
                {
                    case ABS_BuildingAreaRule.RuleCheckResult.NotRelated:
                        break;
                    case ABS_BuildingAreaRule.RuleCheckResult.Allow:
                        return true;
                    case ABS_BuildingAreaRule.RuleCheckResult.Deny:
                        return false;
                }
            }
            return m_BasePermissionType == ABS_BuildingAreaRule.PermissionType.Allow;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntityList Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntityList EntityList { get { return m_Rules; } }
    }
}
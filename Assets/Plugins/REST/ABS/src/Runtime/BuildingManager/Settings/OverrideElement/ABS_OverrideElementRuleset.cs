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

    [CreateAssetMenu(fileName = "NewOverrideElementRuleset", menuName = "AdvancedBuildingSystem/BuildingManager Settings/New OverrideElement Ruleset")]
    public class ABS_OverrideElementRuleset : ScriptableObject, ABS_IEntityListHolder
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private EntityListBase<ABS_OverrideElementRule> m_Rules = new EntityListBase<ABS_OverrideElementRule>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public List<ABS_OverrideElementRule> Rules { get { return m_Rules.EntityList; } }

        public bool CanOverride (ABS_BuildingElement p_TargetElement, ABS_BuildingElement p_ElementForBuild)
        {
            if (m_Rules == null)
            {
                REST_Logging.Warrning("ABS_OverrideElementRuleset", "The ABS_OverrideElementRuleset is null!");
                return false;
            }

            foreach (ABS_OverrideElementRule rule in m_Rules.EntityList)
            {
                if (rule.CanOverride(p_TargetElement, p_ElementForBuild))
                {
                    return true;
                }
            }
            return false;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntityList Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntityList EntityList { get { return m_Rules; } }
    }
}


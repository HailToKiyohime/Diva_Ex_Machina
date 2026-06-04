//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    [CreateAssetMenu(fileName = "NewAdvancedGridSnapPointRuleSet", menuName = "AdvancedBuildingSystem/BuildingElement/New AdvancedGridSnapPointRuleSet")]
    public class ABS_AdvancedGridSnapPointRuleSet : ABS_DrawableScriptableObject, ABS_IEntityListHolder
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private ABS_AdvancedGridType m_Type = ABS_AdvancedGridType.Floor;
        [SerializeField] private EntityListBase<ABS_AdvancedGridSnapPointRule> m_Rules = new EntityListBase<ABS_AdvancedGridSnapPointRule>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_AdvancedGridSnapPointRuleSet () : base()
        {
            foreach (ABS_AdvancedGridType type in Enum.GetValues(typeof(ABS_AdvancedGridType)))
            {
                ABS_AdvancedGridSnapPointRule rule = m_Rules.CreateEntity() as ABS_AdvancedGridSnapPointRule;
                rule.Type = type;
                rule.Name = type.ToString();
                rule.SetupByTargetType(m_Type);
            }
        }

        public List<ABS_AdvancedGridSnapPointRule> Rules { get { return m_Rules.EntityList; } }

        public ABS_AdvancedGridSnapPointRule GetRule (ABS_AdvancedGridType p_Type)
        {
            return m_Rules.GetEntity((int)p_Type) as ABS_AdvancedGridSnapPointRule;
        }

        public List<ABS_AdvancedGridSnapPointRule.SnapPoint> GetSnapPoints (ABS_AdvancedGridType p_Type)
        {
            return GetRule(p_Type).SnapPoints;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntityList Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntityList EntityList { get { return m_Rules; } }

        public ABS_AdvancedGridType Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }
    }
}

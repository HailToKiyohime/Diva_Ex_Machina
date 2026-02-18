//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;

//  Dependencies: Unity

//  Dependencies: REST
using REST.Utils;
using UnityEngine;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [Serializable]
    public class ABS_OverrideElementRule : ABS_IEntity
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum RelationType
        {
            OneToSet,
            SetToOne,
            BothWay,
            Set
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private string m_Name = string.Empty;
        [SerializeField] private RelationType m_Type = RelationType.OneToSet;
        [SerializeField] private ABS_BuildingElement m_OverrideTarget = null;
        [SerializeField] private List<ABS_BuildingElement> m_BuildingElementsForChange = new List<ABS_BuildingElement>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public RelationType Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public ABS_BuildingElement OverrideTarget
        {
            get { return m_OverrideTarget; }
            set { m_OverrideTarget = value; }
        }

        public List<ABS_BuildingElement> BuildingElementsForChange
        {
            get { return m_BuildingElementsForChange; }
            set { m_BuildingElementsForChange = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_OverrideElementRule()
        {
            m_Name = "Rule";
        }

        public ABS_OverrideElementRule(string p_Name)
        {
            m_Name = p_Name;
        }

        public bool CanOverride(ABS_BuildingElement p_TargetElement, ABS_BuildingElement p_ElementForBuild)
        {
            switch (m_Type)
            {
                case RelationType.OneToSet: return CanOverride_OneToSet(p_TargetElement, p_ElementForBuild);
                case RelationType.SetToOne: return CanOverride_SetToOne(p_TargetElement, p_ElementForBuild);
                case RelationType.BothWay: return CanOverride_BothWay(p_TargetElement, p_ElementForBuild);
                case RelationType.Set: return CanOverride_Set(p_TargetElement, p_ElementForBuild);
            }

            return false;
        }

        private bool CanOverride_OneToSet(ABS_BuildingElement p_TargetElement, ABS_BuildingElement p_ElementForBuild)
        {
            if (m_OverrideTarget.PrefabGuid == p_ElementForBuild.PrefabGuid)
            {
                foreach (ABS_BuildingElement element in m_BuildingElementsForChange)
                {
                    if (element == null)
                    {
                        REST_Logging.Warrning("ABS_OverrideElementRule", $"Null element in the set. Rule: {Name}");
                        continue;
                    }
                    else if (element.PrefabGuid == p_TargetElement.PrefabGuid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanOverride_SetToOne(ABS_BuildingElement p_TargetElement, ABS_BuildingElement p_ElementForBuild)
        {
            if (m_OverrideTarget.PrefabGuid == p_TargetElement.PrefabGuid)
            {
                foreach (ABS_BuildingElement element in m_BuildingElementsForChange)
                {
                    if (element == null)
                    {
                        REST_Logging.Warrning("ABS_OverrideElementRule", $"Null element in the set. Rule: {Name}");
                        continue;
                    }
                    else if (element.PrefabGuid == p_ElementForBuild.PrefabGuid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanOverride_BothWay(ABS_BuildingElement p_TargetElement, ABS_BuildingElement p_ElementForBuild)
        {
            if (m_OverrideTarget.PrefabGuid == p_TargetElement.PrefabGuid 
                || m_OverrideTarget.PrefabGuid == p_ElementForBuild.PrefabGuid)
            {
                foreach (ABS_BuildingElement element in m_BuildingElementsForChange)
                {
                    if (element == null)
                    {
                        REST_Logging.Warrning("ABS_OverrideElementRule", $"Null element in the set. Rule: {Name}");
                        continue;
                    }
                    else if (element.PrefabGuid == p_ElementForBuild.PrefabGuid || element.PrefabGuid == p_TargetElement.PrefabGuid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanOverride_Set(ABS_BuildingElement p_TargetElement, ABS_BuildingElement p_ElementForBuild)
        {
            bool targerFound = false;
            bool buildElementFound = false;
            foreach (ABS_BuildingElement element in m_BuildingElementsForChange)
            {
                if (element == null)
                {
                    REST_Logging.Warrning("ABS_OverrideElementRule", $"Null element in the set. Rule: {Name}");
                    continue;
                }
                else if (element.PrefabGuid == p_ElementForBuild.PrefabGuid)
                {
                    if (targerFound)
                    {
                        return true;
                    }
                    else
                    {
                        buildElementFound = true;
                    }
                }
                else if (element.PrefabGuid == p_TargetElement.PrefabGuid)
                {
                    if (buildElementFound)
                    {
                        return true;
                    }
                    else
                    {
                        targerFound = true;
                    }
                }
            }
            return targerFound && buildElementFound;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntity Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntity Clone()
        {
            ABS_OverrideElementRule newRule = new ABS_OverrideElementRule(m_Name + "_Copy");
            newRule.m_OverrideTarget = m_OverrideTarget;
            foreach(ABS_BuildingElement be in m_BuildingElementsForChange)
            {
                newRule.m_BuildingElementsForChange.Add(m_OverrideTarget);
            }

            return newRule;
        }
    }
}

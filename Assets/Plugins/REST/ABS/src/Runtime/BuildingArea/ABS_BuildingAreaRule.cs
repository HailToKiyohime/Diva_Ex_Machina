//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [Serializable]
    public class ABS_BuildingAreaRule : ABS_IEntity
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum PermissionType
        {
            Allow,
            Deny
        }

        public enum ScreeningType
        {
            AreaType,
            Object,
            Foundation,
            PreBuilt
        }

        public enum RuleCheckResult
        {
            NotRelated,
            Allow,
            Deny
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public string m_Name = string.Empty;

        public PermissionType m_PermissionType = PermissionType.Allow;
        public ScreeningType m_ScreeningType = ScreeningType.AreaType;

        public List<ABS_BuildingElementAreaType> m_BEAreaTypes = null;
        public List<ABS_BuildingElement> m_BuildingElementObjects = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingAreaRule()
        {
            m_Name = "Rule";
            m_BEAreaTypes = new List<ABS_BuildingElementAreaType>();
            m_BuildingElementObjects = new List<ABS_BuildingElement>();
        }

        public ABS_BuildingAreaRule(string p_Name)
        {
            m_Name = p_Name;
            m_BEAreaTypes = new List<ABS_BuildingElementAreaType>();
            m_BuildingElementObjects = new List<ABS_BuildingElement>();
        }

        public RuleCheckResult Allow (ABS_BuildingElement p_ElementToCheck)
        {
            if (m_ScreeningType == ScreeningType.AreaType)
            {
                if (m_BEAreaTypes.Contains(p_ElementToCheck.AreaType))
                {
                    return MapPermission();
                }
            }
            else if (m_ScreeningType == ScreeningType.Object)
            {
                ABS_BuildingElement foundPerson = m_BuildingElementObjects.Find(x => x.PrefabGuid == p_ElementToCheck.PrefabGuid);
                if (foundPerson != null)
                {
                    return MapPermission();
                }
            } 
            else if (m_ScreeningType == ScreeningType.Foundation)
            {
                if (p_ElementToCheck.Foundation)
                {
                    return MapPermission();
                }
            }
            else if (m_ScreeningType == ScreeningType.PreBuilt)
            {
                if (p_ElementToCheck.PreBuilt)
                {
                    return MapPermission();
                }
            }

            return RuleCheckResult.NotRelated;
        }

        private RuleCheckResult MapPermission()
        {
            if (m_PermissionType == PermissionType.Allow)
            {
                return RuleCheckResult.Allow;
            }
            else
            {
                return RuleCheckResult.Deny;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntity Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntity Clone()
        {
            ABS_BuildingAreaRule newRule = new ABS_BuildingAreaRule(m_Name + "_Copy");

            newRule.m_PermissionType = m_PermissionType;
            newRule.m_ScreeningType = m_ScreeningType;

            newRule.m_BEAreaTypes = new List<ABS_BuildingElementAreaType>();
            foreach (ABS_BuildingElementAreaType type in m_BEAreaTypes)
            {
                newRule.m_BEAreaTypes.Add(type);
            }

            newRule.m_BuildingElementObjects = new List<ABS_BuildingElement>();
            foreach (ABS_BuildingElement type in m_BuildingElementObjects)
            {
                newRule.m_BuildingElementObjects.Add(type);
            }

            return newRule;
        }

        public string Name { get { return m_Name; } }
    }

}
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
    [Serializable]
    public class ABS_AdvancedGridSnapPointRule : ABS_IEntity
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum PermissionType
        {
            Allow,
            Block,
            Deny
        }

        [Serializable]
        public class SnapPoint
        {
            public SnapPoint (ABS_AdvancedGridSnapPoint p_AdvancedGridSnapPoint)
            {
                m_AdvancedGridSnapPoint = new ABS_AdvancedGridSnapPoint(p_AdvancedGridSnapPoint);
            }

            public ABS_AdvancedGridSnapPoint m_AdvancedGridSnapPoint = null;
            public PermissionType m_Permisson = PermissionType.Allow;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private string m_Name = string.Empty;
        [SerializeField] private ABS_AdvancedGridType m_Type = ABS_AdvancedGridType.Floor;
        [SerializeField] private List<SnapPoint> m_SnapPoints = new List<SnapPoint>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_AdvancedGridSnapPointRule()
        {
            m_Name = "Rule";
        }

        public ABS_AdvancedGridSnapPointRule(string p_Name)
        {
            m_Name = p_Name;
        }

        public ABS_AdvancedGridType Type
        { 
            get { return m_Type; } 
            set { m_Type = value; } 
        }
        
        public List<SnapPoint> SnapPoints
        { 
            get { return m_SnapPoints; } 
        }

        public void SetupByTargetType (ABS_AdvancedGridType p_TargetType)
        {
            m_SnapPoints.Clear();
            ABS_AdvancedGridSnapPoint[] snappoints = ABS_AdvancedGridSnapPointCollection.GetSnapPointsForElements(p_TargetType, m_Type);
            foreach (ABS_AdvancedGridSnapPoint sp in snappoints)
            {
                m_SnapPoints.Add(new SnapPoint(sp));
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntity Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntity Clone()
        {
            ABS_AdvancedGridSnapPointRule newRule = new ABS_AdvancedGridSnapPointRule(m_Name + "_Copy");
            newRule.m_Type = m_Type;

            return newRule;
        }

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
    }
}

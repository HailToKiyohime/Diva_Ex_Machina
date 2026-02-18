//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;
using System.ComponentModel;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewSnapRelationship", menuName = "AdvancedBuildingSystem/BuildingElement/New SnapRelationship")]
    public class ABS_SnapRelationship : ABS_DrawableScriptableObject, ABS_IEntityListHolder
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum RelationType
        {
            [Description("Only A can snap to B with this positions but B can not snap to A")]
            AToB,
            [Description("Only B can snap to A with this positions but A can not snap to B")]
            BToA
        }

        [Serializable]
        public class SnapPosition : ABS_IEntity
        {
            public SnapPosition()
            {
                m_Name = "New SnapPosition";
            }

            public SnapPosition(string p_Name)
            {
                m_Name = p_Name;
            }

            public string m_Name = string.Empty;
            public RelationType m_RelationType = RelationType.AToB;

            public Vector3 m_Position = Vector3.zero;
            public Vector3 m_Rotation = Vector3.zero;

            public ABS_IEntity Clone()
            {
                SnapPosition pos = new SnapPosition();
                pos.m_Name = m_Name;
                pos.m_RelationType = m_RelationType;

                pos.m_Position = new Vector3(m_Position.x, m_Position.y, m_Position.z);
                pos.m_Rotation = new Vector3(m_Rotation.x, m_Rotation.y, m_Rotation.z);
                return pos;
            }
            public string Name { get { return m_Name; } }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private ABS_BuildingElement m_ElementA = null;
        [SerializeField] private ABS_BuildingElement m_ElementB = null;

        [SerializeField] private EntityListBase<SnapPosition> m_Positions = new EntityListBase<SnapPosition>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElement ElementA 
        {
            get { return m_ElementA; }
            set { m_ElementA = value; }
        }
        public ABS_BuildingElement ElementB
        {
            get { return m_ElementB; }
            set { m_ElementB = value; }
        }
        public List<SnapPosition> Positions { get { return m_Positions.EntityList; } }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntityList Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IEntityList EntityList { get { return m_Positions; } }

    }
}
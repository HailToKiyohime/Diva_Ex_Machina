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
    public class EntityListBase <EntityType> : ABS_IEntityList where EntityType : class, ABS_IEntity, new()
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] protected List<EntityType> m_EntityList = new List<EntityType>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public List<EntityType> EntityList
        {
            get { return m_EntityList; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IEntityList Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public int EntityCount
        {
            get { return m_EntityList.Count; }
        }

        public ABS_IEntity GetEntity(int p_Index)
        {
            return m_EntityList[p_Index];
        }

        public ABS_IEntity CreateEntity()
        {
            EntityType entity = new EntityType();
            m_EntityList.Add(entity);
            return entity;
        }

        public string GetName(int p_Index)
        {
            return m_EntityList[p_Index].Name;
        }

        public void Remove(int p_Index)
        {
            m_EntityList.RemoveAt(p_Index);
        }

        public void Duplicate(int p_Index)
        {
            m_EntityList.Add(m_EntityList[p_Index].Clone() as EntityType);
        }

        public bool Top(int p_Index)
        {
            if (p_Index == 0)
            {
                return false;
            }

            EntityType ent = m_EntityList[p_Index];
            m_EntityList.RemoveAt(p_Index);
            m_EntityList.Insert(0, ent);

            return true;
        }

        public bool Up(int p_Index)
        {
            if (p_Index == 0)
            {
                return false;
            }

            Move(p_Index, true);
            return true;
        }

        public bool Down(int p_Index)
        {
            if (p_Index == m_EntityList.Count - 1)
            {
                return false;
            }

            Move(p_Index, false);
            return true;
        }

        public bool Bottom(int p_Index)
        {
            if (p_Index == m_EntityList.Count - 1)
            {
                return false;
            }

            EntityType ent = m_EntityList[p_Index];
            m_EntityList.RemoveAt(p_Index);
            m_EntityList.Add(ent);

            return true;
        }

        private void Move(int p_Idx, bool p_Up)
        {
            EntityType ent = m_EntityList[p_Idx];
            m_EntityList.RemoveAt(p_Idx);
            m_EntityList.Insert(p_Idx + (p_Up ? -1 : 1), ent);
        }
    }
}


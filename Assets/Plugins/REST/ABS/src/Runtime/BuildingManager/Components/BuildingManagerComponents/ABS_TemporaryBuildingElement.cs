//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System.Linq;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    [RequireComponent(typeof(BoxCollider))]
    public class ABS_TemporaryBuildingElement : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum ABS_BlockState : ushort
        {
            NOT_BLOCKED,
            BLOCKED_NOT_ENOUGH_MATERIAL,
            BLOCKED_PLAYER_COLLISION,
            BLOCKED_BUILDING_LOGIC,
            BLOCKED_BUILDING_AREA
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_BuildingElement m_TargetBuildingElement = null;
        private Transform m_TargetBuildingElementTransform = null;
        private ABS_PositionValidationData m_ValidationData = null;

        //Blocking
        private Dictionary<ABS_BlockState, bool> m_Blocking = null;
        private bool m_Blocked = false;
        private ABS_BlockState m_LastBlockState = ABS_BlockState.NOT_BLOCKED;
        private ABS_BuildingManagerTracker m_Tracker = null;

        private bool m_FirstElement = false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Basics
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_TemporaryBuildingElement() : base()
        {
            m_Blocking = new Dictionary<ABS_BlockState, bool>()
            {
                { ABS_BlockState.BLOCKED_NOT_ENOUGH_MATERIAL, false },
                { ABS_BlockState.BLOCKED_PLAYER_COLLISION, false },
                { ABS_BlockState.BLOCKED_BUILDING_LOGIC, false },
                { ABS_BlockState.BLOCKED_BUILDING_AREA, false }
            };
        }

        public ABS_BuildingElement TargetBuildingElement
        {
            get { return m_TargetBuildingElement; }
        }

        public ABS_PositionValidationData ValidationData
        {
            get { return m_ValidationData; }
            set { m_ValidationData = value; }
        }

        public bool FirstElement
        {
            get { return m_FirstElement; }
            set { m_FirstElement = value; }
        }

        public ABS_BuildingManagerTracker Tracker
        {
            set 
            {
                m_Tracker = value;
                CheckBlockState();
            } 
        }

        public ABS_BlockState BlockState
        {
            get
            {
                foreach ((ABS_BlockState cause, bool state) in m_Blocking.Reverse())
                {
                    if (state)
                    {
                        return cause;
                    }
                }
                return ABS_BlockState.NOT_BLOCKED;
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Blocking Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool Avaliable()
        {
            return !m_Blocked;
        }

        public void SetBlockstate(ABS_BlockState p_Cause)
        {
            //With this function the blocking can not be removed
            if (p_Cause == ABS_BlockState.NOT_BLOCKED)
            {
                return;
            }

            //Block the object if it wasn't already blocked
            if (Avaliable())
            {
                m_TargetBuildingElementTransform.localScale = Vector3.one * 1.01f;
                m_Blocked = true;
                m_TargetBuildingElement.State = ABS_BuildingElementState.BLOCKED;
            }

            m_Blocking[p_Cause] = true;
            CheckBlockState();
        }

        public void UnSetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState p_Cause)
        {
            //return in it isn't blocked
            if (Avaliable())
            {
                return;
            }

            m_Blocking[p_Cause] = false;

            //check if this object is blocked for any reasons
            foreach ((ABS_BlockState cause, bool state) in m_Blocking)
            {
                if (state)
                {
                    CheckBlockState();
                    return;
                }
            }

            m_TargetBuildingElementTransform.localScale = Vector3.one;
            m_Blocked = false;
            m_TargetBuildingElement.State = ABS_BuildingElementState.PENDING;
            CheckBlockState();
        }

        private void CheckBlockState ()
        {
            ABS_BlockState bs = ABS_BlockState.NOT_BLOCKED;
            foreach ((ABS_BlockState cause, bool state) in m_Blocking.Reverse())
            {
                if (state)
                {
                    bs = cause;
                    break;
                }
            }
            if (m_LastBlockState != bs)
            {
                m_LastBlockState = bs;
                if (m_FirstElement)
                {
                    m_Tracker.FirstBuildingElementBlockedStateChanged(m_LastBlockState);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                SetBlockstate(ABS_BlockState.BLOCKED_PLAYER_COLLISION);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                UnSetBlockstate(ABS_BlockState.BLOCKED_PLAYER_COLLISION);
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  BuildingElement Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Init(ABS_BuildingElement p_TargetBuildingElement)
        {
            m_TargetBuildingElement = p_TargetBuildingElement;

            m_TargetBuildingElementTransform = m_TargetBuildingElement.transform;
            m_TargetBuildingElement.transform.parent = this.transform;
            m_TargetBuildingElement.transform.localPosition = Vector3.zero;
            m_TargetBuildingElementTransform.rotation = Quaternion.Euler(transform.rotation.eulerAngles);

            m_TargetBuildingElement.EnableCollider(false);
            m_TargetBuildingElement.State = ABS_BuildingElementState.PENDING;
            if (m_TargetBuildingElement.ShouldAllowedByArea)
            {
                SetBlockstate(ABS_BlockState.BLOCKED_BUILDING_AREA);
            }
        }
    }
}
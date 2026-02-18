//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEngine.UI;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Demo
{
    public class Hotbar : MonoBehaviour
    {
        [SerializeField] private BuildingElementSlot[] m_Slots = new BuildingElementSlot[6];
        [SerializeField] private ABS_BuildingElementList m_BuildingElements = null;
        [SerializeField] private Text m_HotbarMessage = null;
        private BuildingElementSlot m_CurrentActive = null;
        [SerializeField] private ABS_IBuildingManagerExternalInterface m_BuildingManagerInterface = null;
        [SerializeField] private ABS_BuildingManager m_BuildingManager = null;
        [SerializeField] private ABS_BuildingManagerBuildMode m_BuildingManagerBuildMode = ABS_BuildingManagerBuildMode.Continues;

        private uint m_CurrentBuildingElement = 0;
        private uint m_CurrentLastElement = 0;

        private void Awake()
        {
            if (m_BuildingManager != null)
            {
                m_BuildingManagerInterface = m_BuildingManager as ABS_IBuildingManagerExternalInterface;
            }
        }

        private void Start()
        {
            for (int i = 0; i < 6; ++i)
            {
                m_Slots[i].SetBuildingElement(m_BuildingElements.BuildingElements[i]);
            }
            ChangeTarget(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ChangeHotbarContent();
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeTarget(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeTarget(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChangeTarget(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ChangeTarget(3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                ChangeTarget(4);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                ChangeTarget(5);
            }
        }

        public void ChangeTarget(in uint p_Index, bool p_Force = false)
        {
            BuildingElementSlot slot = m_Slots[p_Index];
            if (m_CurrentActive == slot && !p_Force)
            {
                return;
            }

            if (m_CurrentActive != null)
            {
                m_CurrentActive.State = BuildingElementSlotState.Inactive;
            }

            m_CurrentActive = slot;
            m_CurrentActive.State = BuildingElementSlotState.Active;

            m_CurrentBuildingElement = p_Index;
            m_HotbarMessage.text = m_BuildingElements.BuildingElements[m_CurrentLastElement + m_CurrentBuildingElement].name;
            m_BuildingManagerInterface.Activate(m_CurrentLastElement + m_CurrentBuildingElement, m_BuildingManagerBuildMode);
        }

        public void ChangeHotbarContent()
        {
            m_CurrentLastElement += 6;
            m_CurrentLastElement = m_CurrentLastElement % (uint)m_BuildingElements.BuildingElements.Length;
            for (int i = 0; i < 6; ++i)
            {
                m_Slots[i].SetBuildingElement(m_BuildingElements.BuildingElements[i + m_CurrentLastElement]);
            }
            ChangeTarget(m_CurrentBuildingElement, true);

        }

        public ABS_BuildingManager ABS_BuildingManager
        {
            set
            {
                m_BuildingManagerInterface = value;
            }
        }

        public void RefreshItem ()
        {
            m_BuildingManagerInterface.Activate(m_CurrentBuildingElement + m_CurrentLastElement, m_BuildingManagerBuildMode);
        }
    }
}
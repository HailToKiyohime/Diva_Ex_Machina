//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Demo
{
    public enum BuildingElementSlotState
    {
        Active,
        Inactive
    }

    public class BuildingElementSlot : MonoBehaviour
    {
        [SerializeField] private Color m_DefaultColor = Color.yellow;
        [SerializeField] private Color m_ActiveColor = new Color32(255, 123, 0, 255);
        private UnityEngine.UI.Image m_Image = null;

        private BuildingElementSlotState m_State = BuildingElementSlotState.Inactive;

        private GameObject m_BuildingElementObject = null;
        private Vector3 m_Rotation = new Vector3(-20f, 0.1f, 0f);

        protected virtual void Awake()
        {
            AlignStateColor();
        }

        public void SetBuildingElement (ABS_BuildingElement p_BuildingElement)
        {
            if (m_BuildingElementObject != null)
            {
                DestroyImmediate(m_BuildingElementObject);
            }
            m_BuildingElementObject = null;
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 position = rectTransform.TransformPoint(new Vector3(0f, 0f, -50f));
            m_BuildingElementObject = Instantiate(p_BuildingElement.gameObject, position, Quaternion.Euler(-20f, 0f, 0f), this.transform);
            m_BuildingElementObject.layer = LayerMask.NameToLayer("UI");
            m_BuildingElementObject.transform.localScale *= 50;
            m_BuildingElementObject.transform.localScale /= (p_BuildingElement.Dimension.x + p_BuildingElement.Dimension.y + p_BuildingElement.Dimension.z) / 3;
        }

        public BuildingElementSlotState State
        {
            get { return m_State; }
            set
            {
                m_State = value;
                AlignStateColor();
            }
        }
        private void AlignStateColor()
        {
            if (m_Image == null)
            {
                m_Image = GetComponent<UnityEngine.UI.Image>();
            }

            if (m_State == BuildingElementSlotState.Active)
            {
                m_Image.color = m_ActiveColor;
            }
            else
            {
                m_Image.color = m_DefaultColor;
            }
        }

        public void Update()
        {
            m_Rotation = new Vector3(-20f, m_Rotation.y + 0.2f, 0);
            if (m_BuildingElementObject != null)
            {
                m_BuildingElementObject.transform.rotation = UnityEngine.Quaternion.Euler(m_Rotation);
            }
        }

    }
}
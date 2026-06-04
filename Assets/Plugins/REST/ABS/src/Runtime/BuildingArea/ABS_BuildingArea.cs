//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_BuildingArea : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum AreaShape
        {
            Sphere,
            Box
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private Transform m_Transform = null;

        [SerializeField] private ABS_BuildingAreaRuleset m_Rules = null;
        [SerializeField] private AreaShape m_Shape = AreaShape.Sphere;
        [SerializeField][Range(0, float.MaxValue)] private float m_SphereSize = 5;
        [SerializeField] private Vector3 m_BoxSize = Vector3.one * 5;

        [SerializeField] private ABS_LayerCollection m_LayerCollection = null;

#if UNITY_EDITOR
        private ABS_ProjectSettings m_ProjectSettings = null;
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingAreaRuleset Rules { get { return m_Rules; } }
        public AreaShape Shape { get { return m_Shape; } }
        public float SphereSize { get { return m_SphereSize; } }
        public Vector3 BoxSize { get { return m_BoxSize; } }
        public ABS_LayerCollection LayerCollection { get { return m_LayerCollection; } }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  MonoBehaviour Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Awake()
        {
            m_Transform = transform;

            Collider collider = null;
            if (m_Shape == AreaShape.Sphere)
            {
                collider = gameObject.AddComponent<SphereCollider>();
                (collider as SphereCollider).radius = m_SphereSize;
            }
            else
            {
                collider = gameObject.AddComponent<BoxCollider>();
                (collider as BoxCollider).size = m_BoxSize;
            }
            collider.isTrigger = true;
            //collider.includeLayers = m_Settings.LayerCollection.LayerOfBuildingElement;
        }
        private void OnTriggerEnter(Collider other)
        {
            ABS_TemporaryBuildingElement tmpElement = other.gameObject.GetComponent<ABS_TemporaryBuildingElement>();
            if (tmpElement != null)
            {
                if (CheckBuildingElement(tmpElement.TargetBuildingElement))
                {
                    if (tmpElement.TargetBuildingElement.ShouldAllowedByArea)
                    {
                        tmpElement.UnSetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_AREA);
                    }
                }
                else
                {
                    if (!tmpElement.TargetBuildingElement.ShouldAllowedByArea)
                    {
                        tmpElement.SetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_AREA);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            ABS_TemporaryBuildingElement tmpElement = other.gameObject.GetComponent<ABS_TemporaryBuildingElement>();
            if (tmpElement != null)
            {
                if (tmpElement.TargetBuildingElement.ShouldAllowedByArea)
                {
                    tmpElement.SetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_AREA);
                }
                else
                {
                    tmpElement.UnSetBlockstate(ABS_TemporaryBuildingElement.ABS_BlockState.BLOCKED_BUILDING_AREA);
                }
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool CheckBuildingElement (ABS_BuildingElement p_ElementToCheck)
        {
            if(m_Rules == null)
            {
                REST_Logging.Error($"{this}", "Missing Rules");
                return true;
            }

            return m_Rules.Allow(p_ElementToCheck);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (m_ProjectSettings == null)
            {
                m_ProjectSettings = ABS_ProjectSettingsGetter.GetSettings();
            }

            if (!m_ProjectSettings.BuildingArea_AreaCollider)
            {
                return;
            }

            if (m_Shape == AreaShape.Sphere)
            {
                REST_GizmosUtils.DrawWireSphere(transform.position, m_SphereSize, m_ProjectSettings.BuildingArea_AreaColliderColor);
            }
            else
            {
                REST_GizmosUtils.DrawWireCube(m_BoxSize, transform.position, transform.eulerAngles, m_ProjectSettings.BuildingArea_AreaColliderColor);
            }
        }
#endif

    }
}

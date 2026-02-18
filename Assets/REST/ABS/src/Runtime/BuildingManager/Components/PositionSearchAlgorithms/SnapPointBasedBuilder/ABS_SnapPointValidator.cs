//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_SnapPointValidator : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        private ABS_SnapPointManager m_SnapPointManager;

        private ABS_SnapPointBasedBuilding m_Building;
        private ABS_BuildingElement m_Element;
        private Vector3 m_Position;
        private Vector3 m_Rotation;

        private bool m_Initalized = false;
        private bool m_Setup = false;
        private bool m_Reported = false;
        private float m_RigidbodyActivationArea = 3;
        private LayerMask m_Layer;

        private List<ABS_SnapPointValidationSigner> m_SignedElements = new List<ABS_SnapPointValidationSigner>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Init(
            in ABS_SnapPointManager p_SnapPointManager, 
            in float p_RigidbodyActivationArea, 
            in LayerMask p_Layer,
            in ABS_SnapPointBasedBuilding p_Building,
            in ABS_BuildingElement p_Element,
            in Vector3 p_Position,
            in Vector3 p_Rotation)
        {
            m_SnapPointManager = p_SnapPointManager;
            m_RigidbodyActivationArea = p_RigidbodyActivationArea;
            m_Initalized = true;
            m_Building = p_Building;
            m_Element = p_Element;
            m_Position = p_Position;
            m_Rotation = p_Rotation;
            m_Layer = p_Layer;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  private Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Update()
        {
            if (m_Initalized)
            {
                if (!m_Setup)
                {
                    Setup();
                }
                else
                {
                    Report(false);
                }
            }
        }

        private void Setup ()
        {
            Collider[] colliders = REST_CollisionChecker.OverlapSphere(Vector3.zero, m_RigidbodyActivationArea, m_Layer);

            if (colliders.Length == 0)
            {
                Report(false);
                return;
            }

            foreach (Collider coll in colliders)
            {
                ABS_BuildingElement element = coll.GetComponent<ABS_BuildingElement>();
                if (element != null)
                {
                    ABS_SnapPointValidationSigner signer = element.GetComponent<ABS_SnapPointValidationSigner>();
                    if(signer == null)
                    {
                        signer = element.gameObject.AddComponent<ABS_SnapPointValidationSigner>();
                        signer.Init(m_Layer);
                    }
                    signer.IncreaseCounter();
                    m_SignedElements.Add(signer);
                }
            }
            m_Setup = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            ABS_BuildingElement element = other.GetComponent<ABS_BuildingElement>();
            if (element != null)
            {
                Report(true);
            }
        }

        private void Report (bool p_Result)
        {
            if (m_Reported)
            {
                return;
            }
            m_SnapPointManager.Report(m_Building, m_Element, m_Position, m_Rotation, p_Result);
            foreach (ABS_SnapPointValidationSigner signer in m_SignedElements)
            {
                signer.DecreaseCounter();
            }
            m_Reported = true;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Report(false);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Gizmos
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying && !m_Initalized && !m_Setup)
            {
                return;
            }

            REST_GizmosUtils.DrawWireSphere(transform.position, m_RigidbodyActivationArea, Color.green);
        }
#endif
    }
}
//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_SnapPointValidationSigner : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private int m_ReferenceCounter = 0;
        private Rigidbody m_Rigidbody = null;
        private bool m_RigidbodyAded = false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Init(in LayerMask p_Layer)
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                m_Rigidbody = gameObject.AddComponent<Rigidbody>();
                m_Rigidbody.isKinematic = true;
                m_Rigidbody.useGravity = false;
                //m_Rigidbody.includeLayers = p_Layer;
                m_RigidbodyAded = true;
            }
        }

        public void IncreaseCounter ()
        {
            ++m_ReferenceCounter;
        }

        public void DecreaseCounter()
        {
            --m_ReferenceCounter;
            if (m_ReferenceCounter == 0)
            {
                RemoveRigidBody();
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            RemoveRigidBody();
        }

        private void RemoveRigidBody()
        {
            if (m_RigidbodyAded)
            {
                Destroy(m_Rigidbody);
            }
        }
    }
}

//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Demo
{
    public class TDCharacter : MonoBehaviour
    {
        public Transform myCameraTransform;

        [SerializeField] private float m_MovementSpeed = 5f;

        private void Update()
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            Vector3 movementDirection = Vector3.ProjectOnPlane(myCameraTransform.forward, Vector3.up) * verticalInput +
                                          Vector3.ProjectOnPlane(myCameraTransform.right, Vector3.up) * horizontalInput;

            myCameraTransform.position += 
                movementDirection
                * (Input.GetKey(KeyCode.LeftShift) ? (m_MovementSpeed * 2) : m_MovementSpeed)
                * Time.deltaTime;
        }

        public void SetRotation(Vector3 p_EulerAngles)
        {
            myCameraTransform.localEulerAngles = p_EulerAngles;
        }

        public void SetPosition(Vector3 p_Position)
        {
            myCameraTransform.localPosition = p_Position;
        }
    }
}
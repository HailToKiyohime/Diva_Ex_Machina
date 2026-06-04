
//*********************************************************************
//  Dependencies: System
using System;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Demo
{
    public class FlyingFPSCharacter : MonoBehaviour
    {
        public Transform myCameraTransform;

        [SerializeField][Range(1, 100)] private float m_MouseSensitivity = 50f;
        [SerializeField] private float m_MovementSpeed = 5f;

        private float m_RotationX = 0f;
        private float m_RotationY = 0f;

        private void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            //Camera Rotation
            {
                m_RotationY += Input.GetAxis("Mouse X") * m_MouseSensitivity * 0.03f;
                m_RotationX += Input.GetAxis("Mouse Y") * -1 * m_MouseSensitivity * 0.03f;
                if (m_RotationX > 90.0f)
                {
                    m_RotationX = 90.0f;
                }
                else if (m_RotationX < -90.0f)
                {
                    m_RotationX = -90.0f;
                }
                myCameraTransform.localEulerAngles = new Vector3(m_RotationX, m_RotationY, 0f);
            }

            //Camera Movement
            {
                float horizontalInput = Input.GetAxisRaw("Horizontal");
                float verticalInput = Input.GetAxisRaw("Vertical");

                float verticalShifting = 0f;
                //move UP
                if (Input.GetKey(KeyCode.Space) && !Input.GetKey(KeyCode.LeftControl))
                {
                    verticalShifting = Input.GetKey(KeyCode.LeftShift) ? m_MovementSpeed : m_MovementSpeed / 2;
                }
                //move Down
                else if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.Space))
                {
                    verticalShifting = Input.GetKey(KeyCode.LeftShift) ? -m_MovementSpeed : -(m_MovementSpeed / 2);
                }

                myCameraTransform.Translate(new Vector3(horizontalInput, 0f, verticalInput) * (Input.GetKey(KeyCode.LeftShift) ? (m_MovementSpeed * 2) : m_MovementSpeed) * Time.deltaTime);
                myCameraTransform.position += Vector3.up * verticalShifting * Time.deltaTime;

            }
        }

        public void SetRotation (Vector3 p_EulerAngles)
        {
            m_RotationX = p_EulerAngles.x;
            m_RotationY = p_EulerAngles.y;
        }
    }
}
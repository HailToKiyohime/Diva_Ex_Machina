//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Demo
{
    public class CharacterChanger : MonoBehaviour
    {
        //ThirdPersonCharacter
        [SerializeField] private GameObject m_TPSCharacter = null;
        [SerializeField] private GameObject m_TPSCamera = null;
        [SerializeField] private ABS_BuildingManager m_TPSManager = null;
        // FirstPersonCharacter
        [SerializeField] private GameObject m_FPSCharacter = null;
        [SerializeField] private GameObject m_FPSCamera = null;
        [SerializeField] private ABS_BuildingManager m_FPSManager = null;
        //TopDownCharacter
        [SerializeField] private GameObject m_TDCharacter = null;
        //[SerializeField] private GameObject m_TDCamera = null;
        [SerializeField] private ABS_BuildingManager m_TDManager = null;

        [SerializeField] private Hotbar m_Hotbar = null;

        // 0 = TPS
        // 1 = FPS
        // 2 = TD
        private int m_Active = 0;

        private void Awake ()
        {
            //Because cheese
            m_TPSCharacter.SetActive(false);
            m_TPSCharacter.SetActive(true);
            m_Hotbar.ABS_BuildingManager = m_TPSManager;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (m_Active == 0)
                {
                    Setup_FPS();
                }
                else if (m_Active == 1)
                {
                    Setup_TD();
                }
                else if (m_Active == 2)
                {
                    Setup_TPS();
                }
            }
        }

        private void Setup_TPS()
        {
            m_TDCharacter.SetActive(false);
            m_TDManager.Deactivate();

            m_TPSCharacter.SetActive(true);
            m_Active = 0;

            m_Hotbar.ABS_BuildingManager = m_TPSManager;
            m_Hotbar.RefreshItem();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Setup_TD ()
        {
            m_FPSCharacter.SetActive(false);
            m_FPSManager.Deactivate();

            m_TDCharacter.SetActive(true);
            m_Active = 2;

            m_TDCharacter.transform.GetComponent<TDCharacter>().SetPosition(new Vector3(
                m_FPSCamera.transform.position.x,
                0,
                m_FPSCamera.transform.position.z
                ));
            m_TDCharacter.transform.GetComponent<TDCharacter>().SetRotation(new Vector3(45f, m_FPSCamera.transform.localEulerAngles.y, 0));

            m_Hotbar.ABS_BuildingManager = m_TDManager;
            m_Hotbar.RefreshItem();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Setup_FPS()
        {
            m_TPSCharacter.SetActive(false);
            m_TPSManager.Deactivate();

            m_FPSCharacter.SetActive(true);
            m_Active = 1;

            m_FPSCamera.transform.position = m_TPSCamera.transform.position;
            m_FPSCamera.transform.localEulerAngles = m_TPSCamera.transform.localEulerAngles;
            m_FPSCharacter.transform.GetComponent<FlyingFPSCharacter>().SetRotation(m_TPSCamera.transform.localEulerAngles);

            m_Hotbar.ABS_BuildingManager = m_FPSManager;
            m_Hotbar.RefreshItem();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
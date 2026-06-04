//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEngine.EventSystems;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_UIRaycastLogger : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;
                List<RaycastResult> results = new List<RaycastResult>();

                EventSystem.current.RaycastAll(eventData, results);
                if (results.Count > 0)
                {
                    REST_Logging.Debug($"{this}", $"UIRaycastLogger Hit: {results[0].gameObject.name}");
                }
            }
        }

    }
}
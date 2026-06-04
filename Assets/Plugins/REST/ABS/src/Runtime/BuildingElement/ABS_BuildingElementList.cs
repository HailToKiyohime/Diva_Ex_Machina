//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewBuildingElementList", menuName = "AdvancedBuildingSystem/BuildingElement/New BuildingElement List")]
   public class ABS_BuildingElementList : ScriptableObject
    {
        [Serializable]
        public class ABS_BuildingElementRow
        {
            [SerializeField] public ABS_BuildingElement m_Element;
        }

        [SerializeField] private ABS_BuildingElement[] m_BuildingElements = new ABS_BuildingElement[0];
        private Dictionary<string, ABS_BuildingElement> m_BuildingElementsDict = null;

        public ABS_BuildingElement[] BuildingElements
        {
            get {return m_BuildingElements; }
        }   

        public Dictionary<string, ABS_BuildingElement> BuildingElementsDict
        {
            get 
            {
                if (m_BuildingElementsDict == null)
                {
                    m_BuildingElementsDict = new Dictionary<string, ABS_BuildingElement>();
                    foreach (ABS_BuildingElement element in m_BuildingElements)
                    {
                        if (!m_BuildingElementsDict.TryAdd(element.PrefabGuid, element))
                        {
                            REST_Logging.Warrning("ABS_BuildingElementList", 
                                $"Duplicated Guid in the List: {element.PrefabGuid} " +
                                $"| element: {element.name} " +
                                $"| element from Dict: {m_BuildingElementsDict[element.PrefabGuid].name}");
                        }
                    }
                }
                return m_BuildingElementsDict;
            }
        }

        public ABS_BuildingElement GetElementPrefab(in string p_PrefabGuid)
        {
            Dictionary<string, ABS_BuildingElement> elementDict = BuildingElementsDict;
            ABS_BuildingElement buildingElementPrefab = null;
            if (!elementDict.TryGetValue(p_PrefabGuid, out buildingElementPrefab))
            {
                return null;
            }
            return buildingElementPrefab;
        }
    }
}
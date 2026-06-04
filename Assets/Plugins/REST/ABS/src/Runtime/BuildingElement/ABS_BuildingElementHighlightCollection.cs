//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    [CreateAssetMenu(fileName = "NewHighlightCollection", menuName = "AdvancedBuildingSystem/BuildingElement/New Highlight Collection")]
    public class ABS_BuildingElementHighlightCollection : ScriptableObject
    {
        [SerializeField] private Material m_PendingHighlightMaterial = null;
        [SerializeField] private Material m_BlockedHighlightMaterial = null;
        [SerializeField] private Material m_DestroyHighlightMaterial = null;
        [SerializeField] private Material m_PreBuiltHighlightMaterial = null;

        [SerializeField] private List<Material> m_CustomHighlightMaterials = new List<Material>();

        public Material PendingHighlightMaterial { get { return m_PendingHighlightMaterial; } }
        public Material BlockedHighlightMaterial { get { return m_BlockedHighlightMaterial; } }
        public Material DestroyHighlightMaterial { get { return m_DestroyHighlightMaterial; } }
        public Material PreBuiltHighlightMaterial { get { return m_PreBuiltHighlightMaterial; } }

        public List<Material> CustomHighlightMaterials { get { return m_CustomHighlightMaterials; } }
    }
}
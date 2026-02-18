using UnityEngine;
using REST.AdvancedBuildSystem;
public class Tracker : ABS_BuildingManagerTracker
{
    public ABS_BuildingElement m_Element;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_BuildingManager.Activate(m_Element, ABS_BuildingManagerBuildMode.Continues);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections.Generic;
using UnityEngine;

public class LandshipNavigation : MonoBehaviour
{
    public static LandshipNavigation Instance { get; private set; }
    public Transform ghostShip;          // Ghost ship root (contains baked navmesh)
    public Transform core;               // Core target on real ship
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

}
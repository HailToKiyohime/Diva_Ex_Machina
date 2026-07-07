using UnityEngine;
using System.Collections.Generic; // Required namespace

[System.Serializable]
public enum EnemyState
{
    Idle,
    Patrolling,
    Retreat,
    Chasing,
    Combat,
}
[System.Serializable]
public enum TargetType { 
    Player,
    Enemy,
    Building,
    Core,
    Obstacle,
}
[System.Serializable]



[System.Serializable]
public class Target { 
    public Transform targetTransform;
    public float targetPriority;
    public float priorityDecreaseMultiplier;
    public Target(Transform targetTransform, float targetPriority,float priorityDecreaseMultiplier)
    {
        this.targetTransform = targetTransform;
        this.targetPriority = targetPriority;
        this.priorityDecreaseMultiplier = priorityDecreaseMultiplier;
    }
}


public class ModularEntityBrain : MonoBehaviour
{
    public EnemyState currentState;
    public List<Target> targets = new List<Target>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = EnemyState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
